using System;
using System.Linq;

namespace used_car_predictor.Backend.Evaluation
{
    public static class DebugChecks
    {
        public static void CheckArrays(string tag, double[] truth, double[] preds)
        {
            if (truth.Length != preds.Length)
                throw new InvalidOperationException(
                    $"{tag}: length mismatch: truth={truth.Length}, preds={preds.Length}");

            bool anyNaN = truth.Any(double.IsNaN) || preds.Any(double.IsNaN);
            bool anyInf = truth.Any(double.IsInfinity) || preds.Any(double.IsInfinity);

            double Min(double[] a)
            {
                double m = double.PositiveInfinity;
                foreach (var v in a)
                    if (v < m)
                        m = v;
                return m;
            }

            double Max(double[] a)
            {
                double m = double.NegativeInfinity;
                foreach (var v in a)
                    if (v > m)
                        m = v;
                return m;
            }

            Console.WriteLine(
                $"{tag}: n={truth.Length}, truth[{Min(truth):F2},{Max(truth):F2}] preds[{Min(preds):F2},{Max(preds):F2}] NaN={anyNaN} Inf={anyInf}");
        }
    }
}