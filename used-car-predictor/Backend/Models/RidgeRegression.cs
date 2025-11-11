using System;
using System.Collections.Generic;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Models
{
    public class RidgeRegression : IRegressor
    {
        private double[] _weights = Array.Empty<double>();
        private double _bias;
        private double _alpha;

        private readonly bool _useClosedForm;
        private readonly double _learningRate;
        private readonly int _epochs;

        public string Name => "Ridge Regression";

        public double[] Weights => _weights;
        public double Bias => _bias;
        public double Alpha => _alpha;

        public double TotalMs { get; private set; }
        public double MeanTrialMs { get; private set; }

        public int? TuningTrials { get; private set; }
        public double? TuningTotalMs { get; private set; }
        public double? TuningMeanTrialMs { get; private set; }

        public RidgeRegression(
            double learningRate = 1e-4,
            int epochs = 10_000,
            double lambda = 0.1,
            bool useClosedForm = true)
        {
            _learningRate = learningRate;
            _epochs = epochs;
            _alpha = Math.Max(0, lambda);
            _useClosedForm = useClosedForm;
        }

        public void Fit(double[,] features, double[] labels)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (_useClosedForm)
                FitClosedForm(features, labels, _alpha, out _weights, out _bias);
            else
                FitGradientDescent(features, labels, _alpha, _learningRate, _epochs, out _weights, out _bias);

            sw.Stop();
            TotalMs = sw.Elapsed.TotalMilliseconds;
            MeanTrialMs = TotalMs;
        }

        public double Predict(double[] featureRow)
        {
            double s = _bias;
            var w = _weights;
            for (int j = 0; j < w.Length; j++) s += w[j] * featureRow[j];
            return s;
        }

        public double[] Predict(double[,] features)
        {
            int n = features.GetLength(0);
            int p = features.GetLength(1);
            var outp = new double[n];
            var w = _weights;
            double b = _bias;

            for (int i = 0; i < n; i++)
            {
                double s = b;
                for (int j = 0; j < p; j++) s += w[j] * features[i, j];
                outp[i] = s;
            }

            return outp;
        }

        private static void FitClosedForm(double[,] X, double[] y, double alpha, out double[] w, out double b)
        {
            int n = X.GetLength(0);
            int p = X.GetLength(1);

            var meanX = new double[p];
            for (int j = 0; j < p; j++)
            {
                double s = 0;
                for (int i = 0; i < n; i++) s += X[i, j];
                meanX[j] = s / n;
            }

            double meanY = 0;
            for (int i = 0; i < n; i++) meanY += y[i];
            meanY /= n;

            var G = new double[p, p];
            var r = new double[p];

            for (int i = 0; i < n; i++)
            {
                double yc = y[i] - meanY;
                for (int j = 0; j < p; j++)
                {
                    double xj = X[i, j] - meanX[j];
                    r[j] += xj * yc;
                    for (int k = 0; k <= j; k++)
                        G[j, k] += xj * (X[i, k] - meanX[k]);
                }
            }

            for (int j = 0; j < p; j++)
            {
                for (int k = 0; k < j; k++) G[k, j] = G[j, k];
                G[j, j] += alpha;
            }

            w = SolveCholesky(G, r);

            double bb = meanY;
            for (int j = 0; j < p; j++) bb -= meanX[j] * w[j];
            b = bb;
        }

        private static double[] SolveCholesky(double[,] A, double[] b)
        {
            int n = A.GetLength(0);
            var L = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < j; k++) sum += L[i, k] * L[j, k];

                    if (i == j)
                    {
                        double v = A[i, i] - sum;
                        if (v <= 1e-12) v = 1e-12;
                        L[i, i] = Math.Sqrt(v);
                    }
                    else
                    {
                        L[i, j] = (A[i, j] - sum) / L[j, j];
                    }
                }
            }

            var y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = 0; k < i; k++) sum += L[i, k] * y[k];
                y[i] = (b[i] - sum) / L[i, i];
            }

            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int k = i + 1; k < n; k++) sum += L[k, i] * x[k];
                x[i] = (y[i] - sum) / L[i, i];
            }

            return x;
        }

        private static void FitGradientDescent(
            double[,] X, double[] y, double alpha, double lr, int epochs,
            out double[] w, out double b)
        {
            int n = X.GetLength(0), p = X.GetLength(1);
            w = new double[p];
            b = 0;

            for (int ep = 0; ep < epochs; ep++)
            {
                var gw = new double[p];
                double gb = 0;

                for (int i = 0; i < n; i++)
                {
                    double pred = b;
                    for (int j = 0; j < p; j++) pred += w[j] * X[i, j];
                    double err = pred - y[i];
                    for (int j = 0; j < p; j++) gw[j] += err * X[i, j];
                    gb += err;
                }

                for (int j = 0; j < p; j++)
                    w[j] -= lr * (gw[j] / n + alpha * w[j]);
                b -= lr * gb / n;
            }
        }

        private static double[] LogSpace(double startExp, double endExp, int steps)
        {
            var arr = new double[Math.Max(1, steps)];
            if (steps <= 1)
            {
                arr[0] = Math.Pow(10, startExp);
                return arr;
            }

            double step = (endExp - startExp) / (steps - 1);
            for (int i = 0; i < steps; i++)
                arr[i] = Math.Pow(10, startExp + i * step);
            return arr;
        }

        public static (RidgeRegression Model, double MeanTrialMs, double TotalMs, int Trials)
            TrainWithBestParamsKFold(
                double[,] X, double[] y,
                LabelScaler yScaler,
                int kFolds = 5,
                double minExp = -9, double maxExp = +3, int alphaSteps = 40,
                int? seed = null)
        {
            var alphas = LogSpace(minExp, maxExp, alphaSteps);

            int n = X.GetLength(0);
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;

            var rng = new Random(seed ?? 123);
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            int foldSize = Math.Max(1, n / kFolds);
            double bestRmse = double.PositiveInfinity;
            double bestAlpha = alphas[0];

            long tuningTotalTicks = 0;
            int trials = 0;

            foreach (var a in alphas)
            {
                double rmseSum = 0;
                int foldsUsed = 0;

                for (int f = 0; f < kFolds; f++)
                {
                    int start = f * foldSize;
                    int end = (f == kFolds - 1) ? n : Math.Min(n, start + foldSize);

                    if (start >= end) break;

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    var (tx, ty, vx, vy) = SplitByIndex(X, y, idx, start, end);

                    var rr = new RidgeRegression(lambda: a, useClosedForm: true);
                    rr.Fit(tx, ty);

                    var predScaled = rr.Predict(vx);
                    var preds = yScaler.InverseTransform(predScaled);
                    var truth = yScaler.InverseTransform(vy);

                    double rmse = Metrics.RootMeanSquaredError(truth, preds);
                    rmseSum += rmse;
                    foldsUsed++;

                    sw.Stop();
                    tuningTotalTicks += sw.ElapsedTicks;
                    trials++;
                }

                double meanRmse = rmseSum / Math.Max(1, foldsUsed);
                Console.WriteLine($"[Ridge kCV] α={a:g}, k={foldsUsed}, meanRMSE={meanRmse:F3}");

                if (meanRmse < bestRmse)
                {
                    bestRmse = meanRmse;
                    bestAlpha = a;
                }
            }

            var final = new RidgeRegression(lambda: bestAlpha, useClosedForm: true);
            final.Fit(X, y);

            double totalMs = tuningTotalTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            double meanMs = trials > 0 ? totalMs / trials : 0.0;

            final.TuningTrials = trials;
            final.TuningTotalMs = totalMs;
            final.TuningMeanTrialMs = meanMs;

            Console.WriteLine(
                $"[Ridge] α={bestAlpha:g}, Trials={trials}, MeanTrialMs={meanMs:F3}, TotalMs={totalMs:F3}");
            return (final, meanMs, totalMs, trials);
        }

        private static (double[,], double[]) Concat(double[,] x1, double[] y1, double[,] x2, double[] y2)
        {
            int n1 = x1.GetLength(0), n2 = x2.GetLength(0), p = x1.GetLength(1);
            var X = new double[n1 + n2, p];
            var y = new double[n1 + n2];

            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < p; j++) X[i, j] = x1[i, j];
                y[i] = y1[i];
            }

            for (int i = 0; i < n2; i++)
            {
                int r = n1 + i;
                for (int j = 0; j < p; j++) X[r, j] = x2[i, j];
                y[r] = y2[i];
            }

            return (X, y);
        }

        private static (double[,], double[], double[,], double[]) SplitByIndex(
            double[,] X, double[] y, int[] shuffledIdx, int valStart, int valEnd)
        {
            int n = X.GetLength(0);
            int p = X.GetLength(1);
            int valN = valEnd - valStart;
            int trainN = n - valN;

            var tx = new double[trainN, p];
            var ty = new double[trainN];
            var vx = new double[valN, p];
            var vy = new double[valN];

            for (int ii = 0; ii < valN; ii++)
            {
                int src = shuffledIdx[valStart + ii];
                for (int j = 0; j < p; j++) vx[ii, j] = X[src, j];
                vy[ii] = y[src];
            }

            int t = 0;
            for (int ii = 0; ii < n; ii++)
            {
                if (ii >= valStart && ii < valEnd) continue;
                int src = shuffledIdx[ii];
                for (int j = 0; j < p; j++) tx[t, j] = X[src, j];
                ty[t] = y[src];
                t++;
            }

            return (tx, ty, vx, vy);
        }
    }
}