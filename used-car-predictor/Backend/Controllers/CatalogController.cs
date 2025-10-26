using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class CatalogController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public CatalogController(IWebHostEnvironment env) => _env = env;

    [HttpGet]
    public ActionResult<CatalogResponse> Get()
    {
        var dir = Path.Combine(_env.ContentRootPath, "Backend", "datasets", "processed");
        if (!Directory.Exists(dir)) return Ok(new CatalogResponse());

        var items = new List<CatalogItemDto>();

        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bundle = ModelPersistence.LoadBundle(file);

                var algos = new List<string>(4);
                if (bundle.Linear?.Weights is { Length: > 0 }) algos.Add("linear");
                if (bundle.Ridge is not null) algos.Add("ridge");
                if (bundle.RandomForest is not null) algos.Add("rf");
                if (bundle.GradientBoosting is not null) algos.Add("gb");

                var modelId = Path.GetFileNameWithoutExtension(file);

                // ← read clean meta (fallbacks if missing)
                var manufacturer = bundle.Car?.Manufacturer ?? "";
                var model = bundle.Car?.Model ?? modelId;

                items.Add(new CatalogItemDto
                {
                    ModelId = modelId,
                    DisplayModel = model,
                    Manufacturer = manufacturer,
                    FileName = Path.GetFileName(file),
                    TrainedAt = bundle.TrainedAtUtc,
                    Algorithms = algos.ToArray()
                });
            }
            catch
            {
                //  log 
            }
        }

        items.Sort((a, b) => string.Compare(a.DisplayModel, b.DisplayModel, StringComparison.OrdinalIgnoreCase));
        return Ok(new CatalogResponse { Items = items });
    }

    private static string? TryGetDisplayModel(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;

        foreach (var seg in notes.Split(new[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = seg.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length == 2 && kv[0].Equals("model", StringComparison.OrdinalIgnoreCase))
                return kv[1];
        }

        return null;
    }
}