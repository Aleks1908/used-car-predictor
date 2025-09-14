namespace used_car_predictor.Backend.Data;

public class LabelScaler
{
    private double mean;
    private double std;

    public double[] FitTransform(double[] labels)
    {
        mean = labels.Average();
        std = Math.Sqrt(labels.Select(v => Math.Pow(v - mean, 2)).Average());

        var scaled = new double[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            scaled[i] = (labels[i] - mean) / std;
        }
        return scaled;
    }

    public double[] InverseTransform(double[] scaledLabels)
    {
        var unscaled = new double[scaledLabels.Length];
        for (int i = 0; i < scaledLabels.Length; i++)
        {
            unscaled[i] = scaledLabels[i] * std + mean;
        }
        return unscaled;
    }

    public double InverseTransform(double scaledValue) =>
        scaledValue * std + mean;
}