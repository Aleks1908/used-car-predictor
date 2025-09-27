namespace used_car_predictor.Backend.Models
{
    public interface IRegressor
    {
        void Fit(double[,] features, double[] labels);
        double[] Predict(double[,] features);   // batch
        double Predict(double[] featureRow);   // single row
        string Name { get; }
    }
}