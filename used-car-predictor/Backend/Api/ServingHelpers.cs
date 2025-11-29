namespace used_car_predictor.Backend.Api;

/// Contains helper functions for encoding and scaling manual API inputs
/// into the same numerical format used during model training.
public static class ServingHelpers
{
    /// Encodes a single user input
    /// into a feature vector compatible with the model's training schema.
    /// Includes numerical and one-hot categorical features.
    public static double[] EncodeManualInput(
        int yearOfProduction, int mileageKm, string fuel, string transmission,
        IReadOnlyList<string> fuels, IReadOnlyList<string> transmissions,
        int targetYear,
        int? anchorTargetYear = null)
    {
        var ageYears = Math.Max(0, targetYear - yearOfProduction);
        var yearOffset = anchorTargetYear.HasValue ? targetYear - anchorTargetYear.Value : 0;

        var feat = new List<double>(4 + fuels.Count + transmissions.Count)
        {
            yearOfProduction,
            mileageKm,
            ageYears,
            yearOffset
        };

        for (int i = 0; i < fuels.Count; i++)
            feat.Add(string.Equals(fuel, fuels[i], StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);

        for (int i = 0; i < transmissions.Count; i++)
            feat.Add(string.Equals(transmission, transmissions[i], StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);

        return feat.ToArray();
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