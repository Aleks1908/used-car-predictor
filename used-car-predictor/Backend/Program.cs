using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
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
    var csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");

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
                // keep full model
                return new
                {
                    FinalCategory = normalized,
                    Manufacturer = v.Manufacturer ?? "unknown"
                };

            return new
            {
                FinalCategory = v.Manufacturer ?? "unknown",
                Manufacturer = v.Manufacturer ?? "unknown"
            };
        })
        .ToList();

    var finalCounts = remappedVehicles
        .GroupBy(v => v.FinalCategory)
        .Select(g => new
        {
            Category = g.Key,
            Count = g.Count(),
            g.First().Manufacturer
        })
        .OrderByDescending(g => g.Count)
        .ToList();

    Console.WriteLine($"Total vehicles: {vehicles.Count}");
    Console.WriteLine($"Original distinct models (≥50): {frequentModels.Count}");
    Console.WriteLine($"After fallback: {finalCounts.Count} distinct categories");
    Console.WriteLine();

    Console.WriteLine("Top 20 categories after fallback:");
    foreach (var entry in finalCounts.Take(20))
        Console.WriteLine(
            $"Manufacturer: {entry.Manufacturer,-12} | Category: {entry.Category,-25} | Count: {entry.Count}");

    return;
}

if (args.Contains("--cli"))
{
    var csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath, 1_000_000);


    // ====== PER-MODEL TRAIN (recommended) ======
    Console.WriteLine("Per-model training (models with ≥50 rows) ...");

    // 1) Count normalized models
    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ToList();

    var eligible = modelCounts.Where(x => x.Count >= 50).ToList();
    Console.WriteLine($"Eligible models (≥50 rows): {eligible.Count}");

    int trained = 0;
    foreach (var m in eligible)
    {
        // 2) Filter rows for this model
        var rows = vehicles.Where(v => ModelNormalizer.Normalize(v.Model) == m.Model).ToList();
        if (rows.Count < 50) continue; // defensive

        // 3) Build matrix + scalers
        var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(rows);
        var fScaler = new FeatureScaler();
        features = fScaler.FitTransform(features);
        var yScaler = new LabelScaler();
        var yScaled = yScaler.FitTransform(labels);

        // 4) Split
        var (trainX, trainY, testX, testY) = DataSplitter.Split(features, yScaled, trainRatio: 0.8);

        // 5) Pick an algorithm (start with Ridge tuned or a small Random Forest)
        // Ridge tuned on small grid:
        var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);
        var ridge = RidgeRegression.TrainWithBestParams(tx, ty, vx, vy, yScaler);

        // Retrain ridge on full train (tx+vx combined)
        ridge.Fit(trainX, trainY);

        // Evaluate
        var preds = yScaler.InverseTransform(ridge.Predict(testX));
        var truth = yScaler.InverseTransform(testY);
        var mae = Metrics.MeanAbsoluteError(truth, preds);
        var rmse = Metrics.RootMeanSquaredError(truth, preds);
        var r2 = Metrics.RSquared(truth, preds);

        Console.WriteLine($"{m.Model,-20} Count={m.Count,5}  MAE={mae,8:F0}  RMSE={rmse,8:F0}  R²={r2,5:F3}");

        trained++;

        // 6) (Next step) Save artifact for API use:
        // SaveModel(m.Model, ridge, fScaler, yScaler, fuels, transmissions);
    }

    Console.WriteLine($"Trained models: {trained}/{eligible.Count}");
    return;
}


app.Run();