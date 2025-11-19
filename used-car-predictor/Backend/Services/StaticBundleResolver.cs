using used_car_predictor.Backend.Evaluation;

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

    public (string Path, string Algorithm) Resolve(string manufacturer, string model)
    { 
        var canonicalId = BundleId.From(manufacturer, model);
        var canonicalPath = Path.Combine(_processedDir, $"{canonicalId}.json");
        if (File.Exists(canonicalPath))
            return (canonicalPath, _defaultAlgorithm);
        
        var fallback = FindByTokenSet(manufacturer, canonicalId);
        if (fallback is not null)
            return (fallback, _defaultAlgorithm);

        return (canonicalPath, _defaultAlgorithm);
    }

    private string? FindByTokenSet(string manufacturer, string id)
    {
        static string Norm(string s) => ModelNormalizer.Normalize(s ?? string.Empty);

        var make = Norm(manufacturer);
        var wanted = TokensAfterMake(make, id);
        if (wanted.Count == 0) return null;

        foreach (var path in Directory.EnumerateFiles(_processedDir, $"{make}_*.json"))
        {
            var fname = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            var have = TokensAfterMake(make, fname);
            if (have.SetEquals(wanted))
                return path;
        }

        return null;
    }

    private static HashSet<string> TokensAfterMake(string make, string id)
    {
        var name = Path.GetFileNameWithoutExtension(id) ?? string.Empty;
        name = name.Replace(' ', '_').Replace('-', '_').Trim('_');

        var prefix = make + "_";
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            name = name.Substring(prefix.Length);
        
        var parts = name
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => ModelNormalizer.Normalize(p))
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
    }
}