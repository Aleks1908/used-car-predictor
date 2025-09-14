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

double[] EncodeManualInput(
    int year, 
    int odometer, 
    string fuel, 
    string transmission, 
    List<string> fuels, 
    List<string> transmissions)
{
    var row = new double[2 + fuels.Count + transmissions.Count];

    row[0] = year;
    row[1] = odometer;

    for (int j = 0; j < fuels.Count; j++)
        row[2 + j] = fuel.Trim().ToLower() == fuels[j] ? 1 : 0;

    for (int j = 0; j < transmissions.Count; j++)
        row[2 + fuels.Count + j] = transmission.Trim().ToLower() == transmissions[j] ? 1 : 0;

    return row;
}


if (args.Contains("--cli"))
{
    string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 20000);

    var selectedModel = "corolla";
    var modelRows = vehicles
        .Where(v => v.Model?.Trim().ToLower() == selectedModel)
        .ToList();

    Console.WriteLine($"Training regression for {selectedModel} ({modelRows.Count} rows)");

    var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(modelRows);
    
    var featureScaler = new FeatureScaler();
    features = featureScaler.FitTransform(features);

    var labelScaler = new LabelScaler();
    var scaledLabels = labelScaler.FitTransform(labels);
    
    var model = new LinearRegression(learningRate: 0.01, epochs: 5000);
    model.Fit(features, scaledLabels);

    var manualRow = EncodeManualInput(
        2025,
        100000,
        "gas",
        "automatic",
        fuels,
        transmissions
    );
    
    var scaledRow = featureScaler.TransformRow(manualRow);
    
    var scaledPrediction = model.Predict(scaledRow);
    var predictedPrice = labelScaler.InverseTransform(scaledPrediction);

    Console.WriteLine($"Predicted 2025 Corolla automatic (100k km) price: {predictedPrice:F2}");

    return;
}

app.Run();