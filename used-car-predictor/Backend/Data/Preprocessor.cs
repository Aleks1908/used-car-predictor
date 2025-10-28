using System;
using System.Collections.Generic;
using System.Linq;

namespace used_car_predictor.Backend.Data
{
    public static class Preprocessor
    {
        /// <summary>
        /// Builds X, y for a given targetYear.
        /// Columns match ServingHelpers.EncodeManualInput exactly:
        /// [yearOfProduction, mileageKm, ageYears, yearOffset, fuel 1hots..., transmission 1hots...]
        /// </summary>
        public static (double[, ] X, double[] y, List<string> fuels, List<string> transmissions)
            ToMatrix(IReadOnlyList<Vehicle> rows, int targetYear, int? anchorTargetYear = null)
        {
            if (rows == null || rows.Count == 0)
                return (new double[0, 0], Array.Empty<double>(), new List<string>(), new List<string>());

            // --- vocabularies (stable order) ---
            var fuels = rows
                .Select(r => NormalizeOrOther(r.Fuel))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            if (!fuels.Contains("other")) fuels.Add("other");

            var transmissions = rows
                .Select(r => NormalizeOrOther(r.Transmission))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            if (!transmissions.Contains("other")) transmissions.Add("other");

            int n = rows.Count;
            int p = 4 + fuels.Count + transmissions.Count;
            var X = new double[n, p];
            var y = new double[n];

            for (int i = 0; i < n; i++)
            {
                var r = rows[i];

                int yop = SafeYear(r);
                // If your Odometer is in MILES, use the *next* line instead:
                // int km  = (int)Math.Max(0, Math.Round((r.Odometer ?? 0) * 1.60934));
                int km  = (int)Math.Max(0, Math.Round(r.Odometer ?? 0)); // Odometer assumed in KM

                int ageYears   = Math.Max(0, targetYear - yop);
                int yearOffset = anchorTargetYear.HasValue ? targetYear - anchorTargetYear.Value : 0;

                string fuelTok = NormalizeOrOther(r.Fuel);
                string transTok = NormalizeOrOther(r.Transmission);

                int c = 0;
                X[i, c++] = yop;
                X[i, c++] = km;
                X[i, c++] = ageYears;
                X[i, c++] = yearOffset;

                // fuel one-hots
                for (int f = 0; f < fuels.Count; f++)
                    X[i, c++] = fuels[f] == fuelTok ? 1.0 : 0.0;

                // transmission one-hots
                for (int t = 0; t < transmissions.Count; t++)
                    X[i, c++] = transmissions[t] == transTok ? 1.0 : 0.0;

                y[i] = Math.Max(0.0, r.Price ?? 0.0);
            }

            return (X, y, fuels, transmissions);
        }

        private static string NormalizeOrOther(string? s)
            => string.IsNullOrWhiteSpace(s) ? "other" : s.Trim().ToLowerInvariant();

        private static int SafeYear(Vehicle r)
        {
            if (r.Year.HasValue && r.Year.Value >= 1950 && r.Year.Value <= DateTime.UtcNow.Year + 10)
                return r.Year.Value;
            // fallback: clamp a reasonable default
            return Math.Clamp(DateTime.UtcNow.Year - 5, 1990, DateTime.UtcNow.Year + 5);
        }
    }
}