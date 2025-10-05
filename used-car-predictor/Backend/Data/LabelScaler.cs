namespace used_car_predictor.Backend.Data
{
    public class LabelScaler
    {
        private double _mean;
        private double _std;
        private readonly bool _useLog;

        public LabelScaler(bool useLog = true)
        {
            _useLog = useLog;
        }

        public double[] FitTransform(double[] y)
        {
            if (_useLog)
                y = y.Select(v => Math.Log(v + 1.0)).ToArray();

            _mean = y.Average();
            _std = Math.Sqrt(y.Select(v => Math.Pow(v - _mean, 2)).Average());
            return y.Select(v => (v - _mean) / _std).ToArray();
        }

        public double[] Transform(double[] y)
        {
            if (_useLog)
                y = y.Select(v => Math.Log(v + 1.0)).ToArray();
            return y.Select(v => (v - _mean) / _std).ToArray();
        }

        public double[] InverseTransform(double[] y)
        {
            var unscaled = y.Select(v => v * _std + _mean).ToArray();
            if (_useLog)
                unscaled = unscaled.Select(v => Math.Exp(v) - 1.0).ToArray();
            return unscaled;
        }
    }
}