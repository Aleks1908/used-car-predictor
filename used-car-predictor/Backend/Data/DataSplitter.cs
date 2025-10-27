using System;
using System.Linq;

namespace used_car_predictor.Backend.Data
{
    public static class DataSplitter
    {
        public static (double[,], double[], double[,], double[]) Split(
            double[,] X,
            double[] y,
            double trainRatio = 0.8,
            int? seed = null) // null = random each run; set a value for reproducibility
        {
            int n = X.GetLength(0);
            var idx = Enumerable.Range(0, n).ToArray();

            var rng = seed is null ? Random.Shared : new Random(seed.Value);
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1); // Fisher–Yates
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            int nTrain = (int)Math.Round(n * trainRatio);
            var trX = new double[nTrain, X.GetLength(1)];
            var trY = new double[nTrain];
            var teX = new double[n - nTrain, X.GetLength(1)];
            var teY = new double[n - nTrain];

            for (int i = 0; i < nTrain; i++)
            {
                int s = idx[i];
                for (int j = 0; j < X.GetLength(1); j++) trX[i, j] = X[s, j];
                trY[i] = y[s];
            }

            for (int i = nTrain; i < n; i++)
            {
                int s = idx[i];
                int r = i - nTrain;
                for (int j = 0; j < X.GetLength(1); j++) teX[r, j] = X[s, j];
                teY[r] = y[s];
            }

            return (trX, trY, teX, teY);
        }
    }
}