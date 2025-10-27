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

    [HttpPost("predict")]
    public async Task<ActionResult<PredictResponse>> Predict([FromBody] PredictRequest req, CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int targetYear = req.TargetYear ?? DateTime.UtcNow.Year;
        targetYear = Math.Clamp(targetYear, 1990, DateTime.UtcNow.Year + 5);

        var raw = ServingHelpers.EncodeManualInput(
            req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
            _active.Fuels, _active.Transmissions, targetYear: targetYear);

        var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
        var zByAlgo = _active.PredictAllScaled(x);

        var results = zByAlgo.Select(kv =>
        {
            var price = ServingHelpers.InverseLabel(kv.Value, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            _active.MetricsByAlgo.TryGetValue(kv.Key, out var m);
            return new ModelPredictionDto
            {
                Algorithm = kv.Key,
                PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0),
                Metrics = new ModelMetricsDto
                {
                    Mse = Math.Round(m.Mse, 2),
                    Mae = Math.Round(m.Mae, 2),
                    R2 = Math.Round(m.R2, 2)
                }
            };
        }).ToList();

        return Ok(new PredictResponse
        {
            Manufacturer = req.Manufacturer,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = targetYear,
            Results = results,
            ModelInfo = new ModelInfoDto
            {
                TrainedAt = _active.TrainedAt,
                AnchorTargetYear = _active.AnchorTargetYear,
                TotalRows = _active.TotalRows
            }
        });
    }

    [HttpPost("predict/range")]
    public async Task<ActionResult<PredictRangeResponse>> PredictRange([FromBody] PredictRangeRequest req,
        CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);
        if (req.StartYear > req.EndYear) return BadRequest(new { error = "startYear must be <= endYear" });

        var items = new List<PredictRangeItem>();

        for (int y = req.StartYear; y <= req.EndYear; y++)
        {
            var raw = ServingHelpers.EncodeManualInput(
                req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
                _active.Fuels, _active.Transmissions, targetYear: y);

            var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
            var zByAlgo = _active.PredictAllScaled(x);

            var results = zByAlgo.Select(kv =>
            {
                var price = ServingHelpers.InverseLabel(kv.Value, _active.LabelMean, _active.LabelStd,
                    _active.LabelUseLog);
                _active.MetricsByAlgo.TryGetValue(kv.Key, out var m);
                return new ModelPredictionDto
                {
                    Algorithm = kv.Key,
                    PredictedPrice = Math.Round((decimal)Math.Max(0, price), 0),
                    Metrics = new ModelMetricsDto
                    {
                        Mse = Math.Round(m.Mse, 2),
                        Mae = Math.Round(m.Mae, 2),
                        R2 = Math.Round(m.R2, 2)
                    }
                };
            }).ToList();

            items.Add(new PredictRangeItem
            {
                Manufacturer = req.Manufacturer,
                Model = req.Model,
                YearOfProduction = req.YearOfProduction,
                TargetYear = y,
                Results = results
            });
        }

        var info = new ModelInfoDto
        {
            TrainedAt = _active.TrainedAt,
            AnchorTargetYear = _active.AnchorTargetYear,
            TotalRows = _active.TotalRows
        };

        return Ok(new PredictRangeResponse { Items = items, ModelInfo = info });
    }

    [HttpPost("predict-two")]
    public async Task<ActionResult<TwoCarPredictResponse>> PredictTwo([FromBody] TwoCarPredictRequest req,
        CancellationToken ct)
    {
        var a = await PredictOneAllAlgosAsync(req.CarA, ct);
        if (a is ObjectResult aProblem && aProblem.StatusCode is >= 400) return aProblem;

        var b = await PredictOneAllAlgosAsync(req.CarB, ct);
        if (b is ObjectResult bProblem && bProblem.StatusCode is >= 400) return bProblem;

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
        if (req.StartYear > req.EndYear)
            return BadRequest(new { error = "startYear must be <= endYear" });

        var algoKey = NormalizeAlgo(req.Algorithm);
        if (algoKey is null)
            return BadRequest(new { error = "algorithm must be one of: linear, ridge, rf, gb" });

        await _hotLoader.EnsureLoadedAsync(req.CarA.Manufacturer, req.CarA.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        var infoA = new ModelInfoDto
        {
            TrainedAt = _active.TrainedAt,
            AnchorTargetYear = _active.AnchorTargetYear,
            TotalRows = _active.TotalRows
        };
        var seriesA = BuildSeriesForCurrentActive(req.CarA, req.StartYear, req.EndYear, algoKey);

        await _hotLoader.EnsureLoadedAsync(req.CarB.Manufacturer, req.CarB.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        var infoB = new ModelInfoDto
        {
            TrainedAt = _active.TrainedAt,
            AnchorTargetYear = _active.AnchorTargetYear,
            TotalRows = _active.TotalRows
        };
        var seriesB = BuildSeriesForCurrentActive(req.CarB, req.StartYear, req.EndYear, algoKey);

        return Ok(new TwoCarPredictRangeResponse
        {
            Algorithm = algoKey,
            CarA = seriesA,
            CarB = seriesB,
            ModelInfoA = infoA,
            ModelInfoB = infoB
        });
    }

    private List<YearlyPrediction> BuildSeriesForCurrentActive(PredictRequest req, int startYear, int endYear,
        string algoKey)
    {
        var series = new List<YearlyPrediction>();

        for (int y = startYear; y <= endYear; y++)
        {
            var raw = ServingHelpers.EncodeManualInput(
                req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
                _active.Fuels, _active.Transmissions, targetYear: y);

            var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
            var zByAlgo = _active.PredictAllScaled(x);

            if (!zByAlgo.TryGetValue(algoKey, out var z))
                throw new InvalidOperationException($"Algorithm '{algoKey}' not available in active model.");

            var price = ServingHelpers.InverseLabel(z, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            series.Add(new YearlyPrediction { Year = y, PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0) });
        }

        return series;
    }

    private static string? NormalizeAlgo(string? algo)
    {
        if (string.IsNullOrWhiteSpace(algo)) return null;
        return algo.Trim().ToLowerInvariant() switch
        {
            "linear" => "linear",
            "ridge" => "ridge",
            "rf" => "rf",
            "gb" => "gb",
            _ => null
        };
    }

    private async Task<ActionResult> PredictOneAllAlgosAsync(PredictRequest req, CancellationToken ct)
    {
        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        int targetYear = req.TargetYear ?? DateTime.UtcNow.Year;
        targetYear = Math.Clamp(targetYear, 1990, DateTime.UtcNow.Year + 5);

        var raw = ServingHelpers.EncodeManualInput(
            req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
            _active.Fuels, _active.Transmissions, targetYear: targetYear);

        var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
        var zByAlgo = _active.PredictAllScaled(x);

        var results = zByAlgo.Select(kv =>
        {
            var price = ServingHelpers.InverseLabel(kv.Value, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            _active.MetricsByAlgo.TryGetValue(kv.Key, out var m);
            return new ModelPredictionDto
            {
                Algorithm = kv.Key,
                PredictedPrice = (decimal)Math.Round(Math.Max(0, price), 0),
                Metrics = new ModelMetricsDto
                {
                    Mse = Math.Round(m.Mse, 2),
                    Mae = Math.Round(m.Mae, 2),
                    R2 = Math.Round(m.R2, 2)
                }
            };
        }).ToList();

        var resp = new PredictResponse
        {
            Manufacturer = req.Manufacturer,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = targetYear,
            Results = results,
            ModelInfo = new ModelInfoDto
            {
                TrainedAt = _active.TrainedAt,
                AnchorTargetYear = _active.AnchorTargetYear,
                TotalRows = _active.TotalRows
            }
        };

        return Ok(resp);
    }
}