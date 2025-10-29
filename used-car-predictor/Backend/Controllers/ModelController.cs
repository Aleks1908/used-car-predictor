using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ModelsController(IWebHostEnvironment env) : ControllerBase
{
    private string ProcessedDir =>
        Path.Combine(env.ContentRootPath, "Backend", "datasets", "processed");

    [HttpPost("list")]
    public ActionResult<IEnumerable<LabeledValueDto>> ListByManufacturer([FromBody] ManufacturerRequest? req)
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
                var make = (bundle.Car?.Manufacturer ?? "").Trim();

                if (!make.Equals(req.Manufacturer.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var modelId = Path.GetFileNameWithoutExtension(file);
                var displayModel = (bundle.Car?.Model ?? modelId).Trim();

                var value = ModelNormalizer.Normalize(modelId);
                var label = ToTitleCase(displayModel);

                if (seen.Add(value))
                    result.Add(new LabeledValueDto { Value = value, Label = label });
            }
            catch
            {
                // TODO: log
            }
        }

        result.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        return Ok(result);
    }

    public sealed class ModelDetailsRequest
    {
        public string Manufacturer { get; set; } = default!;
        public string Model { get; set; } = default!;

        public string[]? AllowedFuels { get; set; }
        public string[]? AllowedTransmissions { get; set; }
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
                var make = (bundle.Car?.Manufacturer ?? "").Trim();
                if (!make.Equals(req.Manufacturer.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileId = Path.GetFileNameWithoutExtension(file);
                var bundleModelName = (bundle.Car?.Model ?? fileId).Trim();

                var matches =
                    ModelNormalizer.Normalize(fileId).Equals(wantedId, StringComparison.OrdinalIgnoreCase) ||
                    ModelNormalizer.Normalize(bundleModelName).Equals(wantedId, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;

                var fuelsRaw = bundle.Preprocess?.Fuels?.ToArray() ?? Array.Empty<string>();
                var transRaw = bundle.Preprocess?.Transmissions?.ToArray() ?? Array.Empty<string>();

                var fuelsFiltered = FilterValues(fuelsRaw, req.AllowedFuels);
                var transFiltered = FilterValues(transRaw, req.AllowedTransmissions);

                int? minYear = bundle.Car?.MinYear ?? bundle.Preprocess?.MinYear;
                int? maxYear = bundle.Car?.MaxYear ?? bundle.Preprocess?.MaxYear;
                int? trainedForYear = bundle.Preprocess?.AnchorTargetYear; // <-- NEW

                var formatted = new ModelFeatureMetaDto
                {
                    Fuels = fuelsFiltered.Select(f => new LabeledValueDto
                    {
                        Value = f.ToLowerInvariant(),
                        Label = ToTitleCase(f)
                    }).ToArray(),

                    Transmissions = transFiltered.Select(t => new LabeledValueDto
                    {
                        Value = t.ToLowerInvariant(),
                        Label = ToTitleCase(t)
                    }).ToArray(),

                    MinYear = minYear,
                    MaxYear = maxYear,
                    AnchorTargetYear = trainedForYear
                };

                return Ok(formatted);
            }
            catch
            {
                // TODO: log
            }
        }

        return NotFound(new { error = "Model not found for given manufacturer." });
    }

    private static IEnumerable<string> FilterValues(IEnumerable<string> values, string[]? allowOnly)
    {
        var v = values.Where(s => !s.Equals("other", StringComparison.OrdinalIgnoreCase));

        if (allowOnly is { Length: > 0 })
        {
            var allow = new HashSet<string>(allowOnly.Select(a => a.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);
            v = v.Where(allow.Contains);
        }

        return v;
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(value.ToLowerInvariant());
    }
}