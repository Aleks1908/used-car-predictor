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
                PredictedPrice = (decimal)Math.Round(price, 0),
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
        if (req.FromYear > req.ToYear) return BadRequest(new { error = "FromYear must be <= ToYear" });

        await _hotLoader.EnsureLoadedAsync(req.Manufacturer, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        var items = new List<PredictRangeItem>();

        for (int y = req.FromYear; y <= req.ToYear; y++)
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
                    PredictedPrice = Math.Round((decimal)price, 0),
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
}