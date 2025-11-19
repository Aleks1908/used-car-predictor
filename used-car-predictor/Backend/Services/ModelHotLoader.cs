using used_car_predictor.Backend.Services;

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

    public async Task EnsureLoadedAsync(string manufacturer, string model, CancellationToken ct = default)
    {
        var fileId = BundleId.From(manufacturer, model);
        
        if (_active.IsLoaded && string.Equals(_currentKey, fileId, StringComparison.OrdinalIgnoreCase))
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_active.IsLoaded && string.Equals(_currentKey, fileId, StringComparison.OrdinalIgnoreCase))
                return;
            
            var (path, algorithm) = _resolver.Resolve(manufacturer, fileId);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Bundle not found at: {path}");

            _active.LoadFromBundle(path, algorithm);
            _currentKey = fileId;

            Console.WriteLine($"[Model] Hot-swapped: '{fileId}' via {algorithm}");
        }
        finally
        {
            _gate.Release();
        }
    }
}