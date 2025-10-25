namespace used_car_predictor.Backend.Data;

public static class Preprocessor
{
    public static (double[,] X, double[] y, List<string> fuels, List<string> transmissions)
        ToMatrix(List<Vehicle> rows, int targetYear = 2025)
    {
        var fuels = rows.Select(r => (r.Fuel ?? "").Trim().ToLower())
            .Distinct().Where(s => s != "").OrderBy(s => s).ToList();
        var transmissions = rows.Select(r => (r.Transmission ?? "").Trim().ToLower())
            .Distinct().Where(s => s != "").OrderBy(s => s).ToList();

        int n = rows.Count;
        int p = 9 + fuels.Count + transmissions.Count;
        var X = new double[n, p];
        var y = new double[n];

        for (int i = 0; i < n; i++)
        {
            var r = rows[i];

            int age = targetYear - (r.Year ?? targetYear);
            if (age < 0) age = 0;
            double odo = r.Odometer ?? 0;

            double mileagePerYear = odo / (age + 1.0);
            double logOdometer = Math.Log(odo + 1.0);
            double age2 = age * age;

            X[i, 0] = age;
            X[i, 1] = odo;
            X[i, 2] = mileagePerYear;
            X[i, 3] = logOdometer;
            X[i, 4] = age2;

            X[i, 5 + fuels.Count + transmissions.Count] = age * logOdometer;
            X[i, 6 + fuels.Count + transmissions.Count] = Math.Pow(mileagePerYear, 2);
            X[i, 7 + fuels.Count + transmissions.Count] = Math.Pow(age, 3);
            X[i, 8 + fuels.Count + transmissions.Count] = Math.Pow(mileagePerYear, 3);

            var f = (r.Fuel ?? "").Trim().ToLower();
            for (int j = 0; j < fuels.Count; j++)
                X[i, 5 + j] = f == fuels[j] ? 1.0 : 0.0;

            var t = (r.Transmission ?? "").Trim().ToLower();
            int baseIdx = 5 + fuels.Count;
            for (int j = 0; j < transmissions.Count; j++)
                X[i, baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

            y[i] = r.Price ?? 0;
        }

        return (X, y, fuels, transmissions);
    }
}