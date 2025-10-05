namespace used_car_predictor.Backend.Data;

public static class Preprocessor
{
    public static (double[,] X, double[] y, List<string> fuels, List<string> transmissions)
        ToMatrix(List<Vehicle> rows, int targetYear = 2025)
    {
        // vocab
        var fuels = rows.Select(r => (r.Fuel ?? "").Trim().ToLower())
            .Distinct().Where(s => s != "").OrderBy(s => s).ToList();
        var transmissions = rows.Select(r => (r.Transmission ?? "").Trim().ToLower())
            .Distinct().Where(s => s != "").OrderBy(s => s).ToList();

        int n = rows.Count;
        int p = 5 + fuels.Count + transmissions.Count; // 5 = age, odometer, mileagePerYear, logOdometer, age2
        var X = new double[n, p];
        var y = new double[n];

        for (int i = 0; i < n; i++)
        {
            var r = rows[i];

            int age = targetYear - (r.Year ?? targetYear);
            if (age < 0) age = 0; // guard
            double odo = r.Odometer ?? 0;

            double mileagePerYear = odo / (age + 1.0);
            double logOdometer = Math.Log(odo + 1.0);
            double age2 = age * age;

            // base features
            X[i, 0] = age;
            X[i, 1] = odo;
            X[i, 2] = mileagePerYear;
            X[i, 3] = logOdometer;
            X[i, 4] = age2;

            // 1-hot fuel
            var f = (r.Fuel ?? "").Trim().ToLower();
            for (int j = 0; j < fuels.Count; j++)
                X[i, 5 + j] = (f == fuels[j]) ? 1.0 : 0.0;

            // 1-hot transmission
            var t = (r.Transmission ?? "").Trim().ToLower();
            int baseIdx = 5 + fuels.Count;
            for (int j = 0; j < transmissions.Count; j++)
                X[i, baseIdx + j] = (t == transmissions[j]) ? 1.0 : 0.0;

            y[i] = r.Price ?? 0;
        }

        return (X, y, fuels, transmissions);
    }
}