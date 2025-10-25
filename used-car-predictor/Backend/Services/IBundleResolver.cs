namespace used_car_predictor.Backend.Services;

public interface IBundleResolver
{
    (string Path, string Algorithm) Resolve(string make, string model);
}