using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Services;

public sealed class StaticBundleResolver : IBundleResolver
{
    private readonly string _processedDir;
    private readonly string _defaultAlgorithm;

    public StaticBundleResolver(IHostEnvironment env, IConfiguration cfg)
    {
        _processedDir = Path.Combine(env.ContentRootPath, "Backend", "datasets", "processed");
        _defaultAlgorithm = cfg["Model:Algorithm"] ?? "ridge";
    }

    public (string Path, string Algorithm) Resolve(string make, string model)
    {
        var id = ModelNormalizer.Normalize(model);
        var path = Path.Combine(_processedDir, $"{id}.json");
        return (path, _defaultAlgorithm);
    }
}