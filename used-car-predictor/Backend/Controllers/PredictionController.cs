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
        await _hotLoader.EnsureLoadedAsync(req.Make, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        var raw = ServingHelpers.EncodeManualInput(
            req.YearOfProduction, req.MileageKm, req.FuelType, req.Transmission,
            _active.Fuels, _active.Transmissions, targetYear: req.TargetYear);


        var x = ServingHelpers.ScaleRow(raw, _active.FeatureMeans, _active.FeatureStds);
        var zByAlgo = _active.PredictAllScaled(x);

        var results = zByAlgo.Select(kv =>
        {
            var price = ServingHelpers.InverseLabel(kv.Value, _active.LabelMean, _active.LabelStd, _active.LabelUseLog);
            _active.MetricsByAlgo.TryGetValue(kv.Key, out var m);
            return new ModelPredictionDto
            {
                Algorithm = kv.Key,
                PredictedPrice = Math.Round((decimal)price, 2),
                Metrics = new ModelMetricsDto { Mse = m.Mse, Mae = m.Mae, R2 = m.R2 }
            };
        }).ToList();

        var info = new ModelInfoDto { Version = _active.Version, TrainedAt = _active.TrainedAt };

        return Ok(new PredictResponse
        {
            Make = req.Make,
            Model = req.Model,
            YearOfProduction = req.YearOfProduction,
            TargetYear = req.TargetYear,
            Currency = "EUR",
            Results = results,
            ModelInfo = info
        });
    }

    [HttpPost("predict/range")]
    public async Task<ActionResult<PredictRangeResponse>> PredictRange([FromBody] PredictRangeRequest req,
        CancellationToken ct)
    {
        if (req.FromYear > req.ToYear) return BadRequest(new { error = "FromYear must be <= ToYear" });

        await _hotLoader.EnsureLoadedAsync(req.Make, req.Model, ct);
        if (!_active.IsLoaded) return Problem("No active model loaded.", statusCode: 503);

        var items = new List<PredictResponse>();
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
                    PredictedPrice = Math.Round((decimal)price, 2),
                    Metrics = new ModelMetricsDto { Mse = m.Mse, Mae = m.Mae, R2 = m.R2 }
                };
            }).ToList();

            items.Add(new PredictResponse
            {
                Make = req.Make,
                Model = req.Model,
                YearOfProduction = req.YearOfProduction,
                TargetYear = y,
                Currency = "EUR",
                Results = results,
                ModelInfo = new ModelInfoDto { Version = _active.Version, TrainedAt = _active.TrainedAt }
            });
        }

        var info = new ModelInfoDto { Version = _active.Version, TrainedAt = _active.TrainedAt };
        return Ok(new PredictRangeResponse { Currency = "EUR", Items = items, ModelInfo = info });
    }
}