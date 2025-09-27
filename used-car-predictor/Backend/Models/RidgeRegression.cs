using System;
using System.Linq;

namespace used_car_predictor.Backend.Models
{
    // Converted to primary constructor
    public class RidgeRegression(double learningRate = 0.0000001, int epochs = 10000, double lambda = 0.1) : IRegressor
    {
        private double[] weights = [];
        private double bias;

        public string Name => "Ridge Regression";

        public void Fit(double[,] features, double[] labels)
        {
            var sampleCount = features.GetLength(0);
            var featureCount = features.GetLength(1);

            weights = new double[featureCount];
            bias = 0;

            for (var epoch = 0; epoch < epochs; epoch++)
            {
                var gradients = new double[featureCount];
                var biasGradient = 0d;

                for (var i = 0; i < sampleCount; i++)
                {
                    var prediction = bias;
                    for (var j = 0; j < featureCount; j++)
                        prediction += weights[j] * features[i, j];

                    var error = prediction - labels[i];

                    for (var j = 0; j < featureCount; j++)
                        gradients[j] += error * features[i, j] + lambda * weights[j];

                    biasGradient += error;
                }

                for (var j = 0; j < featureCount; j++)
                    weights[j] -= learningRate * gradients[j] / sampleCount;

                bias -= learningRate * biasGradient / sampleCount;

                if (epoch % 1000 == 0)
                {
                    Console.WriteLine($"[Ridge] Epoch {epoch}, Bias={bias:F2}, First Weight={weights[0]:F6}");
                }
            }
        }

        public double Predict(double[] featureRow)
        {
            return bias + Enumerable.Range(0, weights.Length).Sum(j => weights[j] * featureRow[j]);
        }

        public double[] Predict(double[,] features)
        {
            var sampleCount = features.GetLength(0);
            var featureCount = features.GetLength(1);
            return Enumerable.Range(0, sampleCount)
                .Select(i => Predict(Enumerable.Range(0, featureCount)
                                               .Select(j => features[i, j])
                                               .ToArray()))
                .ToArray();
        }
    }
}
