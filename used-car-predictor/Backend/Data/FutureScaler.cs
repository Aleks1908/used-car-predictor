namespace used_car_predictor.Backend.Data;

public class FeatureScaler
{
    private double[] means = Array.Empty<double>();
    private double[] stds = Array.Empty<double>();

    public double[,] FitTransform(double[,] features)
    {
        int sampleCount = features.GetLength(0);
        int featureCount = features.GetLength(1);

        means = new double[featureCount];
        stds = new double[featureCount];

        var scaled = new double[sampleCount, featureCount];

        for (int j = 0; j < featureCount; j++)
        {
            // mean
            double sum = 0;
            for (int i = 0; i < sampleCount; i++) sum += features[i, j];
            means[j] = sum / sampleCount;

            // std
            double variance = 0;
            for (int i = 0; i < sampleCount; i++)
                variance += Math.Pow(features[i, j] - means[j], 2);
            stds[j] = Math.Sqrt(variance / sampleCount);

            // scale
            for (int i = 0; i < sampleCount; i++)
                scaled[i, j] = stds[j] > 0 ? (features[i, j] - means[j]) / stds[j] : 0;
        }

        return scaled;
    }

    public double[] TransformRow(double[] row)
    {
        var result = new double[row.Length];
        for (int j = 0; j < row.Length; j++)
            result[j] = stds[j] > 0 ? (row[j] - means[j]) / stds[j] : 0;
        return result;
    }
}