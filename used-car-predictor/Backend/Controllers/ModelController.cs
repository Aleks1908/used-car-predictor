using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Serialization;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ModelsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public ModelsController(IWebHostEnvironment env) => _env = env;

    private string ProcessedDir =>
        Path.Combine(_env.ContentRootPath, "Backend", "datasets", "processed");

    [HttpPost("list")]
    public ActionResult<IEnumerable<LabeledValueDto>> ListByManufacturer([FromBody] ManufacturerRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Manufacturer))
            return BadRequest(new { error = "Manufacturer is required." });

        if (!Directory.Exists(ProcessedDir))
            return Ok(Array.Empty<LabeledValueDto>());

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<LabeledValueDto>();

        foreach (var file in Directory.EnumerateFiles(ProcessedDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bundle = ModelPersistence.LoadBundle(file);
                var make = bundle.Car?.Manufacturer ?? "";
                if (!make.Equals(req.Manufacturer, StringComparison.OrdinalIgnoreCase))
                    continue;

                var modelId = Path.GetFileNameWithoutExtension(file);
                var displayModel = bundle.Car?.Model ?? modelId;

                var value = ModelNormalizer.Normalize(modelId);
                var label = ToTitleCase(displayModel);

                if (seen.Add(value))
                    result.Add(new LabeledValueDto { Value = value, Label = label });
            }
            catch
            {
                // log
            }
        }

        result.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        return Ok(result);
    }

    [HttpPost("details")]
    public ActionResult<ModelFeatureMetaDto> GetModelDetails([FromBody] ModelDetailsRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Manufacturer) || string.IsNullOrWhiteSpace(req.Model))
            return BadRequest(new { error = "Manufacturer and Model are required." });

        if (!Directory.Exists(ProcessedDir))
            return NotFound(new { error = "No processed directory found." });

        var wantedId = ModelNormalizer.Normalize(req.Model);

        foreach (var file in Directory.EnumerateFiles(ProcessedDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bundle = ModelPersistence.LoadBundle(file);
                var make = bundle.Car?.Manufacturer ?? "";
                if (!make.Equals(req.Manufacturer, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileId = Path.GetFileNameWithoutExtension(file);
                var bundleModelName = bundle.Car?.Model ?? fileId;

                var matches =
                    ModelNormalizer.Normalize(fileId).Equals(wantedId, StringComparison.OrdinalIgnoreCase) ||
                    ModelNormalizer.Normalize(bundleModelName).Equals(wantedId, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;

                var fuels = bundle.Preprocess?.Fuels?.ToArray() ?? Array.Empty<string>();
                var transmissions = bundle.Preprocess?.Transmissions?.ToArray() ?? Array.Empty<string>();

                var formatted = new ModelFeatureMetaDto
                {
                    Fuels = fuels.Select(f => new LabeledValueDto
                    {
                        Value = f.ToLowerInvariant(),
                        Label = ToTitleCase(f)
                    }).ToArray(),

                    Transmissions = transmissions.Select(t => new LabeledValueDto
                    {
                        Value = t.ToLowerInvariant(),
                        Label = ToTitleCase(t)
                    }).ToArray()
                };

                return Ok(formatted);
            }
            catch
            {
                // log
            }
        }

        return NotFound(new { error = "Model not found for given manufacturer." });
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}