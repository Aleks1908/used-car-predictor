using System;
using System.Linq;
namespace used_car_predictor.Backend.Models;

public class LinearRegression(double learningRate = 0.01, int epochs = 10000) : IRegressor
{
   private double[] _weights = [];
    private double _bias;

    public string Name => "Linear Regression";

    public void Fit(double[,] features, double[] labels) 
    {
        var sampleCount = features.GetLength(0);
        var featureCount = features.GetLength(1);

        _weights = new double[featureCount];
        _bias = 0;

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            var gradients = new double[featureCount];
            double biasGradient = 0;

            for (var i = 0; i < sampleCount; i++)
            {
                var prediction = _bias;
                for (var j = 0; j < featureCount; j++)
                {
                    prediction += _weights[j] * features[i, j];
                }

                var error = prediction - labels[i];

                for (var j = 0; j < featureCount; j++)
                {
                    gradients[j] += error * features[i, j];
                }
                biasGradient += error;
            }
            
            for (var j = 0; j < featureCount; j++)
            {
                _weights[j] -= learningRate * gradients[j] / sampleCount;
            }
            _bias -= learningRate * biasGradient / sampleCount;

            if (epoch % 1000 == 0)
            {
                Console.WriteLine($"Epoch {epoch}, Bias={_bias:F2}, First Weight={_weights[0]:F6}");
            }
        }
    }

    public double Predict(double[] featureRow)
    {
        return _bias + _weights.Select((t, j) => t * featureRow[j]).Sum();
    }

    public double[] Weights => [.._weights]; // collection expression copy
    public double[] Parameters => [.._weights, _bias]; // weights plus bias via collection expression

    public double[] Predict(double[,] features)
    {
        var sampleCount = features.GetLength(0);
        var predictions = new double[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            double[] row = [..Enumerable.Range(0, features.GetLength(1)).Select(j => features[i, j])];
            predictions[i] = Predict(row);
        }

        return predictions;
    }
}