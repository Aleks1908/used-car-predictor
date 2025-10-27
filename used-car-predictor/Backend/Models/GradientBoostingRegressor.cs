using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Random _rng;

        public string Name => "Gradient Boosting Regressor";
        public int BestIteration { get; private set; } = -1;

        public GradientBoostingRegressor(
            int nEstimators = 400,
            double learningRate = 0.05,
            int maxDepth = 3,
            int minSamplesSplit = 10,
            int minSamplesLeaf = 5,
            double subsample = 0.8)
        {
            _nEstimators = Math.Max(1, nEstimators);
            _learningRate = Math.Max(1e-6, learningRate);
            _maxDepth = Math.Max(1, maxDepth);
            _minSamplesSplit = Math.Max(2, minSamplesSplit);
            _minSamplesLeaf = Math.Max(1, minSamplesLeaf);
            _subsample = Math.Clamp(subsample, 0.3, 1.0);

            _rng = Random.Shared;
        }

        public void Fit(double[,] X, double[] y) => Fit(X, y, null, null, null);

        public void Fit(
            double[,] X, double[] y,
            double[,]? Xval,
            double[]? yval,
            LabelScaler? labelScaler,
            int evalEvery = 5,
            int patience = 20,
            double minDelta = 1e-6)
        {
            _trees.Clear();
            int n = X.GetLength(0);
            _init = Mean(y);

            var pred = Enumerable.Repeat(_init, n).ToArray();

            double[]? valPred = null;
            if (Xval is not null)
            {
                int nv = Xval.GetLength(0);
                valPred = Enumerable.Repeat(_init, nv).ToArray();
            }

            double bestRmse = double.PositiveInfinity;
            int lastImproveAt = -1;
            BestIteration = -1;

            for (int t = 0; t < _nEstimators; t++)
            {
                var residuals = new double[n];
                for (int i = 0; i < n; i++)
                    residuals[i] = y[i] - pred[i];

                int[] idx = SampleIndicesNoReplacement(n, _subsample);
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

                var stepPredTrain = tree.Predict(X);
                for (int i = 0; i < n; i++)
                    pred[i] += _learningRate * stepPredTrain[i];

                if (Xval is not null && valPred is not null && (t + 1) % evalEvery == 0)
                {
                    var stepPredVal = tree.Predict(Xval);
                    for (int i = 0; i < valPred.Length; i++)
                        valPred[i] += _learningRate * stepPredVal[i];

                    if (labelScaler is not null && yval is not null)
                    {
                        var preds = labelScaler.InverseTransform(valPred);
                        var truth = labelScaler.InverseTransform(yval);
                        var rmse = Metrics.RootMeanSquaredError(truth, preds);

                        if (rmse + minDelta < bestRmse)
                        {
                            bestRmse = rmse;
                            BestIteration = t + 1;
                            lastImproveAt = t;
                        }
                        else if (t - lastImproveAt >= patience)
                        {
                            if (BestIteration > 0) TruncateTrees(BestIteration);
                            break;
                        }
                    }
                }
            }

            if (BestIteration > 0 && BestIteration < _trees.Count)
                TruncateTrees(BestIteration);
        }

        public double[] Predict(double[,] X)
        {
            int n = X.GetLength(0);
            var outp = Enumerable.Repeat(_init, n).ToArray();

            foreach (var tree in _trees)
            {
                var step = tree.Predict(X);
                for (int i = 0; i < n; i++)
                    outp[i] += _learningRate * step[i];
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

        private static double Mean(double[] arr)
        {
            double sum = 0;
            foreach (var v in arr) sum += v;
            return sum / arr.Length;
        }

        private int[] SampleIndicesNoReplacement(int n, double frac)
        {
            int k = Math.Max(1, (int)Math.Round(n * frac));
            if (k >= n)
                return Enumerable.Range(0, n).ToArray();

            var idx = Enumerable.Range(0, n).ToArray();
            for (int i = 0; i < k; i++)
            {
                int j = _rng.Next(i, n);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            var res = new int[k];
            Array.Copy(idx, res, k);
            return res;
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

        private void TruncateTrees(int keep)
        {
            if (keep >= 0 && keep < _trees.Count)
                _trees.RemoveRange(keep, _trees.Count - keep);
        }

        public static GradientBoostingRegressor TrainWithBestParams(
            double[,] trainFeatures, double[] trainLabels,
            double[,] valFeatures, double[] valLabels,
            LabelScaler labelScaler,
            int maxConfigs = 60)
        {
            var rng = Random.Shared;

            int[] nEstimatorsList = { 200, 300, 400 };
            double[] learningRates = { 0.03, 0.05, 0.1 };
            int[] maxDepths = { 2, 3, 4 };
            int[] minLeaf = { 1, 3, 5, 10 };
            int[] minSplit = { 2, 10, 20 };
            double[] subsamples = { 0.6, 0.8, 1.0 };

            double bestRmse = double.PositiveInfinity;
            GradientBoostingRegressor? bestModel = null;
            Dictionary<string, object>? bestParams = null;

            for (int trial = 0; trial < maxConfigs; trial++)
            {
                var p = new Dictionary<string, object>
                {
                    ["nEstimators"] = nEstimatorsList[rng.Next(nEstimatorsList.Length)],
                    ["learningRate"] = learningRates[rng.Next(learningRates.Length)],
                    ["maxDepth"] = maxDepths[rng.Next(maxDepths.Length)],
                    ["minSamplesLeaf"] = minLeaf[rng.Next(minLeaf.Length)],
                    ["minSamplesSplit"] = minSplit[rng.Next(minSplit.Length)],
                    ["subsample"] = subsamples[rng.Next(subsamples.Length)],
                };

                var model = new GradientBoostingRegressor(
                    nEstimators: (int)p["nEstimators"],
                    learningRate: (double)p["learningRate"],
                    maxDepth: (int)p["maxDepth"],
                    minSamplesSplit: (int)p["minSamplesSplit"],
                    minSamplesLeaf: (int)p["minSamplesLeaf"],
                    subsample: (double)p["subsample"]
                );

                model.Fit(trainFeatures, trainLabels, valFeatures, valLabels, labelScaler,
                    evalEvery: 5, patience: 20);

                var valScaled = model.Predict(valFeatures);
                var preds = labelScaler.InverseTransform(valScaled);
                var truth = labelScaler.InverseTransform(valLabels);

                var rmse = Metrics.RootMeanSquaredError(truth, preds);
                var mae = Metrics.MeanAbsoluteError(truth, preds);
                var r2 = Metrics.RSquared(truth, preds);

                Console.WriteLine(
                    $"[GB tune] try={trial + 1}/{maxConfigs} " +
                    $"nEst={p["nEstimators"]}, lr={p["learningRate"]}, " +
                    $"maxDepth={p["maxDepth"]}, minSplit={p["minSamplesSplit"]}, " +
                    $"minLeaf={p["minSamplesLeaf"]}, subsample={p["subsample"]} -> " +
                    $"RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}, bestIt={model.BestIteration}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                    bestParams = p;
                }
            }

            if (bestModel == null)
                throw new InvalidOperationException("GB randomized search failed.");

            Console.WriteLine(
                $"[GB Best] nEst={bestParams!["nEstimators"]}, lr={bestParams!["learningRate"]}, " +
                $"maxDepth={bestParams!["maxDepth"]}, minSplit={bestParams!["minSamplesSplit"]}, " +
                $"minLeaf={bestParams!["minSamplesLeaf"]}, subsample={bestParams!["subsample"]}, " +
                $"bestIt={bestModel.BestIteration}");

            return bestModel;
        }
    }
}