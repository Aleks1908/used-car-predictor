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
    // 5 base features + one-hots
    var row = new double[5 + fuels.Count + transmissions.Count];

    int age = Math.Max(0, targetYear - year);
    double odo = Math.Max(0, (double)odometer);

    double mileagePerYear = odo / (age + 1.0);
    double logOdometer = Math.Log(odo + 1.0);
    double age2 = age * (double)age;

    // base features (must match Preprocessor indices)
    row[0] = age;
    row[1] = odo;
    row[2] = mileagePerYear;
    row[3] = logOdometer;
    row[4] = age2;

    // one-hots
    var f = (fuel ?? "").Trim().ToLower();
    for (int j = 0; j < fuels.Count; j++)
        row[5 + j] = f == fuels[j] ? 1.0 : 0.0;

    var t = (transmission ?? "").Trim().ToLower();
    int baseIdx = 5 + fuels.Count;
    for (int j = 0; j < transmissions.Count; j++)
        row[baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

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

    // -------- pick exactly ONE algo flag --------
    bool wantLinear = args.Contains("--linear");
    bool wantRidge = args.Contains("--ridge");
    bool wantTree = args.Contains("--tree");
    bool wantRF = args.Contains("--rf");
    bool wantGB = args.Contains("--gb"); // placeholder

    int picked =
        (wantLinear ? 1 : 0) + (wantRidge ? 1 : 0) + (wantTree ? 1 : 0) + (wantRF ? 1 : 0) + (wantGB ? 1 : 0);

    if (picked != 1)
    {
        Console.WriteLine("Pick exactly ONE algorithm flag:");
        Console.WriteLine("  dotnet run -c Release -- --cli --linear");
        Console.WriteLine("  dotnet run -c Release -- --cli --ridge");
        Console.WriteLine("  dotnet run -c Release -- --cli --tree");
        Console.WriteLine("  dotnet run -c Release -- --cli --rf");
        Console.WriteLine("  dotnet run -c Release -- --cli --gb   (placeholder)");
        Console.WriteLine("Optional: --model <normalized_model_name> (e.g. corolla)");
        return;
    }

    // Hardcoded minimum examples per model
    const int MinCount = 50;

    // Optional: train only one model
    string? specificModel = null;
    int mIdx = Array.FindIndex(args, a => a == "--model");
    if (mIdx >= 0 && mIdx + 1 < args.Length)
        specificModel = args[mIdx + 1]?.Trim().ToLower();

    Console.WriteLine(specificModel == null
        ? $"Per-model training (models with ≥{MinCount} rows) ..."
        : $"Training only model: '{specificModel}' (requires ≥{MinCount} rows) ...");

    // Build counts over normalized model names
    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ToList();

    // Choose the training list
    List<(string Model, int Count)> trainList;

    if (!string.IsNullOrEmpty(specificModel))
    {
        var normalized = ModelNormalizer.Normalize(specificModel);
        var entry = modelCounts.FirstOrDefault(x => x.Model == normalized);
        if (entry == null)
        {
            Console.WriteLine($"No rows found for model '{normalized}'.");
            return;
        }

        if (entry.Count < MinCount)
        {
            Console.WriteLine($"Model '{normalized}' has only {entry.Count} rows (< {MinCount}). Aborting.");
            return;
        }

        trainList = new List<(string, int)> { (entry.Model, entry.Count) };
    }
    else
    {
        trainList = modelCounts
            .Where(x => x.Count >= MinCount)
            .Select(x => (x.Model, x.Count))
            .ToList();
        Console.WriteLine($"Eligible models (≥{MinCount}): {trainList.Count}");
    }

    int trained = 0;

    foreach (var m in trainList)
    {
        var rows = vehicles.Where(v => ModelNormalizer.Normalize(v.Model) == m.Model).ToList();
        if (rows.Count < MinCount) continue;

        var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(rows);

        var fScaler = new FeatureScaler();
        features = fScaler.FitTransform(features);

        var yScaler = new LabelScaler();
        var yScaled = yScaler.FitTransform(labels);

        var (trainX, trainY, testX, testY) = DataSplitter.Split(features, yScaled, trainRatio: 0.8);

        IRegressor? model = null;

        if (wantLinear)
        {
            model = new LinearRegression(learningRate: 1e-4, epochs: 10_000);
            model.Fit(trainX, trainY);
        }
        else if (wantRidge)
        {
            var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);
            var best = RidgeRegression.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            best.Fit(trainX, trainY);
            model = best;
        }
        else if (wantTree)
        {
            model = new DecisionTreeRegressor(
                maxDepth: 6,
                minSamplesSplit: 10,
                minSamplesLeaf: 5,
                maxSplitsPerFeature: 32
            );
            model.Fit(trainX, trainY);
        }
        else if (wantRF)
        {
            Console.WriteLine($"\n[{m.Model}] 🔍 Starting Random Forest hyperparameter tuning...");

            var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);

            var rfParamGrid = new List<Dictionary<string, object>>();
            int[] nTrees = { 50, 100 };
            int[] maxDepths = { 4, 6, 8 };
            int[] minLeaf = { 10, 20, 30 };
            double[] sampleRatios = { 0.6, 0.8 };


            foreach (var nt in nTrees)
            foreach (var md in maxDepths)
            foreach (var ml in minLeaf)
            foreach (var sr in sampleRatios)
                rfParamGrid.Add(new Dictionary<string, object>
                {
                    { "nEstimators", nt },
                    { "maxDepth", md },
                    { "minSamplesSplit", 10 },
                    { "minSamplesLeaf", ml },
                    { "bootstrap", true },
                    { "sampleRatio", sr },
                    { "randomSeed", 42 }
                });

            Func<Dictionary<string, object>, IRegressor> rfFactory = p => new RandomForestRegressor(
                (int)p["nEstimators"],
                (int)p["maxDepth"],
                (int)p["minSamplesSplit"],
                (int)p["minSamplesLeaf"],
                (bool)p["bootstrap"],
                (double)p["sampleRatio"],
                (int)p["randomSeed"]
            );

            var (bestModel, bestValRmse, bestParams) = HyperparamSearch.GridSearch(
                rfFactory,
                rfParamGrid,
                tx, ty,
                vx, vy,
                yScaler
            );

            Console.WriteLine($"[{m.Model}] ✅ Best validation RMSE = {bestValRmse:F2}");

            // Bump trees to 100 for final fit, keep other best params
            var finalRf = new RandomForestRegressor(
                nEstimators: 100,
                maxDepth: (int)bestParams["maxDepth"],
                minSamplesSplit: (int)bestParams["minSamplesSplit"],
                minSamplesLeaf: (int)bestParams["minSamplesLeaf"],
                bootstrap: (bool)bestParams["bootstrap"],
                sampleRatio: (double)bestParams["sampleRatio"],
                randomSeed: (int)bestParams["randomSeed"]
            );
            finalRf.Fit(trainX, trainY);
            model = finalRf;
        }


        else if (wantGB)
        {
            Console.WriteLine($"[{m.Model}] Gradient Boosting: not implemented yet.");
            continue;
        }

        var preds = yScaler.InverseTransform(model!.Predict(testX));
        var truth = yScaler.InverseTransform(testY);
        var mae = Metrics.MeanAbsoluteError(truth, preds);
        var rmse = Metrics.RootMeanSquaredError(truth, preds);
        var r2 = Metrics.RSquared(truth, preds);

        Console.WriteLine($"{m.Model,-22} Count={m.Count,5}  MAE={mae,7:F0}  RMSE={rmse,7:F0}  R²={r2,5:F3}");
        trained++;

        // TODO: SaveModel(m.Model, model, fScaler, yScaler, fuels, transmissions);
        if (specificModel != null) break; // stop after the one requested
    }

    Console.WriteLine(specificModel == null
        ? $"Trained models: {trained}/{trainList.Count}"
        : $"Trained model: {trained}/1");

    return;
}


app.Run();