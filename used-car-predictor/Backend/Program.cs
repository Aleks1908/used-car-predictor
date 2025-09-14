using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;

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
    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 100000);

    var selectedModel = "corolla";
    var modelRows = vehicles
        .Where(v => v.Model?.Trim().ToLower() == selectedModel)
        .ToList();

    Console.WriteLine($"Training regression for {selectedModel} ({modelRows.Count} rows)");

    var (features, labels) = Preprocessor.ToMatrix(modelRows);

    var featureScaler = new FeatureScaler();
    features = featureScaler.FitTransform(features);

    var labelScaler = new LabelScaler();
    var scaledLabels = labelScaler.FitTransform(labels);

    var model = new LinearRegression(learningRate: 0.01, epochs: 5000);
    model.Fit(features, scaledLabels);

    var scaledPredictions = model.Predict(features);
    var predictions = labelScaler.InverseTransform(scaledPredictions);

    for (int i = 0; i < Math.Min(10, modelRows.Count); i++)
    {
        Console.WriteLine($"Year={modelRows[i].Year}, Odo={modelRows[i].Odometer}, " +
                          $"True={labels[i]}, Pred={predictions[i]:F2}");
    }

    return;
}

app.Run();