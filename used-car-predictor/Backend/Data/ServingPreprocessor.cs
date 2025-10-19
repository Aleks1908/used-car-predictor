namespace used_car_predictor.Backend.Data;

public static class ServingPreprocessor
{
    public static double[] BuildRawFeatureRow(
        string fuel, string transmission, int vehicleYear, double odometerKm, int targetYear,
        IReadOnlyList<string> fuels, IReadOnlyList<string> transmissions)
    {
        fuel = (fuel ?? "").Trim().ToLowerInvariant();
        transmission = (transmission ?? "").Trim().ToLowerInvariant();

        var age = Math.Max(0, targetYear - vehicleYear);
        var age2 = age * age;
        var logOdo = Math.Log(Math.Max(0.0, odometerKm) + 1.0);
        var mileagePerYear = age > 0 ? odometerKm / age : odometerKm;

        var cols = new List<double>(9 + fuels.Count + transmissions.Count)
        {
            mileagePerYear,
            logOdo,
            age,
            mileagePerYear * age,
            age2
        };

        for (int j = 0; j < fuels.Count; j++)
            cols.Add(fuels[j] == fuel ? 1.0 : 0.0);

        for (int j = 0; j < transmissions.Count; j++)
            cols.Add(transmissions[j] == transmission ? 1.0 : 0.0);

        cols.Add(age * logOdo);
        cols.Add(mileagePerYear * mileagePerYear);
        cols.Add(age * age * age);
        cols.Add(mileagePerYear * mileagePerYear * mileagePerYear);

        return cols.ToArray();
    }

    public static double[] ScaleFeatureRow(double[] raw, double[] means, double[] stds)
    {
        var x = new double[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            x[i] = stds[i] > 0 ? (raw[i] - means[i]) / stds[i] : 0.0;
        return x;
    }

    public static double InverseLabel(double yScaled, double mean, double std, bool useLog)
    {
        var unscaled = yScaled * std + mean;
        return useLog ? Math.Exp(unscaled) - 1.0 : unscaled;
    }
}