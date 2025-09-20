namespace used_car_predictor.Backend.Evaluation;

public static class Metrics
{
    public static double MeanAbsoluteError(double[] actual, double[] predicted) =>
        actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average();

    public static double RootMeanSquaredError(double[] actual, double[] predicted)
    {
        var mse = actual.Zip(predicted, (a, p) => Math.Pow(a - p, 2)).Average();
        return Math.Sqrt(mse);
    }

    public static double RSquared(double[] actual, double[] predicted)
    {
        double mean = actual.Average();
        double ssTot = actual.Sum(a => Math.Pow(a - mean, 2));
        double ssRes = actual.Zip(predicted, (a, p) => Math.Pow(a - p, 2)).Sum();
        return 1 - (ssRes / ssTot);
    }
}