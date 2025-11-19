using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Serialization;
using used_car_predictor.Backend.Services;

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

        var wantedMakeRaw  = req.Manufacturer.Trim();
        var wantedMakeNorm = ModelNormalizer.Normalize(wantedMakeRaw);

        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<LabeledValueDto>();

        foreach (var path in Directory.EnumerateFiles(ProcessedDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var fileIdRaw  = (Path.GetFileNameWithoutExtension(path) ?? string.Empty).Trim();
            var fileIdNorm = ModelNormalizer.Normalize(fileIdRaw);

            if (!fileIdNorm.StartsWith(wantedMakeNorm + "_", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = BundleId.From(wantedMakeRaw, fileIdRaw);
            
            var afterMake = fileIdNorm[(wantedMakeNorm.Length)..].TrimStart('_');
            var label = BundleId.BundleLabel.From(afterMake);
            
            try
            {
                var bundle = ModelPersistence.LoadBundle(path);
                var displayModel = (bundle.Car?.Model ?? afterMake).Trim();
                if (!string.IsNullOrWhiteSpace(displayModel))
                    label = BundleId.BundleLabel.From(displayModel);
            }
            catch
            {
                // ignore parse errors; filename-derived label is OK
            }

            if (seen.Add(value))
                result.Add(new LabeledValueDto { Value = value, Label = label });
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

        var wantedMakeRaw  = req.Manufacturer.Trim();
        var wantedMakeNorm = ModelNormalizer.Normalize(wantedMakeRaw);
        var wantedTokens   = Tokenize(wantedMakeNorm, req.Model);

        foreach (var file in Directory.EnumerateFiles(ProcessedDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var fileIdRaw  = (Path.GetFileNameWithoutExtension(file) ?? string.Empty).Trim();
            var fileIdNorm = ModelNormalizer.Normalize(fileIdRaw);
            
            if (!fileIdNorm.StartsWith(wantedMakeNorm + "_", StringComparison.OrdinalIgnoreCase))
                continue;
            
            var fileTokens = Tokenize(wantedMakeNorm, fileIdRaw);
            var filenameMatches = TokensEqual(fileTokens, wantedTokens);
            bool anyMatch = filenameMatches;
            
            string? displayModel = null;
            string[] fuelsRaw = Array.Empty<string>();
            string[] transRaw = Array.Empty<string>();
            int? minYear = null, maxYear = null, trainedForYear = null;

            try
            {
                var bundle = ModelPersistence.LoadBundle(file);
                displayModel = (bundle.Car?.Model ?? fileIdRaw).Trim();

                fuelsRaw = bundle.Preprocess?.Fuels?.ToArray() ?? Array.Empty<string>();
                transRaw = bundle.Preprocess?.Transmissions?.ToArray() ?? Array.Empty<string>();
                minYear  = bundle.Car?.MinYear ?? bundle.Preprocess?.MinYear;
                maxYear  = bundle.Car?.MaxYear ?? bundle.Preprocess?.MaxYear;
                trainedForYear = bundle.Preprocess?.AnchorTargetYear;

                var bundleTokens = Tokenize(wantedMakeNorm, displayModel);
                anyMatch |= TokensEqual(bundleTokens, wantedTokens);
            }
            catch
            {
                // if parsing fails, we'll filenameTokens which is checked
            }

            if (!anyMatch) continue;
            
            var fuelsFiltered = FilterValues(fuelsRaw, req.AllowedFuels);
            var transFiltered = FilterValues(transRaw, req.AllowedTransmissions);

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

    private static string[] Tokenize(string wantedMakeNorm, string modelOrFileId)
    {
        static string N(string s) => ModelNormalizer.Normalize(s ?? string.Empty);

        var raw = N(modelOrFileId);
        if (raw.StartsWith(wantedMakeNorm + "_", StringComparison.OrdinalIgnoreCase))
            raw = raw[(wantedMakeNorm.Length + 1)..];

        return raw
            .Replace('-', '_')
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(N)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TokensEqual(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;

        return a.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(b.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }
}