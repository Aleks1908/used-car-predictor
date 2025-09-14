using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var reactBuildPath = Path.Combine(Directory.GetCurrentDirectory(), "ui", "build");

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(reactBuildPath)
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(reactBuildPath),
    RequestPath = ""
});

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from .NET 9!" }));

app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(reactBuildPath)
});

if (args.Contains("--cli"))
{
    
    string baseDir = AppContext.BaseDirectory;
    string csvPath = Path.Combine(baseDir, "Backend", "datasets", "raw", "vehicles.csv");

    if (!File.Exists(csvPath))
    {
        Console.WriteLine($" File not found: {csvPath}");
        return;
    }

    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 100);
    Console.WriteLine($"✅ Loaded {vehicles.Count} rows");

    return; 
}


app.Run();