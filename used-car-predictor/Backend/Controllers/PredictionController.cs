using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Services;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly ActiveModel _active;
    private readonly ModelHotLoader _hotLoader;

    public PredictionController(ActiveModel active, ModelHotLoader hotLoader)
    {
        _active = active;
        _hotLoader = hotLoader;
    }

    private static int ClampYear(int y)
    {
        int max = DateTime.UtcNow.Year + 10;
        return Math.Clamp(y, 1990, max);
    }

    private static (double mse, double mae, double r2) GetMetricsOrDefault(
        IReadOnlyDictionary<string, (double Mse, double Mae, double R2)> map, string key)
    {
        if (map.TryGetValue(key, out var m)) return (m.Mse, m.Mae, m.R2);

        if (key.Equals("ridge_rf", StringComparison.OrdinalIgnoreCase) && map.TryGetValue("rf", out m))
            return (m.Mse, m.Mae, m.R2);

        if (key.Equals("ridge_gb", StringComparison.OrdinalIgnoreCase) && map.TryGetValue("gb", out m))
            return (m.Mse, m.Mae, m.R2);

        return (0, 0, 0);
    }

    private static string? NormalizeAlgo(string? algo)
    {
        if (string.IsNullOrWhiteSpace(algo)) return null;
        var k = algo.Trim().ToLowerInvariant();
        return k switch
        {
            "linear" => "linear",
            "ridge" => "ridge",
            "ridge_rf" => "ridge_rf",
            "ridge_gb" => "ridge_gb",
            _ => null
        };
    }

    private List<ModelPredictionDto> PredictAllForTargetYear(PredictRequest req, int targetYear)
    {
        var raw = ServingHelpers.EncodeManualInput(
            req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
            _active.Fuels, _active.Transmissions, targetYear: targetYear,
            anchorTargetYear: _active.AnchorTargetYear);

        var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
        var zByAlgo = _active.PredictAllScaled(x);

        var preferredOrder = new[] { "linear", "ridge", "ridge_rf", "ridge_gb" };
        var results = new List<ModelPredictionDto>(zByAlgo.Count);

        foreach (var key in preferredOrder)
        {
            if (!zByAlgo.TryGetValue(key, out var z)) continue;

            var price = ServingHelpers.InverseLabel(z, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            var (mse, mae, r2) = GetMetricsOrDefault(_active.MetricsByAlgo, key);

            results.Add(new ModelPredictionDto
            {
                Algorithm = key,
                PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0),
                Metrics = new ModelMetricsDto
                {
                    Mse = Math.Round(mse, 2),
                    Mae = Math.Round(mae, 2),
                    R2 = Math.Round(r2, 2)
                }
            });
        }

        foreach (var kv in zByAlgo)
        {
            if (results.Any(r => r.Algorithm.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))) continue;

            var price = ServingHelpers.InverseLabel(kv.Value, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            var (mse, mae, r2) = GetMetricsOrDefault(_active.MetricsByAlgo, kv.Key);

            results.Add(new ModelPredictionDto
            {
                Algorithm = kv.Key,
                PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0),
                Metrics = new ModelMetricsDto
                {
                    Mse = Math.Round(mse, 2),
                    Mae = Math.Round(mae, 2),
                    R2 = Math.Round(r2, 2)
                }
            });
        }

        return results;
    }

    private List<YearlyPrediction> BuildSeriesForCurrentActive(PredictRequest req, int startYear, int endYear,
        string algoKey)
    {
        var series = new List<YearlyPrediction>();
        for (int y = startYear; y <= endYear; y++)
        {
            var raw = ServingHelpers.EncodeManualInput(
                req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
                _active.Fuels, _active.Transmissions,
                targetYear: y,
                anchorTargetYear: _active.AnchorTargetYear
            );

            var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
            var zByAlgo = _active.PredictAllScaled(x);

            if (!zByAlgo.TryGetValue(algoKey, out var z))
                throw new InvalidOperationException($"Algorithm '{algoKey}' not available in active model.");

            var price = ServingHelpers.InverseLabel(z, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            series.Add(new YearlyPrediction
            {
                Year = y,
                PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0)
            });
        }

        return series;
    }

    private ModelInfoDto CurrentModelInfo()
    {
        return new ModelInfoDto
        {
            TrainedAt = _active.TrainedAt,
            AnchorTargetYear = _active.AnchorTargetYear,
            TotalRows = _active.TotalRows,
        };
    }

    [HttpPost("predict")]
    public async Task<ActionResult<PredictResponse>> Predict([FromBody] PredictRequest req, CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int targetYear = ClampYear(req.TargetYear ?? DateTime.UtcNow.Year);
        var results = PredictAllForTargetYear(req, targetYear);

        foreach (var r in results) r.Metrics = null;

        return Ok(new PredictResponse
        {
            Manufacturer = req.Manufacturer,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = targetYear,
            Results = results,
            ModelInfo = CurrentModelInfo(),
            Metrics = BuildMetricsSummary()
        });
    }

    [HttpPost("predict/range")]
    public async Task<ActionResult<PredictRangeResponse>> PredictRange([FromBody] PredictRangeRequest req,
        CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int start = ClampYear(req.StartYear);
        int end = ClampYear(req.EndYear);
        if (start > end) return BadRequest(new { error = "startYear must be <= endYear" });

        var items = new List<PredictRangeItem>();
        for (int y = start; y <= end; y++)
        {
            var single = new PredictRequest
            {
                Manufacturer = req.Manufacturer,
                Model = req.Model,
                YearOfProduction = req.YearOfProduction,
                MileageKm = req.MileageKm,
                FuelType = req.FuelType,
                Transmission = req.Transmission,
                TargetYear = y
            };

            var results = PredictAllForTargetYear(single, y);
            foreach (var r in results) r.Metrics = null;

            items.Add(new PredictRangeItem
            {
                Manufacturer = req.Manufacturer,
                Model = req.Model,
                YearOfProduction = req.YearOfProduction,
                TargetYear = y,
                Results = results
            });
        }

        return Ok(new PredictRangeResponse
        {
            Items = items,
            ModelInfo = CurrentModelInfo(),
            Metrics = BuildMetricsSummary()
        });
    }


    [HttpPost("predict-two")]
    public async Task<ActionResult<TwoCarPredictResponse>> PredictTwo([FromBody] TwoCarPredictRequest req,
        CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.CarA.Manufacturer, req.CarA.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int ta = ClampYear(req.CarA.TargetYear ?? DateTime.UtcNow.Year);
        var resA = PredictAllForTargetYear(req.CarA with { TargetYear = ta }, ta);
        foreach (var r in resA) r.Metrics = null;
        var a = new PredictResponse
        {
            Manufacturer = req.CarA.Manufacturer,
            Model = req.CarA.Model,
            YearOfProduction = req.CarA.YearOfProduction,
            TargetYear = ta,
            Results = resA,
            ModelInfo = CurrentModelInfo(),
            Metrics = BuildMetricsSummary()
        };

        await _hotLoader.EnsureLoadedAsync(req.CarB.Manufacturer, req.CarB.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int tb = ClampYear(req.CarB.TargetYear ?? DateTime.UtcNow.Year);
        var resB = PredictAllForTargetYear(req.CarB with { TargetYear = tb }, tb);
        foreach (var r in resB) r.Metrics = null;
        var b = new PredictResponse
        {
            Manufacturer = req.CarB.Manufacturer,
            Model = req.CarB.Model,
            YearOfProduction = req.CarB.YearOfProduction,
            TargetYear = tb,
            Results = resB,
            ModelInfo = CurrentModelInfo(),
            Metrics = BuildMetricsSummary()
        };

        return Ok(new TwoCarPredictResponse
        {
            CarA = a,
            CarB = b,
        });
    }

    [HttpPost("predict-two/range")]
    public async Task<ActionResult<TwoCarPredictRangeResponse>> PredictTwoRange(
        [FromBody] TwoCarPredictRangeRequest req, CancellationToken ct)
    {
        int start = ClampYear(req.StartYear);
        int end = ClampYear(req.EndYear);
        if (start > end) return BadRequest(new { error = "startYear must be <= endYear" });

        var algoKey = NormalizeAlgo(req.Algorithm);
        if (algoKey is null)
            return BadRequest(new { error = "algorithm must be one of: linear, ridge, ridge_rf, ridge_gb" });

        await _hotLoader.EnsureLoadedAsync(req.CarA.Manufacturer, req.CarA.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);
        var seriesA = BuildSeriesForCurrentActive(req.CarA, start, end, algoKey);
        var infoA = CurrentModelInfo();
        var metricsA = BuildMetricsSummary(algoKey);

        await _hotLoader.EnsureLoadedAsync(req.CarB.Manufacturer, req.CarB.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);
        var seriesB = BuildSeriesForCurrentActive(req.CarB, start, end, algoKey);
        var infoB = CurrentModelInfo();
        var metricsB = BuildMetricsSummary(algoKey);

        return Ok(new TwoCarPredictRangeResponse
        {
            Algorithm = algoKey,
            CarA = seriesA,
            CarB = seriesB,
            ModelInfoA = infoA,
            ModelInfoB = infoB,
            MetricsA = metricsA,
            MetricsB = metricsB
        });
    }


    private Dictionary<string, AlgorithmMetricsDto>? BuildMetricsSummary(params string[] restrictTo)
    {
        if (!_active.IsLoaded) return null;

        HashSet<string>? allowed = null;
        if (restrictTo is { Length: > 0 })
            allowed = new HashSet<string>(restrictTo, StringComparer.OrdinalIgnoreCase);

        var temp = new Dictionary<string, AlgorithmMetricsDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var (algo, triplet) in _active.MetricsByAlgo)
        {
            if (allowed != null && !allowed.Contains(algo)) continue;

            temp[algo] = new AlgorithmMetricsDto
            {
                Metrics = new ModelMetricsDto
                {
                    Mse = Math.Round(triplet.Mse, 2),
                    Mae = Math.Round(triplet.Mae, 2),
                    R2 = Math.Round(triplet.R2, 2)
                }
            };
        }

        if (_active.TrainingTimes is not null)
        {
            foreach (var kv in _active.TrainingTimes)
            {
                string? algo = kv.Key switch
                {
                    "Linear" => "linear",
                    "Ridge" => "ridge",
                    "RandomForest" => "ridge_rf",
                    "GradientBoosting" => "ridge_gb",
                    _ => null
                };
                if (algo is null) continue;
                if (allowed != null && !allowed.Contains(algo)) continue;

                if (!temp.TryGetValue(algo, out var entry))
                    entry = temp[algo] = new AlgorithmMetricsDto();

                entry.Timing = kv.Value;
            }
        }

        var preferred = new[] { "linear", "ridge", "ridge_rf", "ridge_gb" };
        var result = new Dictionary<string, AlgorithmMetricsDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in preferred)
            if (temp.TryGetValue(key, out var v))
                result[key] = v;

        foreach (var kv in temp)
            if (!result.ContainsKey(kv.Key))
                result[kv.Key] = kv.Value;

        return result.Count > 0 ? result : null;
    }
}