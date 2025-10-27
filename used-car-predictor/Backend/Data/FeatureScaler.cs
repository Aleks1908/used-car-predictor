namespace used_car_predictor.Backend.Data
{
    public class FeatureScaler
    {
        private double[] _means = Array.Empty<double>();
        private double[] _stds = Array.Empty<double>();

        public IReadOnlyList<double> Means => _means;
        public IReadOnlyList<double> Stds => _stds;
        public bool IsFitted => _means.Length > 0 && _stds.Length == _means.Length;

        public double[,] FitTransform(double[,] features)
        {
            int n = features.GetLength(0);
            int p = features.GetLength(1);

            _means = new double[p];
            _stds = new double[p];

            var scaled = new double[n, p];

            for (int j = 0; j < p; j++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++) sum += features[i, j];
                _means[j] = sum / Math.Max(1, n);

                double variance = 0;
                for (int i = 0; i < n; i++)
                    variance += Math.Pow(features[i, j] - _means[j], 2);
                _stds[j] = Math.Sqrt(variance / Math.Max(1, n));

                for (int i = 0; i < n; i++)
                    scaled[i, j] = _stds[j] > 0 ? (features[i, j] - _means[j]) / _stds[j] : 0.0;
            }

            return scaled;
        }

        public double[,] Transform(double[,] features)
        {
            EnsureFitted();
            int n = features.GetLength(0);
            int p = features.GetLength(1);
            if (_means.Length != p)
                throw new InvalidOperationException($"FeatureScaler expected {_means.Length} features, got {p}.");

            var outp = new double[n, p];
            for (int j = 0; j < p; j++)
            for (int i = 0; i < n; i++)
                outp[i, j] = _stds[j] > 0 ? (features[i, j] - _means[j]) / _stds[j] : 0.0;

            return outp;
        }

        public double[] TransformRow(double[] row)
        {
            EnsureFitted();
            if (row.Length != _means.Length)
                throw new InvalidOperationException(
                    $"FeatureScaler expected row len={_means.Length}, got {row.Length}.");

            var result = new double[row.Length];
            for (int j = 0; j < row.Length; j++)
                result[j] = _stds[j] > 0 ? (row[j] - _means[j]) / _stds[j] : 0.0;

            return result;
        }

        private void EnsureFitted()
        {
            if (!IsFitted)
                throw new InvalidOperationException("FeatureScaler has not been fitted. Call FitTransform first.");
        }
    }
}