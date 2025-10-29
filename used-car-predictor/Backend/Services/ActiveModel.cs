using System;
using System.Collections.Generic;
using System.Linq;
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

    // NEW: per-algorithm training time info (e.g., Linear/Ridge/RandomForest/GradientBoosting)
    public Dictionary<string, TrainingTimeDto>? TrainingTimes { get; private set; }

    public bool IsLoaded => _models.Count > 0;

    public IReadOnlyDictionary<string, (double Mse, double Mae, double R2)> MetricsByAlgo
        => _metricsByAlgo;

    public (double Mse, double Mae, double R2) Metrics =>
        _metricsByAlgo.TryGetValue("ridge", out var m) ? m :
        _metricsByAlgo.Count > 0 ? _metricsByAlgo.Values.First() : (0, 0, 0);

    public Dictionary<string, double> PredictAllScaled(ReadOnlySpan<double> x)
    {
        var models = Volatile.Read(ref _models);
        if (models.Count == 0) throw new InvalidOperationException("No models loaded.");

        var input = x.ToArray();
        var output = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Direct models
        foreach (var (algo, model) in models)
        {
            // Skip residual learners in direct passthrough
            if (algo.Equals("gb", StringComparison.OrdinalIgnoreCase)) continue;
            if (algo.Equals("rf", StringComparison.OrdinalIgnoreCase)) continue;

            output[algo] = model.Predict(input);
        }

        // Ridge + GB residual
        if (models.TryGetValue("ridge", out var ridge) &&
            models.TryGetValue("gb", out var gbResidual))
        {
            var zr = ridge.Predict(input);
            var zg = gbResidual.Predict(input);
            output["ridge_gb"] = zr + zg;
        }

        // Ridge + RF residual
        if (models.TryGetValue("ridge", out var ridge2) &&
            models.TryGetValue("rf", out var rfResidual))
        {
            var zr = ridge2.Predict(input);
            var zf = rfResidual.Predict(input);
            output["ridge_rf"] = zr + zf;
        }

        return output;
    }

    public void LoadFromBundle(string path, string _ = "")
    {
        var bundle = ModelPersistence.LoadBundle(path);

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
            static (double Mse, double Mae, double R2) ToTriplet(MetricsDto m)
            {
                var mse = (m.RMSE > 0) ? m.RMSE * m.RMSE : 0.0;
                return (mse, m.MAE, m.R2);
            }

            foreach (var kv in bundle.Metrics)
            {
                var key = kv.Key.Trim().ToLowerInvariant();
                newMetrics[key] = ToTriplet(kv.Value);
            }
        }

        // Normalize & carry over training-time map (case-insensitive keys)
        Dictionary<string, TrainingTimeDto>? newTimes = null;
        if (bundle.TrainingTimes is not null)
        {
            newTimes = new Dictionary<string, TrainingTimeDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in bundle.TrainingTimes)
            {
                // Keep original keys as-is but store in case-insensitive map
                newTimes[kv.Key] = kv.Value ?? new TrainingTimeDto();
            }
        }

        lock (_swapLock)
        {
            Volatile.Write(ref _models, newModels);
            Volatile.Write(ref _metricsByAlgo, newMetrics);

            var pp = bundle.Preprocess ?? new PreprocessDto();

            Fuels = (pp.Fuels != null && pp.Fuels.Count > 0) ? pp.Fuels : new List<string>();
            Transmissions = (pp.Transmissions != null && pp.Transmissions.Count > 0)
                ? pp.Transmissions
                : new List<string>();

            FeatureMeans = pp.FeatureMeans;
            FeatureStds = pp.FeatureStds;
            LabelMean = pp.LabelMean;
            LabelStd = pp.LabelStd;
            LabelUseLog = pp.LabelUseLog;
            AnchorTargetYear = pp.AnchorTargetYear;
            TotalRows = pp.TotalRows;
            TrainingTimes = newTimes ?? new Dictionary<string, TrainingTimeDto>(StringComparer.OrdinalIgnoreCase);

            TrainedAt = bundle.TrainedAtUtc;
        }
    }
}