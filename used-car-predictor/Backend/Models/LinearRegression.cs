namespace used_car_predictor.Backend.Models;

public class LinearRegression
{
    private double[] weights = Array.Empty<double>();
    private double bias;

    private readonly double learningRate;
    private readonly int epochs;

    public LinearRegression(double learningRate = 0.0000001, int epochs = 10000)
    {
        this.learningRate = learningRate;
        this.epochs = epochs;
    }

    public void Fit(double[,] features, double[] labels)
    {
        int sampleCount = features.GetLength(0);
        int featureCount = features.GetLength(1);

        weights = new double[featureCount];
        bias = 0;

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double[] gradients = new double[featureCount];
            double biasGradient = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                double prediction = bias;
                for (int j = 0; j < featureCount; j++)
                {
                    prediction += weights[j] * features[i, j];
                }

                double error = prediction - labels[i];

                for (int j = 0; j < featureCount; j++)
                {
                    gradients[j] += error * features[i, j];
                }
                biasGradient += error;
            }
            
            for (int j = 0; j < featureCount; j++)
            {
                weights[j] -= learningRate * gradients[j] / sampleCount;
            }
            bias -= learningRate * biasGradient / sampleCount;

            if (epoch % 1000 == 0)
            {
                Console.WriteLine($"Epoch {epoch}, Bias={bias:F2}, First Weight={weights[0]:F6}");
            }
        }
    }

    public double Predict(double[] featureRow)
    {
        double result = bias;
        for (int j = 0; j < weights.Length; j++)
        {
            result += weights[j] * featureRow[j];
        }
        return result;
    }

    public double[] Predict(double[,] features)
    {
        int sampleCount = features.GetLength(0);
        var predictions = new double[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            double[] row = new double[features.GetLength(1)];
            for (int j = 0; j < row.Length; j++)
            {
                row[j] = features[i, j];
            }
            predictions[i] = Predict(row);
        }

        return predictions;
    }
}