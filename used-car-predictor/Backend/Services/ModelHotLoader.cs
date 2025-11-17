using System.Threading;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Services;

public sealed class ModelHotLoader
{
    private readonly ActiveModel _active;
    private readonly IBundleResolver _resolver;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _currentKey;

    public ModelHotLoader(ActiveModel active, IBundleResolver resolver)
    {
        _active = active;
        _resolver = resolver;
    }

    private static string Norm(string s) => ModelNormalizer.Normalize(s ?? string.Empty);

    public async Task EnsureLoadedAsync(string manufacturer, string model, CancellationToken ct = default)
    {
        var key = $"{Norm(manufacturer)}_{Norm(model)}";

        if (_active.IsLoaded && string.Equals(_currentKey, key, StringComparison.OrdinalIgnoreCase))
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_active.IsLoaded && string.Equals(_currentKey, key, StringComparison.OrdinalIgnoreCase))
                return;
            
            var (path, algorithm) = _resolver.Resolve(manufacturer, key);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Bundle not found: {path}");

            _active.LoadFromBundle(path, algorithm);
            _currentKey = key;
            Console.WriteLine($"[Model] Hot-swapped: '{key}' via {algorithm}");
        }
        finally
        {
            _gate.Release();
        }
    }
}