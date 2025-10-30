using System.Linq;

namespace used_car_predictor.Backend.Data
{
    public class LabelScaler()
    {
        private double _mean;
        private double _std;
        private bool _fitted;


        public double[] FitTransform(double[] y)
        {
            var z = (double[])y.Clone();

            _mean = z.Average();
            _std = Math.Sqrt(z.Select(v => Math.Pow(v - _mean, 2)).Average());
            if (_std <= 0) _std = 1e-12;

            _fitted = true;
            return z.Select(v => (v - _mean) / _std).ToArray();
        }

        public double[] Transform(double[] y)
        {
            EnsureFitted();
            var z = (double[])y.Clone();
            return z.Select(v => (v - _mean) / _std).ToArray();
        }

        public double[] InverseTransform(double[] y)
        {
            EnsureFitted();
            return y.Select(v => v * _std + _mean).ToArray();
        }

        private void EnsureFitted()
        {
            if (!_fitted)
                throw new InvalidOperationException("LabelScaler has not been fitted. Call FitTransform first.");
        }
    }
}