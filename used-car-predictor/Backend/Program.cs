using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Utils;

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

// Helper for encoding new inputs
double[] EncodeManualInput(
    int year,
    int odometer,
    string fuel,
    string transmission,
    List<string> fuels,
    List<string> transmissions,
    int targetYear = 2025)
{
    var row = new double[2 + fuels.Count + transmissions.Count];

    int age = targetYear - year;

    row[0] = age;
    row[1] = odometer;

    for (int j = 0; j < fuels.Count; j++)
        row[2 + j] = fuel.Trim().ToLower() == fuels[j] ? 1 : 0;

    for (int j = 0; j < transmissions.Count; j++)
        row[2 + fuels.Count + j] = transmission.Trim().ToLower() == transmissions[j] ? 1 : 0;

    return row;
}

if (args.Contains("--cli"))
{
    // Load vehicles from your CSV (adjust path as needed)
string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");

    var vehicles = CsvLoader.LoadVehicles(csvPath);
    // Count distinct non-empty models
    
    
    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .OrderByDescending(g => g.Count)
        .ToList();
    
    var frequentModels = modelCounts.Where(m => m.Count >= 50).ToList();

    // Second call: count vehicles belonging to those frequent models
    var frequentVehicles = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .Where(v => frequentModels.Any(m => 
            m.Model == ModelNormalizer.Normalize(v.Model)))
        .ToList();
    
    Console.WriteLine($"Total vehicles: {vehicles.Count}");
    Console.WriteLine($"Number of distinct models with ≥50 vehicles: {frequentModels.Count}");
    Console.WriteLine($"Total number of vehicles from those models: {frequentVehicles.Count}");

    
    // string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    // var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 1000000);
    //
    // var selectedModel = "corolla";
    // var modelRows = vehicles
    //     .Where(v => v.Model?.Trim().ToLower() == selectedModel)
    //     .ToList();
    //
    // Console.WriteLine($"Training regression for {selectedModel} ({modelRows.Count} rows)");
    //
    //
    //
    // var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(modelRows);
    //
    // // Scale
    // var featureScaler = new FeatureScaler();
    // features = featureScaler.FitTransform(features);
    //
    // var labelScaler = new LabelScaler();
    // var scaledLabels = labelScaler.FitTransform(labels);
    //
    // // Train/Test split
    // var (trainFeatures, trainLabels, testFeatures, testLabels) =
    //     DataSplitter.Split(features, scaledLabels, trainRatio: 0.8);
    //
    // var model = new LinearRegression(learningRate: 0.01, epochs: 5000);
    // model.Fit(trainFeatures, trainLabels);
    //
    // var scaledPreds = model.Predict(testFeatures);
    // var preds = labelScaler.InverseTransform(scaledPreds);
    // var trueVals = labelScaler.InverseTransform(testLabels);
    //
    // var mae = Metrics.MeanAbsoluteError(trueVals, preds);
    // var rmse = Metrics.RootMeanSquaredError(trueVals, preds);
    // var r2 = Metrics.RSquared(trueVals, preds);
    //
    // Console.WriteLine($"MAE = {mae:F2}, RMSE = {rmse:F2}, R² = {r2:F3}");
    //
    // var manualRow = EncodeManualInput(
    //     2016,          // manufacturing year
    //     100000,
    //     "gas",
    //     "automatic",
    //     fuels,
    //     transmissions,
    //     targetYear: 2030  // simulate for n year
    // );
    //
    // var scaledRow = featureScaler.TransformRow(manualRow);
    // var scaledPrediction = model.Predict(scaledRow);
    // var predictedPrice = labelScaler.InverseTransform(scaledPrediction);
    //
    // Console.WriteLine($"Predicted 2018 Corolla automatic (100k km) price in 2025: {predictedPrice:F2}");
    //
    return;
}

app.Run();
