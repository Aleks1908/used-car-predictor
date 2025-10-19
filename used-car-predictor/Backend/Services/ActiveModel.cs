using System.Collections.Generic;
using System.Threading;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Services;

public sealed class ActiveModel
{
    private Dictionary<string, IRegressor> _models = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, (double Mse, double Mae, double R2)> _metricsByAlgo =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _swapLock = new();

    public string Version { get; private set; } = "unloaded";
    public DateTimeOffset TrainedAt { get; private set; } = DateTimeOffset.MinValue;

    public IReadOnlyList<string> Fuels { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Transmissions { get; private set; } = Array.Empty<string>();
    public double[] FeatureMeans { get; private set; } = Array.Empty<double>();
    public double[] FeatureStds { get; private set; } = Array.Empty<double>();
    public double LabelMean { get; private set; }
    public double LabelStd { get; private set; }
    public bool LabelUseLog { get; private set; }

    public bool IsLoaded => _models.Count > 0;

    public IReadOnlyDictionary<string, (double Mse, double Mae, double R2)> MetricsByAlgo
        => _metricsByAlgo;

    public (double Mse, double Mae, double R2) Metrics =>
        _metricsByAlgo.TryGetValue("ridge", out var m) ? m :
        _metricsByAlgo.Count > 0 ? _metricsByAlgo.Values.First() : (0, 0, 0);

    /// <summary>
    /// Returns raw (scaled) predictions per algorithm: keys = "linear" | "ridge" | "rf" | "gb".
    /// Caller should inverse-transform with LabelMean/Std/UseLog.
    /// </summary>
    public Dictionary<string, double> PredictAllScaled(ReadOnlySpan<double> x)
    {
        var models = Volatile.Read(ref _models); // snapshot
        if (models.Count == 0) throw new InvalidOperationException("No models loaded.");

        var input = x.ToArray();
        var output = new Dictionary<string, double>(models.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (algo, model) in models)
            output[algo] = model.Predict(input);

        return output;
    }

    /// <summary>
    /// Loads all available models + state from a bundle (linear/ridge/rf/gb if present).
    /// </summary>
    public void LoadFromBundle(string path, string _ = "")
    {
        var bundle = ModelPersistence.LoadBundle(path);

        // Build new immutable snapshots
        var newModels = new Dictionary<string, IRegressor>(StringComparer.OrdinalIgnoreCase);

        if (bundle.Linear?.Weights is { Length: > 0 })
            newModels["linear"] = ModelPersistence.ImportLinear(bundle.Linear);

        if (bundle.Ridge is not null)
            newModels["ridge"] = ModelPersistence.ImportRidge(bundle.Ridge);

        if (bundle.RandomForest is not null)
            newModels["rf"] = ModelPersistence.ImportRandomForest(bundle.RandomForest);

        if (bundle.GradientBoosting is not null)
            newModels["gb"] = ModelPersistence.ImportGradientBoosting(bundle.GradientBoosting);

        var newMetrics = new Dictionary<string, (double Mse, double Mae, double R2)>(StringComparer.OrdinalIgnoreCase);
        if (bundle.Metrics is not null)
        {
            static (double Mse, double Mae, double R2) Norm(MetricsDto m)
                => (m.RMSE > 0 ? m.RMSE * m.RMSE : 0, m.MAE, m.R2);

            if (bundle.Metrics.TryGetValue("linear", out var ml)) newMetrics["linear"] = Norm(ml);
            if (bundle.Metrics.TryGetValue("ridge", out var mr)) newMetrics["ridge"] = Norm(mr);
            if (bundle.Metrics.TryGetValue("rf", out var mf)) newMetrics["rf"] = Norm(mf);
            if (bundle.Metrics.TryGetValue("gb", out var mg)) newMetrics["gb"] = Norm(mg);
        }

        lock (_swapLock)
        {
            Volatile.Write(ref _models, newModels);
            Volatile.Write(ref _metricsByAlgo, newMetrics);

            Fuels = bundle.Preprocess.Fuels;
            Transmissions = bundle.Preprocess.Transmissions;
            FeatureMeans = bundle.Preprocess.FeatureMeans;
            FeatureStds = bundle.Preprocess.FeatureStds;
            LabelMean = bundle.Preprocess.LabelMean;
            LabelStd = bundle.Preprocess.LabelStd;
            LabelUseLog = bundle.Preprocess.LabelUseLog;

            Version = bundle.Version;
            TrainedAt = bundle.TrainedAtUtc;
        }
    }
}