using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Services;
using used_car_predictor.Backend.Training;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<ActiveModel>();
builder.Services.AddSingleton<IBundleResolver, StaticBundleResolver>();
builder.Services.AddSingleton<ModelHotLoader>();


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

app.UseHttpsRedirection();

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

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(spaRoot),
    DefaultFileNames = new List<string> { "index.html" }
});

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

app.MapControllers();

app.MapGet("/_spa-root", () => new
{
    spaRoot,
    indexExists = File.Exists(Path.Combine(spaRoot, "index.html"))
});

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
        active.LoadFromBundle(startupBundlePath, startupAlgorithm);
        Console.WriteLine($"[Model] Loaded '{startupAlgorithm}' bundle (trained {active.TrainedAt:u})");
        if (active.AnchorTargetYear.HasValue)
            Console.WriteLine($"[Model] Anchor target year in bundle: {active.AnchorTargetYear.Value}");
    }
    else
    {
        Console.WriteLine(
            $"[Model] Bundle not found at '{startupBundlePath}'. Endpoints will return 503 until first hot-load.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Model] Failed to load bundle: {ex.Message}");
}

app.Run();