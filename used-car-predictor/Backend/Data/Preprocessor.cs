namespace used_car_predictor.Backend.Data;

public static class Preprocessor
{
    public static (double[,], double[], List<string>, List<string>) ToMatrix(List<Vehicle> vehicles)
    {
        int sampleCount = vehicles.Count;

        var fuels = vehicles
            .Where(v => !string.IsNullOrWhiteSpace(v.Fuel))
            .Select(v => v.Fuel!.Trim().ToLower())
            .Distinct()
            .OrderBy(f => f)
            .ToList();

        var transmissions = vehicles
            .Where(v => !string.IsNullOrWhiteSpace(v.Transmission))
            .Select(v => v.Transmission!.Trim().ToLower())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        int featureCount = 2 + fuels.Count + transmissions.Count;

        var features = new double[sampleCount, featureCount];
        var labels = new double[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            var v = vehicles[i];

            features[i, 0] = v.Year ?? 0;
            features[i, 1] = v.Odometer ?? 0;

            for (int j = 0; j < fuels.Count; j++)
            {
                features[i, 2 + j] = (v.Fuel?.Trim().ToLower() == fuels[j]) ? 1 : 0;
            }

            for (int j = 0; j < transmissions.Count; j++)
            {
                features[i, 2 + fuels.Count + j] =
                    (v.Transmission?.Trim().ToLower() == transmissions[j]) ? 1 : 0;
            }

            labels[i] = v.Price ?? 0;
        } 
        return (features, labels, fuels, transmissions);
    }
}