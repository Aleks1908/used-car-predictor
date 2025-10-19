namespace used_car_predictor.Backend.Api;

public static class ServingHelpers
{
    public static double[] EncodeManualInput(
        int year,
        int odometer,
        string fuel,
        string transmission,
        IReadOnlyList<string> fuels,
        IReadOnlyList<string> transmissions,
        int targetYear = 2030)
    {
        var row = new double[9 + fuels.Count + transmissions.Count];

        int age = Math.Max(0, targetYear - year);
        double odo = Math.Max(0.0, odometer);
        double mileagePerYear = odo / (age + 1.0);
        double logOdometer = Math.Log(odo + 1.0);
        double age2 = age * age;

        row[0] = age;
        row[1] = odo;
        row[2] = mileagePerYear;
        row[3] = logOdometer;
        row[4] = age2;

        var f = (fuel ?? "").Trim().ToLowerInvariant();
        var t = (transmission ?? "").Trim().ToLowerInvariant();

        for (int j = 0; j < fuels.Count; j++)
            row[5 + j] = f == fuels[j] ? 1.0 : 0.0;

        int baseIdx = 5 + fuels.Count;
        for (int j = 0; j < transmissions.Count; j++)
            row[baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

        int k = baseIdx + transmissions.Count;
        row[k + 0] = age * logOdometer;
        row[k + 1] = mileagePerYear * mileagePerYear;
        row[k + 2] = age * age * age;
        row[k + 3] = mileagePerYear * mileagePerYear * mileagePerYear;

        return row;
    }

    public static double[] ScaleRow(double[] raw, double[] means, double[] stds)
    {
        if (means.Length != raw.Length || stds.Length != raw.Length)
            throw new InvalidOperationException("Feature scaler shape mismatch.");

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