using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Services;

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
        int max = DateTime.UtcNow.Year + 5;
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

    private ModelInfoDto CurrentModelInfo() => new()
    {
        TrainedAt = _active.TrainedAt,
        AnchorTargetYear = _active.AnchorTargetYear,
        TotalRows = _active.TotalRows
    };

    [HttpPost("predict")]
    public async Task<ActionResult<PredictResponse>> Predict([FromBody] PredictRequest req, CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int targetYear = ClampYear(req.TargetYear ?? DateTime.UtcNow.Year);
        var results = PredictAllForTargetYear(req, targetYear);

        return Ok(new PredictResponse
        {
            Manufacturer = req.Manufacturer,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = targetYear,
            Results = results,
            ModelInfo = CurrentModelInfo()
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
            ModelInfo = CurrentModelInfo()
        });
    }

    [HttpPost("predict-two")]
    public async Task<ActionResult<TwoCarPredictResponse>> PredictTwo([FromBody] TwoCarPredictRequest req,
        CancellationToken ct)
    {
        var a = await PredictOneAllAlgosAsync(req.CarA, ct);
        if (a is ObjectResult ao && ao.StatusCode is >= 400) return ao;

        var b = await PredictOneAllAlgosAsync(req.CarB, ct);
        if (b is ObjectResult bo && bo.StatusCode is >= 400) return bo;

        return Ok(new TwoCarPredictResponse
        {
            CarA = (PredictResponse)((OkObjectResult)a!).Value!,
            CarB = (PredictResponse)((OkObjectResult)b!).Value!
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
        var infoA = CurrentModelInfo();
        var seriesA = BuildSeriesForCurrentActive(req.CarA, start, end, algoKey);

        await _hotLoader.EnsureLoadedAsync(req.CarB.Manufacturer, req.CarB.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);
        var infoB = CurrentModelInfo();
        var seriesB = BuildSeriesForCurrentActive(req.CarB, start, end, algoKey);

        return Ok(new TwoCarPredictRangeResponse
        {
            Algorithm = algoKey,
            CarA = seriesA,
            CarB = seriesB,
            ModelInfoA = infoA,
            ModelInfoB = infoB
        });
    }

    private async Task<ActionResult> PredictOneAllAlgosAsync(PredictRequest req, CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int targetYear = ClampYear(req.TargetYear ?? DateTime.UtcNow.Year);
        var results = PredictAllForTargetYear(req, targetYear);

        var resp = new PredictResponse
        {
            Manufacturer = req.Manufacturer,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = targetYear,
            Results = results,
            ModelInfo = CurrentModelInfo()
        };

        return Ok(resp);
    }
}