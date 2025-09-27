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

    var age = targetYear - year;

    row[0] = age;
    row[1] = odometer;

    for (var j = 0; j < fuels.Count; j++)
        row[2 + j] = fuel.Trim().ToLower() == fuels[j] ? 1 : 0;

    for (var j = 0; j < transmissions.Count; j++)
        row[2 + fuels.Count + j] = transmission.Trim().ToLower() == transmissions[j] ? 1 : 0;

    return row;
}

if (args.Contains("--cliCsvTest"))
{
    string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");

    var vehicles = CsvLoader.LoadVehicles(csvPath);
    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .ToList();

    var frequentModels = modelCounts.Where(m => m.Count >= 50).ToList();
    var frequentSet = new HashSet<string>(frequentModels.Select(m => m.Model));

    var remappedVehicles = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .Select(v =>
        {
            var normalized = ModelNormalizer.Normalize(v.Model);

            if (frequentSet.Contains(normalized))
            {
                // keep full model
                return new
                {
                    FinalCategory = normalized,
                    Manufacturer = v.Manufacturer ?? "unknown"
                };
            }
            else
            {
                return new
                {
                    FinalCategory = v.Manufacturer ?? "unknown",
                    Manufacturer = v.Manufacturer ?? "unknown"
                };
            }
        })
        .ToList();

    var finalCounts = remappedVehicles
        .GroupBy(v => v.FinalCategory)
        .Select(g => new
        {
            Category = g.Key,
            Count = g.Count(),
            Manufacturer = g.First().Manufacturer
        })
        .OrderByDescending(g => g.Count)
        .ToList();

    Console.WriteLine($"Total vehicles: {vehicles.Count}");
    Console.WriteLine($"Original distinct models (≥50): {frequentModels.Count}");
    Console.WriteLine($"After fallback: {finalCounts.Count} distinct categories");
    Console.WriteLine();

    Console.WriteLine("Top 20 categories after fallback:");
    foreach (var entry in finalCounts.Take(20))
    {
        Console.WriteLine(
            $"Manufacturer: {entry.Manufacturer,-12} | Category: {entry.Category,-25} | Count: {entry.Count}");
    }

    return;
} 

if (args.Contains("--cli")){
    string csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    
    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows: 1000000);
    
    var selectedModel = "corolla";
    var modelRows = vehicles
        .Where(v => v.Model?.Trim().ToLower() == selectedModel)
        .ToList();
    
    Console.WriteLine($"Training regression for {selectedModel} ({modelRows.Count} rows)");
    
    var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(modelRows);
    
    // Scale
    var featureScaler = new FeatureScaler();
    features = featureScaler.FitTransform(features);
    
    var labelScaler = new LabelScaler();
    var scaledLabels = labelScaler.FitTransform(labels);
    
    // Train/Test split
    var (trainFeatures, trainLabels, testFeatures, testLabels) =
        DataSplitter.Split(features, scaledLabels, trainRatio: 0.8);
    
    IRegressor linear = new LinearRegression(learningRate: 0.0001, epochs: 10000);
    linear.Fit(trainFeatures, trainLabels);
    
    var (trainValX, trainValY, testX, testY) = DataSplitter.Split(features, scaledLabels, trainRatio: 0.8);  // 80% train+val, 20% test
    var (trainX, trainY, valX, valY) = DataSplitter.Split(trainValX, trainValY, trainRatio: 0.75);      // 75% train, 25% val (of the 80%) for tuning
    var ridge = RidgeRegression.TrainWithBestParams(trainX, trainY, valX, valY, labelScaler);
    Evaluator.Evaluate(ridge, testX, testY, labelScaler);


    var manualRow = EncodeManualInput(
        2016,          // manufacturing year
        100000,
        "gas",
        "automatic",
        fuels,
        transmissions,
        targetYear: 2025  // simulate for n year
    );
    
    var scaledRow = featureScaler.TransformRow(manualRow);
    
    var scaledPredictionLR = linear.Predict(scaledRow);
    var predictedPriceLR = labelScaler.InverseTransform(new double[] { scaledPredictionLR })[0];


    var scaledPredictionRR = ridge.Predict(scaledRow);
    var predictedPriceRR = labelScaler.InverseTransform(new double[] { scaledPredictionRR })[0];
    

    
    Evaluator.Evaluate(linear, testFeatures, testLabels, labelScaler);
    
    Evaluator.Evaluate(ridge, testFeatures, testLabels, labelScaler);


    Console.WriteLine($"[Linear] Predicted Corolla price: {predictedPriceLR:F2}");
    Console.WriteLine($"[Ridge] Predicted Corolla price: {predictedPriceRR:F2}");
    
    return;
}

app.Run();
