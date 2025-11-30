using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Services;
using used_car_predictor.Backend.Training;

// ASP.NET Core startup for the used-car predictor
// Configures MVC controllers, serves the React SPA, and loads the initial model bundle

var builder = WebApplication.CreateBuilder(args);

// Register core ASP.NET services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Register model-related services for dependency injection
builder.Services.AddSingleton<ActiveModel>();
builder.Services.AddSingleton<IBundleResolver, StaticBundleResolver>();
builder.Services.AddSingleton<ModelHotLoader>();

// CLI mode for offline training invoked with --cli
if (args.Contains("--cli"))
{
    CliTrainer.Run(args, builder.Environment);
    return;
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enforce HTTPS redirection
app.UseHttpsRedirection();

// Locate built React SPA assets
var spaRoot = Path.Combine(builder.Environment.ContentRootPath, "ui", "dist");
if (!Directory.Exists(spaRoot))
{
    Console.WriteLine($"[SPA] Build folder NOT found: {spaRoot}");
}
else
{
    var idx = Path.Combine(spaRoot, "index.html");
    Console.WriteLine($"[SPA] Serving index: {idx} (exists={File.Exists(idx)})");
}

// Serve index.html by default from the SPA root
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(spaRoot),
    DefaultFileNames = new List<string> { "index.html" }
});

// Serve static SPA assets and disable caching for index.html
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(spaRoot),
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.PhysicalPath?.Replace('\\', '/') ?? "";
        if (path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        }
    }
});

// Map MVC controllers 
app.MapControllers();

app.MapGet("/_spa-root", () => new
{
    spaRoot,
    indexExists = File.Exists(Path.Combine(spaRoot, "index.html"))
});

// Determine which model bundle to load at startup
var defaultStartupBundlePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "Backend", "datasets", "processed", "current.bundle.json");

var startupBundlePath = builder.Configuration["Model:BundlePath"] ?? defaultStartupBundlePath;
var startupAlgorithm = builder.Configuration["Model:Algorithm"] ?? "linear";

try
{
    var active = app.Services.GetRequiredService<ActiveModel>();
    if (File.Exists(startupBundlePath))
    {
        // Load initial model bundle into ActiveModel
        active.LoadFromBundle(startupBundlePath, startupAlgorithm);
        Console.WriteLine($"[Model] Loaded '{startupAlgorithm}' bundle (trained {active.TrainedAt:u})");
        if (active.AnchorTargetYear.HasValue)
            Console.WriteLine($"[Model] Anchor target year in bundle: {active.AnchorTargetYear.Value}");
    }
    else
    {
        // Backend will respond with 503 until a model is hot-loaded
        Console.WriteLine(
            $"[Model] Bundle not found at '{startupBundlePath}'. Endpoints will return 503 until first hot-load.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Model] Failed to load bundle: {ex.Message}");
}

app.Run();

public abstract partial class Program { }