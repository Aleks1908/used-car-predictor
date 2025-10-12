using System;
using System.Collections.Generic;
using System.Linq;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Models
{
    /// <summary>
    /// Random Forest for regression built from multiple DecisionTreeRegressor models.
    /// - Bagging (bootstrap samples of rows) per tree.
    /// - Final prediction is the average of tree predictions.
    /// NOTE: This baseline version uses all features in each tree. (Feature subsampling is a later enhancement.)
    /// </summary>
    ///
    public class RandomForestRegressor : IRegressor

    {
        private readonly int _nEstimators;
        private readonly int _maxDepth;
        private readonly int _minSamplesSplit;
        private readonly int _minSamplesLeaf;
        private readonly bool _bootstrap;
        private readonly double _sampleRatio; // proportion of rows per tree (with replacement)
        private readonly int _randomSeed;

        private readonly List<DecisionTreeRegressor> _trees = new();
        private Random _rng;

        public string Name => "Random Forest Regressor";

        public RandomForestRegressor(
            int nEstimators = 50,
            int maxDepth = 8,
            int minSamplesSplit = 10,
            int minSamplesLeaf = 5,
            bool bootstrap = true,
            double sampleRatio = 1.0,
            int randomSeed = 42)
        {
            _nEstimators = Math.Max(1, nEstimators);
            _maxDepth = maxDepth;
            _minSamplesSplit = minSamplesSplit;
            _minSamplesLeaf = minSamplesLeaf;
            _bootstrap = bootstrap;
            _sampleRatio = Math.Clamp(sampleRatio, 0.1, 1.0);
            _randomSeed = randomSeed;
            _rng = new Random(_randomSeed);
        }

        public void Fit(double[,] features, double[] labels)
        {
            _trees.Clear();
            int n = features.GetLength(0);
            int bagSize = (int)Math.Max(1, Math.Round(n * _sampleRatio));
            var trees = new DecisionTreeRegressor[_nEstimators];

            System.Threading.Tasks.Parallel.For(0, _nEstimators, t =>
            {
                var rng = new Random(_randomSeed + t);
                int[] idx = SampleWithReplacement(rng, n, bagSize);
                var bagX = Subset(features, idx);
                var bagY = Subset(labels, idx);

                var tree = new DecisionTreeRegressor(
                    maxDepth: _maxDepth,
                    minSamplesSplit: _minSamplesSplit,
                    minSamplesLeaf: _minSamplesLeaf,
                    maxSplitsPerFeature: 32 // see #4 below
                );
                tree.Fit(bagX, bagY);
                trees[t] = tree;
            });

            _trees.AddRange(trees);
        }


        public double[] Predict(double[,] features)
        {
            if (_trees.Count == 0)
                throw new InvalidOperationException("Forest not trained yet.");

            int n = features.GetLength(0);
            var sums = new double[n];

            foreach (var tree in _trees)
            {
                var preds = tree.Predict(features);
                for (int i = 0; i < n; i++) sums[i] += preds[i];
            }

            // average
            for (int i = 0; i < n; i++) sums[i] /= _trees.Count;
            return sums;
        }

        public double Predict(double[] featureRow)
        {
            if (_trees.Count == 0)
                throw new InvalidOperationException("Forest not trained yet.");

            double sum = 0;
            foreach (var tree in _trees)
                sum += tree.Predict(featureRow);

            return sum / _trees.Count;
        }

        // ----------------- helpers -----------------

        private static int[] SampleWithReplacement(Random rng, int n, int k)
        {
            var idx = new int[k];
            for (int i = 0; i < k; i++)
                idx[i] = rng.Next(n); // [0, n)
            return idx;
        }

        private static double[,] Subset(double[,] matrix, int[] indices)
        {
            int rows = indices.Length;
            int cols = matrix.GetLength(1);
            var result = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = matrix[indices[i], j];
            return result;
        }

        private static double[] Subset(double[] arr, int[] indices)
        {
            var result = new double[indices.Length];
            for (int i = 0; i < indices.Length; i++)
                result[i] = arr[indices[i]];
            return result;
        }

        public static RandomForestRegressor TrainWithBestParams(
            double[,] trainFeatures, double[] trainLabels,
            double[,] valFeatures, double[] valLabels,
            LabelScaler labelScaler)
        {
            // Define hyperparameter grid
            int[] nEstimatorsList = { 30, 50, 80 };
            int[] maxDepths = { 6, 8, 10 };
            int[] minLeaf = { 5, 10, 15 };
            double[] sampleRatios = { 0.6, 0.8 };

            double bestRmse = double.PositiveInfinity;
            RandomForestRegressor? bestModel = null;
            Dictionary<string, object>? bestParams = null;

            var paramGrid = new List<Dictionary<string, object>>();
            foreach (var nEst in nEstimatorsList)
            foreach (var md in maxDepths)
            foreach (var ml in minLeaf)
            foreach (var sr in sampleRatios)
            {
                paramGrid.Add(new Dictionary<string, object>
                {
                    { "nEstimators", nEst },
                    { "maxDepth", md },
                    { "minSamplesLeaf", ml },
                    { "sampleRatio", sr },
                    { "minSamplesSplit", 10 },
                    { "bootstrap", true },
                    { "randomSeed", 42 }
                });
            }

            // Grid search
            foreach (var p in paramGrid)
            {
                var model = new RandomForestRegressor(
                    nEstimators: (int)p["nEstimators"],
                    maxDepth: (int)p["maxDepth"],
                    minSamplesSplit: (int)p["minSamplesSplit"],
                    minSamplesLeaf: (int)p["minSamplesLeaf"],
                    bootstrap: (bool)p["bootstrap"],
                    sampleRatio: (double)p["sampleRatio"],
                    randomSeed: (int)p["randomSeed"]
                );

                model.Fit(trainFeatures, trainLabels);

                var preds = labelScaler.InverseTransform(model.Predict(valFeatures));
                var trueVals = labelScaler.InverseTransform(valLabels);

                var rmse = Metrics.RootMeanSquaredError(trueVals, preds);
                var mae = Metrics.MeanAbsoluteError(trueVals, preds);
                var r2 = Metrics.RSquared(trueVals, preds);

                Console.WriteLine($"[RF tune] nEst={p["nEstimators"]}, maxDepth={p["maxDepth"]}, " +
                                  $"minLeaf={p["minSamplesLeaf"]}, sampleRatio={p["sampleRatio"]} -> RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                    bestParams = p;
                }
            }

            if (bestModel == null) throw new InvalidOperationException("RF grid search failed.");

            Console.WriteLine($"[GridSearch] Best params: nEstimators={bestParams!["nEstimators"]}, " +
                              $"maxDepth={bestParams!["maxDepth"]}, minLeaf={bestParams!["minSamplesLeaf"]}, " +
                              $"sampleRatio={bestParams!["sampleRatio"]}");

            // Retrain best model on full training set
            var finalRf = new RandomForestRegressor(
                nEstimators: (int)bestParams!["nEstimators"],
                maxDepth: (int)bestParams!["maxDepth"],
                minSamplesSplit: (int)bestParams!["minSamplesSplit"],
                minSamplesLeaf: (int)bestParams!["minSamplesLeaf"],
                bootstrap: (bool)bestParams!["bootstrap"],
                sampleRatio: (double)bestParams!["sampleRatio"],
                randomSeed: (int)bestParams!["randomSeed"]
            );

            finalRf.Fit(trainFeatures, trainLabels);
            return finalRf;
        }
    }
}