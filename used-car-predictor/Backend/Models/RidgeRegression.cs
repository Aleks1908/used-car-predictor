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
            return _bias + _weights.Select((t, j) => t * featureRow[j]).Sum();
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
            double[,] valFeatures,   double[] valLabels,
            LabelScaler labelScaler)
        {
            var lambdas = new double[] { 1e-6, 1e-5, 1e-4, 0.001, 0.01, 0.1, 1 };
            var learningRates = new double[] { 1e-4, 1e-5, 1e-6 };
            var epochs = 10000;

            var paramGrid = new List<Dictionary<string, object>>();
            foreach (var lr in learningRates)
                for (var i = 0; i < lambdas.Length; i++)
                {
                    var lam = lambdas[i];
                    paramGrid.Add(new Dictionary<string, object>
                    {
                        { "learningRate", lr },
                        { "epochs", epochs },
                        { "lambda", lam }
                    });
                }

            // Use the generic search utility
            var (bestModel, bestRmse) = HyperparamSearch.GridSearch(
                paramSet => new RidgeRegression(
                    (double)paramSet["learningRate"],
                    (int)paramSet["epochs"],
                    (double)paramSet["lambda"]),
                paramGrid,
                trainFeatures, trainLabels,
                valFeatures, valLabels,
                labelScaler
            );

            Console.WriteLine($"[Ridge] Best model chosen with RMSE={bestRmse:F2}");

            return (RidgeRegression)bestModel;
        }
    }
}
