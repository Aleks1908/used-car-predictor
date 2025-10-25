using System;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Models
{
    public class RidgeRegression(double learningRate = 0.0000001, int epochs = 10000, double lambda = 0.1)
        : IRegressor
    {
        private double[] _weights = [];
        private double _bias;

        public string Name => "Ridge Regression";

        private int Epochs { get; } = epochs;

        public void Fit(double[,] features, double[] labels)
        {
            var sampleCount = features.GetLength(0);
            var featureCount = features.GetLength(1);

            _weights = new double[featureCount];
            _bias = 0;

            for (var epoch = 0; epoch < Epochs; epoch++)
            {
                var gradients = new double[featureCount];
                double biasGradient = 0;

                for (var i = 0; i < sampleCount; i++)
                {
                    var prediction = _bias;
                    for (var j = 0; j < featureCount; j++)
                        prediction += _weights[j] * features[i, j];

                    var error = prediction - labels[i];

                    for (var j = 0; j < featureCount; j++)
                        gradients[j] += error * features[i, j] + lambda * _weights[j];

                    biasGradient += error;
                }

                for (var j = 0; j < featureCount; j++)
                    _weights[j] -= learningRate * gradients[j] / sampleCount;

                _bias -= learningRate * biasGradient / sampleCount;
            }
        }

        public double Predict(double[] featureRow)
        {
            double sum = _bias;
            for (int j = 0; j < _weights.Length; j++) sum += _weights[j] * featureRow[j];
            return sum;
        }

        public double[] Predict(double[,] features)
        {
            var sampleCount = features.GetLength(0);
            var predictions = new double[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var row = new double[features.GetLength(1)];
                for (var j = 0; j < row.Length; j++)
                    row[j] = features[i, j];
                predictions[i] = Predict(row);
            }

            return predictions;
        }


        public static RidgeRegression TrainWithBestParams(
            double[,] trainFeatures, double[] trainLabels,
            double[,] valFeatures, double[] valLabels,
            LabelScaler labelScaler)
        {
            var lambdas = new double[] { 1e-7, 1e-6, 1e-5, 1e-4, 1e-3, 1e-2, 0.1, 1 };
            var learningRates = new double[] { 1e-4, 5e-5 };
            var epochs = 10_000;

            var paramGrid = new List<Dictionary<string, object>>();
            foreach (var lr in learningRates)
            foreach (var lam in lambdas)
                paramGrid.Add(new Dictionary<string, object>
                {
                    { "learningRate", lr }, { "epochs", epochs }, { "lambda", lam }
                });

            var (bestModel, bestRmse, bestParams) = HyperparamSearch.GridSearch(
                p => new RidgeRegression((double)p["learningRate"], (int)p["epochs"], (double)p["lambda"]),
                paramGrid, trainFeatures, trainLabels, valFeatures, valLabels, labelScaler
            );

            Console.WriteLine(
                $"[Ridge tune] Best RMSE={bestRmse:F2} (lr={(double)bestParams["learningRate"]:g}, λ={(double)bestParams["lambda"]:g})");

            var (fullX, fullY) = Concat(trainFeatures, trainLabels, valFeatures, valLabels);
            var finalRidge = new RidgeRegression((double)bestParams["learningRate"], (int)bestParams["epochs"],
                (double)bestParams["lambda"]);
            finalRidge.Fit(fullX, fullY);
            return finalRidge;

            static (double[,], double[]) Concat(double[,] X1, double[] y1, double[,] X2, double[] y2)
            {
                int n1 = X1.GetLength(0), n2 = X2.GetLength(0), p = X1.GetLength(1);
                var X = new double[n1 + n2, p];
                var y = new double[n1 + n2];
                for (int i = 0; i < n1; i++)
                {
                    for (int j = 0; j < p; j++) X[i, j] = X1[i, j];
                    y[i] = y1[i];
                }

                for (int i = 0; i < n2; i++)
                {
                    int r = n1 + i;
                    for (int j = 0; j < p; j++) X[r, j] = X2[i, j];
                    y[r] = y2[i];
                }

                return (X, y);
            }
        }
    }
}