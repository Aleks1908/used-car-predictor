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
    string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 1000);

    Console.WriteLine($"Loaded {vehicles.Count} rows");

    var (x, y) = Preprocessor.ToMatrix(vehicles);
    Console.WriteLine($"Converted to matrix: {x.GetLength(0)} samples, {x.GetLength(1)} features");

    return;
}



app.Run();