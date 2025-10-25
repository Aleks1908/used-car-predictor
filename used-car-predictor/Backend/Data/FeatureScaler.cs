namespace used_car_predictor.Backend.Data;

public class FeatureScaler
{
    private double[] means = Array.Empty<double>();
    private double[] stds = Array.Empty<double>();

    public double[,] FitTransform(double[,] features)
    {
        int n = features.GetLength(0);
        int m = features.GetLength(1);

        means = new double[m];
        stds = new double[m];

        var scaled = new double[n, m];

        for (int j = 0; j < m; j++)
        {
            double sum = 0;
            for (int i = 0; i < n; i++) sum += features[i, j];
            means[j] = sum / n;

            double variance = 0;
            for (int i = 0; i < n; i++)
                variance += Math.Pow(features[i, j] - means[j], 2);
            stds[j] = Math.Sqrt(variance / n);

            for (int i = 0; i < n; i++)
                scaled[i, j] = stds[j] > 0 ? (features[i, j] - means[j]) / stds[j] : 0.0;
        }

        return scaled;
    }


    public double[,] Transform(double[,] features)
    {
        if (means.Length == 0 || stds.Length == 0)
            throw new InvalidOperationException("FeatureScaler not fitted. Call FitTransform(...) first.");

        int n = features.GetLength(0);
        int m = features.GetLength(1);
        if (m != means.Length)
            throw new InvalidOperationException($"Feature count mismatch: got {m}, expected {means.Length}");

        var scaled = new double[n, m];
        for (int j = 0; j < m; j++)
        {
            var mean = means[j];
            var std = stds[j];
            for (int i = 0; i < n; i++)
                scaled[i, j] = std > 0 ? (features[i, j] - mean) / std : 0.0;
        }

        return scaled;
    }

    public double[] TransformRow(double[] row)
    {
        if (means.Length == 0 || stds.Length == 0)
            throw new InvalidOperationException("FeatureScaler not fitted. Call FitTransform(...) first.");

        if (row.Length != means.Length)
            throw new InvalidOperationException($"Feature count mismatch: got {row.Length}, expected {means.Length}");

        var result = new double[row.Length];
        for (int j = 0; j < row.Length; j++)
            result[j] = stds[j] > 0 ? (row[j] - means[j]) / stds[j] : 0.0;
        return result;
    }
}