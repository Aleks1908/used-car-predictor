using System;
using System.Collections.Generic;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Models
{
    public class GradientBoostingRegressor : IRegressor
    {
        private readonly int _nEstimators;
        private readonly double _learningRate;
        private readonly int _maxDepth;
        private readonly int _minSamplesSplit;
        private readonly int _minSamplesLeaf;
        private readonly double _subsample;

        private readonly List<DecisionTreeRegressor> _trees = new();
        private double _init;
        private Random _rng;

        public string Name => "Gradient Boosting Regressor";

        public GradientBoostingRegressor(
            int nEstimators = 200,
            double learningRate = 0.1,
            int maxDepth = 3,
            int minSamplesSplit = 10,
            int minSamplesLeaf = 5,
            double subsample = 1.0,
            int randomSeed = 42)
        {
            _nEstimators = nEstimators;
            _learningRate = learningRate;
            _maxDepth = maxDepth;
            _minSamplesSplit = minSamplesSplit;
            _minSamplesLeaf = minSamplesLeaf;
            _subsample = Math.Clamp(subsample, 0.3, 1.0);
            _rng = new Random(randomSeed);
        }

        public void Fit(double[,] X, double[] y)
        {
            _trees.Clear();
            int n = X.GetLength(0);
            _init = Mean(y);

            var pred = new double[n];
            for (int i = 0; i < n; i++) pred[i] = _init;

            for (int t = 0; t < _nEstimators; t++)
            {
                var residuals = new double[n];
                for (int i = 0; i < n; i++)
                    residuals[i] = y[i] - pred[i];

                int[] idx = SampleIndices(n, _subsample);
                var Xt = Subset(X, idx);
                var rt = Subset(residuals, idx);

                var tree = new DecisionTreeRegressor(
                    maxDepth: _maxDepth,
                    minSamplesSplit: _minSamplesSplit,
                    minSamplesLeaf: _minSamplesLeaf,
                    maxSplitsPerFeature: 32
                );
                tree.Fit(Xt, rt);
                _trees.Add(tree);

                var stepPred = tree.Predict(X);
                for (int i = 0; i < n; i++)
                    pred[i] += _learningRate * stepPred[i];
            }
        }

        public double[] Predict(double[,] X)
        {
            int n = X.GetLength(0);
            var outp = new double[n];
            for (int i = 0; i < n; i++) outp[i] = _init;

            foreach (var tree in _trees)
            {
                var step = tree.Predict(X);
                for (int i = 0; i < n; i++) outp[i] += _learningRate * step[i];
            }

            return outp;
        }

        public double Predict(double[] row)
        {
            double y = _init;
            foreach (var tree in _trees)
                y += _learningRate * tree.Predict(row);
            return y;
        }

        // ---------- Helpers ----------
        private static double Mean(double[] arr)
        {
            double sum = 0;
            foreach (var v in arr) sum += v;
            return sum / arr.Length;
        }

        private int[] SampleIndices(int n, double frac)
        {
            int k = (int)Math.Max(1, Math.Round(n * frac));
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = _rng.Next(n);
            return idx;
        }

        private static double[,] Subset(double[,] X, int[] idx)
        {
            int r = idx.Length, c = X.GetLength(1);
            var B = new double[r, c];
            for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
                B[i, j] = X[idx[i], j];
            return B;
        }

        private static double[] Subset(double[] arr, int[] idx)
        {
            var b = new double[idx.Length];
            for (int i = 0; i < idx.Length; i++) b[i] = arr[idx[i]];
            return b;
        }

        public static GradientBoostingRegressor TrainWithBestParams(
            double[,] trainFeatures, double[] trainLabels,
            double[,] valFeatures, double[] valLabels,
            LabelScaler labelScaler)
        {
            // Define search grid
            int[] nEstimatorsList = { 100, 150 };
            double[] learningRates = { 0.05, 0.1, 0.2 };
            int[] maxDepths = { 2, 3, 4 };
            int[] minLeaf = { 5, 10, 20 };

            double bestRmse = double.PositiveInfinity;
            GradientBoostingRegressor? bestModel = null;
            Dictionary<string, object>? bestParams = null;

            var paramGrid = new List<Dictionary<string, object>>();
            foreach (var nEst in nEstimatorsList)
            foreach (var lr in learningRates)
            foreach (var md in maxDepths)
            foreach (var ml in minLeaf)
            {
                paramGrid.Add(new Dictionary<string, object>
                {
                    { "nEstimators", nEst },
                    { "learningRate", lr },
                    { "maxDepth", md },
                    { "minSamplesLeaf", ml }
                });
            }

            // Grid search loop
            foreach (var paramSet in paramGrid)
            {
                var model = new GradientBoostingRegressor(
                    nEstimators: (int)paramSet["nEstimators"],
                    learningRate: (double)paramSet["learningRate"],
                    maxDepth: (int)paramSet["maxDepth"],
                    minSamplesLeaf: (int)paramSet["minSamplesLeaf"]
                );

                model.Fit(trainFeatures, trainLabels);

                var scaledPreds = model.Predict(valFeatures);
                var preds = labelScaler.InverseTransform(scaledPreds);
                var trueVals = labelScaler.InverseTransform(valLabels);

                var rmse = Metrics.RootMeanSquaredError(trueVals, preds);
                var mae = Metrics.MeanAbsoluteError(trueVals, preds);
                var r2 = Metrics.RSquared(trueVals, preds);

                Console.WriteLine($"[GB tune] nEst={paramSet["nEstimators"]}, lr={paramSet["learningRate"]}, " +
                                  $"maxDepth={paramSet["maxDepth"]}, minLeaf={paramSet["minSamplesLeaf"]} " +
                                  $"-> RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                    bestParams = paramSet;
                }
            }

            if (bestModel == null) throw new InvalidOperationException("GB grid search failed.");

            Console.WriteLine($"[GridSearch] Best params: nEstimators={bestParams!["nEstimators"]}, " +
                              $"learningRate={bestParams!["learningRate"]}, maxDepth={bestParams!["maxDepth"]}, " +
                              $"minLeaf={bestParams!["minSamplesLeaf"]}");

            // 🔹 Retrain best model on full train+val
            var finalGb = new GradientBoostingRegressor(
                nEstimators: (int)bestParams!["nEstimators"],
                learningRate: (double)bestParams!["learningRate"],
                maxDepth: (int)bestParams!["maxDepth"],
                minSamplesLeaf: (int)bestParams!["minSamplesLeaf"]
            );

            finalGb.Fit(trainFeatures, trainLabels);
            return finalGb;
        }
    }
}