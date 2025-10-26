using Microsoft.AspNetCore.Mvc;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ManufacturersController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public ManufacturersController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public ActionResult<IEnumerable<string>> Get()
    {
        var dir = Path.Combine(_env.ContentRootPath, "Backend", "datasets", "processed");
        if (!Directory.Exists(dir))
            return Ok(Array.Empty<string>());

        var manufacturers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var bundle = ModelPersistence.LoadBundle(file);
                var manufacturer = bundle.Car?.Manufacturer ?? bundle.Car?.Manufacturer ?? "";
                if (!string.IsNullOrWhiteSpace(manufacturer))
                    manufacturers.Add(manufacturer.Trim());
            }
            catch
            {
                // log
            }
        }

        var sorted = manufacturers
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(sorted);
    }
}