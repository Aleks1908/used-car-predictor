using System;
using System.Collections.Generic;
using System.Linq;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Models
{
    public class RandomForestRegressor : IRegressor
    {
        private readonly int _nEstimators, _maxDepth, _minSamplesSplit, _minSamplesLeaf;
        private readonly bool _bootstrap;
        private readonly double _sampleRatio;
        private readonly List<DecisionTreeRegressor> _trees = new();
        private readonly Random _rng;

        public string Name => "Random Forest Regressor";

        public RandomForestRegressor(
            int nEstimators = 50,
            int maxDepth = 8,
            int minSamplesSplit = 10,
            int minSamplesLeaf = 5,
            bool bootstrap = true,
            double sampleRatio = 1.0)
        {
            _nEstimators = Math.Max(1, nEstimators);
            _maxDepth = Math.Max(1, maxDepth);
            _minSamplesSplit = Math.Max(2, minSamplesSplit);
            _minSamplesLeaf = Math.Max(1, minSamplesLeaf);
            _bootstrap = bootstrap;
            _sampleRatio = Math.Clamp(sampleRatio, 0.1, 1.0);

            _rng = Random.Shared;
        }


        public void Fit(double[,] features, double[] labels)
        {
            _trees.Clear();
            int n = features.GetLength(0);
            int bagSize = (int)Math.Max(1, Math.Round(n * _sampleRatio));
            var trees = new DecisionTreeRegressor[_nEstimators];

            System.Threading.Tasks.Parallel.For(0, _nEstimators, t =>
            {
                var treeRng = new Random(_rng.Next());

                int[] idx = _bootstrap
                    ? SampleWithReplacement(treeRng, n, bagSize)
                    : SampleWithoutReplacement(treeRng, n, bagSize);

                var bagX = Subset(features, idx);
                var bagY = Subset(labels, idx);

                var tree = new DecisionTreeRegressor(
                    maxDepth: _maxDepth,
                    minSamplesSplit: _minSamplesSplit,
                    minSamplesLeaf: _minSamplesLeaf,
                    maxSplitsPerFeature: 32
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

            for (int i = 0; i < n; i++) sums[i] /= _trees.Count;
            return sums;
        }

        public double Predict(double[] featureRow)
        {
            if (_trees.Count == 0)
                throw new InvalidOperationException("Forest not trained yet.");

            double sum = 0;
            foreach (var tree in _trees) sum += tree.Predict(featureRow);
            return sum / _trees.Count;
        }


        public static RandomForestRegressor TrainWithBestParams(
            double[,] trainFeatures, double[] trainLabels,
            double[,] valFeatures, double[] valLabels,
            LabelScaler labelScaler,
            int? searchSeed = null)
        {
            var rng = new Random(searchSeed ?? Random.Shared.Next());

            int[] nEstimatorsList = { 30, 50, 80 };
            int[] maxDepths = { 6, 8, 10 };
            int[] minLeaf = { 5, 10, 15 };
            int[] minSplit = { 5, 10, 20 };
            double[] sampleRatios = { 0.6, 0.8 };
            bool[] bootstraps = { true };

            double bestRmse = double.PositiveInfinity;
            RandomForestRegressor? bestModel = null;
            Dictionary<string, object>? bestParams = null;

            foreach (var nEst in nEstimatorsList)
            foreach (var md in maxDepths)
            foreach (var ml in minLeaf)
            foreach (var ms in minSplit)
            foreach (var sr in sampleRatios)
            foreach (var bs in bootstraps)
            {
                var p = new Dictionary<string, object>
                {
                    { "nEstimators", nEst },
                    { "maxDepth", md },
                    { "minSamplesLeaf", ml },
                    { "minSamplesSplit", ms },
                    { "sampleRatio", sr },
                    { "bootstrap", bs }
                };

                var model = new RandomForestRegressor(
                    nEstimators: nEst,
                    maxDepth: md,
                    minSamplesSplit: ms,
                    minSamplesLeaf: ml,
                    bootstrap: bs,
                    sampleRatio: sr
                );

                model.Fit(trainFeatures, trainLabels);

                var preds = labelScaler.InverseTransform(model.Predict(valFeatures));
                var truth = labelScaler.InverseTransform(valLabels);

                var rmse = Metrics.RootMeanSquaredError(truth, preds);
                var mae = Metrics.MeanAbsoluteError(truth, preds);
                var r2 = Metrics.RSquared(truth, preds);

                Console.WriteLine(
                    $"[RF tune] nEst={p["nEstimators"]}, maxDepth={p["maxDepth"]}, " +
                    $"minSplit={p["minSamplesSplit"]}, minLeaf={p["minSamplesLeaf"]}, sampleRatio={p["sampleRatio"]} -> " +
                    $"RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                    bestParams = p;
                }
            }

            if (bestModel == null) throw new InvalidOperationException("RF grid search failed.");

            Console.WriteLine($"[RF Best] nEstimators={bestParams!["nEstimators"]}, " +
                              $"maxDepth={bestParams!["maxDepth"]}, minSplit={bestParams!["minSamplesSplit"]}, " +
                              $"minLeaf={bestParams!["minSamplesLeaf"]}, sampleRatio={bestParams!["sampleRatio"]}");

            return bestModel;
        }


        public static RandomForestRegressor TrainResidualsWithBestParams(
            double[,] trainX, double[] trainResidualY,
            double[,] valX, double[] valResidualY,
            int maxConfigs = 60,
            int? searchSeed = null)
        {
            var rng = new Random(searchSeed ?? Random.Shared.Next());

            int[] nEstimatorsList = { 50, 80, 120 };
            int[] maxDepths = { 6, 8, 10 };
            int[] minLeaf = { 3, 5, 10, 15 };
            int[] minSplit = { 5, 10, 20 };
            double[] sampleRatios = { 0.6, 0.8, 1.0 };
            bool[] bootstraps = { true };

            double bestRmse = double.PositiveInfinity;
            RandomForestRegressor? bestModel = null;
            Dictionary<string, object>? bestParams = null;

            for (int trial = 0; trial < maxConfigs; trial++)
            {
                var p = new Dictionary<string, object>
                {
                    ["nEstimators"] = nEstimatorsList[rng.Next(nEstimatorsList.Length)],
                    ["maxDepth"] = maxDepths[rng.Next(maxDepths.Length)],
                    ["minSamplesLeaf"] = minLeaf[rng.Next(minLeaf.Length)],
                    ["minSamplesSplit"] = minSplit[rng.Next(minSplit.Length)],
                    ["sampleRatio"] = sampleRatios[rng.Next(sampleRatios.Length)],
                    ["bootstrap"] = bootstraps[rng.Next(bootstraps.Length)]
                };

                var model = new RandomForestRegressor(
                    nEstimators: (int)p["nEstimators"],
                    maxDepth: (int)p["maxDepth"],
                    minSamplesSplit: (int)p["minSamplesSplit"],
                    minSamplesLeaf: (int)p["minSamplesLeaf"],
                    bootstrap: (bool)p["bootstrap"],
                    sampleRatio: (double)p["sampleRatio"]
                );

                model.Fit(trainX, trainResidualY);

                var valPredRes = model.Predict(valX);
                var rmse = Metrics.RootMeanSquaredError(valResidualY, valPredRes);
                var mae = Metrics.MeanAbsoluteError(valResidualY, valPredRes);
                var r2 = Metrics.RSquared(valResidualY, valPredRes);

                Console.WriteLine(
                    $"[RF(res) tune] try={trial + 1}/{maxConfigs} " +
                    $"nEst={p["nEstimators"]}, maxDepth={p["maxDepth"]}, minSplit={p["minSamplesSplit"]}, " +
                    $"minLeaf={p["minSamplesLeaf"]}, sampleRatio={p["sampleRatio"]}, bootstrap={p["bootstrap"]} -> " +
                    $"RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                    bestParams = p;
                }
            }

            if (bestModel == null)
                throw new InvalidOperationException("RF residual search failed.");

            Console.WriteLine(
                $"[RF(res) Best] nEst={bestParams!["nEstimators"]}, maxDepth={bestParams!["maxDepth"]}, " +
                $"minSplit={bestParams!["minSamplesSplit"]}, minLeaf={bestParams!["minSamplesLeaf"]}, " +
                $"sampleRatio={bestParams!["sampleRatio"]}, bootstrap={bestParams!["bootstrap"]}");

            return bestModel;
        }


        private static int[] SampleWithReplacement(Random rng, int n, int k)
        {
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = rng.Next(n);
            return idx;
        }

        private static int[] SampleWithoutReplacement(Random rng, int n, int k)
        {
            var a = Enumerable.Range(0, n).ToArray();
            for (int i = 0; i < k; i++)
            {
                int j = rng.Next(i, n);
                (a[i], a[j]) = (a[j], a[i]);
            }

            var res = new int[k];
            Array.Copy(a, res, k);
            return res;
        }

        private static double[,] Subset(double[,] matrix, int[] indices)
        {
            int rows = indices.Length, cols = matrix.GetLength(1);
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
    }
}