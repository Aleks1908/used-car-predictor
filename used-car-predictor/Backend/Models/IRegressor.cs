namespace used_car_predictor.Backend.Models
{
    public interface IRegressor
    {
        void Fit(double[,] features, double[] labels);
        double[] Predict(double[,] features);
        double Predict(double[] featureRow);
        string Name { get; }
    }
}