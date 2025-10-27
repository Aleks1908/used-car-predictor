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

        // Keep GD knobs for compatibility; default to closed-form.
        private readonly bool _useClosedForm;
        private readonly double _learningRate;
        private readonly int _epochs;

        public string Name => "Ridge Regression";

        // Expose for persistence/inspection
        public double[] Weights => _weights;
        public double Bias => _bias;
        public double Alpha => _alpha;

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
            if (_useClosedForm)
                FitClosedForm(features, labels, _alpha, out _weights, out _bias);
            else
                FitGradientDescent(features, labels, _alpha, _learningRate, _epochs, out _weights, out _bias);
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

        // ---------- Closed-form ridge: (Xc'Xc + λI) w = Xc' yc, b = ȳ − x̄·w ----------
        private static void FitClosedForm(double[,] X, double[] y, double alpha, out double[] w, out double b)
        {
            int n = X.GetLength(0);
            int p = X.GetLength(1);

            // Means
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

            // Build Gram (Xc'Xc) and rhs (Xc'yc)
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

            // Symmetrize + regularize
            for (int j = 0; j < p; j++)
            {
                for (int k = 0; k < j; k++) G[k, j] = G[j, k];
                G[j, j] += alpha; // ridge penalty (bias NOT penalized)
            }

            // Solve (SPD) via Cholesky
            w = SolveCholesky(G, r);

            // Intercept
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
                        if (v <= 1e-12) v = 1e-12; // numerical floor
                        L[i, i] = Math.Sqrt(v);
                    }
                    else
                    {
                        L[i, j] = (A[i, j] - sum) / L[j, j];
                    }
                }
            }

            // Forward solve: L y = b
            var y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = 0; k < i; k++) sum += L[i, k] * y[k];
                y[i] = (b[i] - sum) / L[i, i];
            }

            // Back solve: L^T x = y
            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int k = i + 1; k < n; k++) sum += L[k, i] * x[k];
                x[i] = (y[i] - sum) / L[i, i];
            }

            return x;
        }

        // ---------- Optional GD path (kept for completeness) ----------
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
                    w[j] -= lr * (gw[j] / n + alpha * w[j]); // bias not penalized
                b -= lr * gb / n;
            }
        }

        // ---------- Hyperparameter search (log λ grid, closed-form) ----------
        public static RidgeRegression TrainWithBestParams(
            double[,] tx, double[] ty,
            double[,] vx, double[] vy,
            LabelScaler yScaler)
        {
            // Log grid for λ; exclude 0 to keep Gram SPD.
            double[] alphas = { 1e-8, 1e-7, 1e-6, 1e-5, 1e-4, 1e-3, 5e-3, 1e-2, 5e-2, 0.1, 0.2, 0.5, 1.0, 2.0 };

            double bestRmse = double.PositiveInfinity;
            double bestAlpha = alphas[0];
            RidgeRegression? bestModel = null;

            foreach (var a in alphas)
            {
                var rr = new RidgeRegression(lambda: a, useClosedForm: true);
                rr.Fit(tx, ty);

                var predScaled = rr.Predict(vx);
                var preds = yScaler.InverseTransform(predScaled);
                var truth = yScaler.InverseTransform(vy);

                double rmse = Metrics.RootMeanSquaredError(truth, preds);
                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestAlpha = a;
                    bestModel = rr;
                }
            }

            if (bestModel == null)
                throw new InvalidOperationException("Ridge tuning failed.");

            var (fullX, fullY) = Concat(tx, ty, vx, vy);
            var final = new RidgeRegression(lambda: bestAlpha, useClosedForm: true);
            final.Fit(fullX, fullY);
            return final;
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
    }
}