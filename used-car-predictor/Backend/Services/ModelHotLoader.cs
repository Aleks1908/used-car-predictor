// ModelHotLoader.cs

using System.Threading;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Services;

public sealed class ModelHotLoader
{
    private readonly ActiveModel _active;
    private readonly IBundleResolver _resolver;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _currentKey; // normalized model id currently loaded

    public ModelHotLoader(ActiveModel active, IBundleResolver resolver)
    {
        _active = active;
        _resolver = resolver;
    }

    public async Task EnsureLoadedAsync(string make, string model, CancellationToken ct = default)
    {
        var key = ModelNormalizer.Normalize(model);

        if (_active.IsLoaded && _currentKey == key) return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_active.IsLoaded && _currentKey == key) return;

            var (path, algorithm) = _resolver.Resolve(make, model);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Bundle not found: {path}");

            _active.LoadFromBundle(path, algorithm);
            _currentKey = key;
            Console.WriteLine($"[Model] Hot-swapped: '{key}' via {algorithm} ({_active.Version})");
        }
        finally
        {
            _gate.Release();
        }
    }
}