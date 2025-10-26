using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api; // ManufacturerRequest, ModelDetailDto
using used_car_predictor.Backend.Serialization; // ModelPersistence

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ModelsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public ModelsController(IWebHostEnvironment env) => _env = env;

    [HttpPost]
    public ActionResult<IEnumerable<ModelDetailDto>> GetModels([FromBody] ManufacturerRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Manufacturer))
            return BadRequest(new { error = "Manufacturer name is required." });

        var dir = Path.Combine(_env.ContentRootPath, "Backend", "datasets", "processed");
        if (!Directory.Exists(dir))
            return Ok(Array.Empty<ModelDetailDto>());

        var items = new List<ModelDetailDto>();

        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bundle = ModelPersistence.LoadBundle(file);

                // Prefer Manufacturer, fall back to Make for older bundles
                var make = bundle.Car?.Manufacturer ?? bundle.Car?.Manufacturer ?? string.Empty;
                if (!make.Equals(request.Manufacturer, StringComparison.OrdinalIgnoreCase))
                    continue;

                var modelId = Path.GetFileNameWithoutExtension(file);
                var modelName = bundle.Car?.Model ?? modelId;

                // Fuels/Transmissions may be List<string> – convert to arrays safely
                var fuels = bundle.Preprocess?.Fuels?.ToArray() ?? Array.Empty<string>();
                var transmissions = bundle.Preprocess?.Transmissions?.ToArray() ?? Array.Empty<string>();

                // Figure out which algorithms exist in this bundle
                var algos = new List<string>(4);
                if (bundle.Linear?.Weights is { Length: > 0 }) algos.Add("linear");
                if (bundle.Ridge != null) algos.Add("ridge");
                if (bundle.RandomForest != null) algos.Add("rf");
                if (bundle.GradientBoosting != null) algos.Add("gb");

                items.Add(new ModelDetailDto
                {
                    Model = modelName,
                    Manufacturer = make,
                    FileName = Path.GetFileName(file),
                    TrainedAt = bundle.TrainedAtUtc,
                    Fuels = fuels,
                    Transmissions = transmissions,
                    Algorithms = algos.ToArray()
                });
            }
            catch
            {
                // log
            }
        }

        items.Sort((a, b) => string.Compare(a.Model, b.Model, StringComparison.OrdinalIgnoreCase));
        return Ok(items);
    }
}