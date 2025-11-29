namespace used_car_predictor.Backend.Services;

//Used for resolving the path to a preprocessed dataset bundle based on manufacturer and model.
public sealed class StaticBundleResolver : IBundleResolver
{
    private readonly string _processedDir;
    private readonly string _defaultAlgorithm;

    public StaticBundleResolver(IHostEnvironment env, IConfiguration cfg)
    {
        _processedDir = Path.Combine(env.ContentRootPath, "Backend", "datasets", "processed");
        _defaultAlgorithm = cfg["Model:Algorithm"] ?? "ridge";
    }

    public (string Path, string Algorithm) Resolve(string manufacturer, string model)
    {
        var fileId = BundleId.From(manufacturer, model);
        var path = Path.Combine(_processedDir, $"{fileId}.json");
        return (path, _defaultAlgorithm);
    }
}