using System.Collections.Generic;
using System.Threading;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Services;

public sealed class ActiveModel
{
    private Dictionary<string, IRegressor> _models = new(StringComparer.OrdinalIgnoreCase);

    // Store MSE/MAE/R2 per algorithm key (keys are lower-cased)
    private Dictionary<string, (double Mse, double Mae, double R2)> _metricsByAlgo =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _swapLock = new();

    public DateTimeOffset TrainedAt { get; private set; } = DateTimeOffset.MinValue;

    public IReadOnlyList<string> Fuels { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Transmissions { get; private set; } = Array.Empty<string>();
    public double[] FeatureMeans { get; private set; } = Array.Empty<double>();
    public double[] FeatureStds { get; private set; } = Array.Empty<double>();
    public double LabelMean { get; private set; }
    public double LabelStd { get; private set; }
    public bool LabelUseLog { get; private set; }
    public int? AnchorTargetYear { get; private set; }
    public int? TotalRows { get; private set; }

    public bool IsLoaded => _models.Count > 0;

    public IReadOnlyDictionary<string, (double Mse, double Mae, double R2)> MetricsByAlgo
        => _metricsByAlgo;

    // Default "Metrics" property (kept for back-compat): prefer ridge; else first available
    public (double Mse, double Mae, double R2) Metrics =>
        _metricsByAlgo.TryGetValue("ridge", out var m) ? m :
        _metricsByAlgo.Count > 0 ? _metricsByAlgo.Values.First() : (0, 0, 0);

    /// <summary>
    /// Produce predictions in z-space for all exposed algorithms.
    /// Notes:
    /// - We DO NOT expose raw "gb" when GB is trained on residuals.
    /// - If both "ridge" and "gb" exist, we add "ridge_gb" = ridge + gb_residual.
    /// - Other base models (linear, rf) are exposed as-is.
    /// </summary>
    public Dictionary<string, double> PredictAllScaled(ReadOnlySpan<double> x)
    {
        var models = Volatile.Read(ref _models);
        if (models.Count == 0) throw new InvalidOperationException("No models loaded.");

        var input = x.ToArray();
        var output = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (algo, model) in models)
        {
            // Skip exposing raw GB residuals directly
            if (algo.Equals("gb", StringComparison.OrdinalIgnoreCase))
                continue;

            output[algo] = model.Predict(input);
        }

        // If we have both ridge and gb (residual), expose the combined "ridge_gb"
        if (models.TryGetValue("ridge", out var ridge) &&
            models.TryGetValue("gb", out var gbResidual))
        {
            var zr = ridge.Predict(input);
            var zg = gbResidual.Predict(input); // residual in z-space
            output["ridge_gb"] = zr + zg;
        }

        return output;
    }

    /// <summary>
    /// Load bundle and swap the active model set + metadata atomically.
    /// We import available models and load ALL metrics keys (linear, ridge, rf, ridge_gb, etc.).
    /// </summary>
    public void LoadFromBundle(string path, string _ = "")
    {
        var bundle = ModelPersistence.LoadBundle(path);

        var newModels = new Dictionary<string, IRegressor>(StringComparer.OrdinalIgnoreCase);

        // Import models when present
        if (bundle.Linear?.Weights is { Length: > 0 })
            newModels["linear"] = ModelPersistence.ImportLinear(bundle.Linear);

        if (bundle.Ridge is not null)
            newModels["ridge"] = ModelPersistence.ImportRidge(bundle.Ridge);

        if (bundle.RandomForest is not null)
            newModels["rf"] = ModelPersistence.ImportRandomForest(bundle.RandomForest);

        if (bundle.GradientBoosting is not null)
        {
            // IMPORTANT: In the new pipeline GB is trained on residuals, so we keep it under "gb"
            // but we won't expose it directly; we'll expose "ridge_gb" at prediction time.
            newModels["gb"] = ModelPersistence.ImportGradientBoosting(bundle.GradientBoosting);
        }

        // Load ALL metrics keys present in the bundle (including ridge_gb)
        var newMetrics = new Dictionary<string, (double Mse, double Mae, double R2)>(StringComparer.OrdinalIgnoreCase);
        if (bundle.Metrics is not null)
        {
            static (double Mse, double Mae, double R2) ToTriplet(MetricsDto m)
            {
                // Bundle stores RMSE, MAE, R2; runtime dictionary stores MSE, MAE, R2
                var mse = (m.RMSE > 0) ? m.RMSE * m.RMSE : 0.0;
                return (mse, m.MAE, m.R2);
            }

            foreach (var kv in bundle.Metrics)
            {
                var key = kv.Key.Trim().ToLowerInvariant(); // normalize
                newMetrics[key] = ToTriplet(kv.Value);
            }
        }

        // Swap everything atomically
        lock (_swapLock)
        {
            Volatile.Write(ref _models, newModels);
            Volatile.Write(ref _metricsByAlgo, newMetrics);

            // Safely read preprocess (null-protect)
            var pp = bundle.Preprocess ?? new PreprocessDto();

            Fuels = (pp.Fuels != null && pp.Fuels.Count > 0)
                ? pp.Fuels
                : new List<string>();

            Transmissions = (pp.Transmissions != null && pp.Transmissions.Count > 0)
                ? pp.Transmissions
                : new List<string>();

            FeatureMeans = pp.FeatureMeans ?? Array.Empty<double>();
            FeatureStds = pp.FeatureStds ?? Array.Empty<double>();
            LabelMean = pp.LabelMean;
            LabelStd = pp.LabelStd;
            LabelUseLog = pp.LabelUseLog;
            AnchorTargetYear = pp.AnchorTargetYear;
            TotalRows = pp.TotalRows;

            TrainedAt = bundle.TrainedAtUtc;
        }
    }
}