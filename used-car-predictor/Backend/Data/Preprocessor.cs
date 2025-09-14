namespace used_car_predictor.Backend.Data;

public class Preprocessor
{
    public static (double[,], double[]) ToMatrix(List<Vehicle> vehicles)
    {
        int m = vehicles.Count;
        int n = 2;

        var x = new double[m, n];
        var y = new double[m];

        for (int i = 0; i < m; i++)
        {
            var v = vehicles[i];
            x[i, 0] = v.Year ?? 0;
            x[i, 1] = v.Odometer ?? 0;
            y[i] = v.Price ?? 0;
        }

        return (x, y);
    }
}