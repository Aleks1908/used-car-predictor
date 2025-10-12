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
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(reactBuildPath) });
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(reactBuildPath), RequestPath = "" });

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from .NET 9!" }));
app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = new PhysicalFileProvider(reactBuildPath) });

// ------------------------------------
// Helper: Encode manual input to features
// ------------------------------------
double[] EncodeManualInput(
    int year,
    int odometer,
    string fuel,
    string transmission,
    List<string> fuels,
    List<string> transmissions,
    int targetYear = 2025)
{
    var row = new double[5 + fuels.Count + transmissions.Count];
    int age = Math.Max(0, targetYear - year);
    double odo = Math.Max(0, (double)odometer);

    double mileagePerYear = odo / (age + 1.0);
    double logOdometer = Math.Log(odo + 1.0);
    double age2 = age * age;

    row[0] = age;
    row[1] = odo;
    row[2] = mileagePerYear;
    row[3] = logOdometer;
    row[4] = age2;

    // one-hot fuel
    var f = (fuel ?? "").Trim().ToLower();
    for (int j = 0; j < fuels.Count; j++) row[5 + j] = f == fuels[j] ? 1.0 : 0.0;

    // one-hot transmission
    var t = (transmission ?? "").Trim().ToLower();
    int baseIdx = 5 + fuels.Count;
    for (int j = 0; j < transmissions.Count; j++) row[baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

    return row;
}

// ------------------------------------
// CLI commands
// ------------------------------------
if (args.Contains("--cliCsvTest"))
{
    var csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath);

    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ToList();

    Console.WriteLine($"Total vehicles: {vehicles.Count}");
    Console.WriteLine($"Models with ≥50 rows: {modelCounts.Count(m => m.Count >= 50)}");
    foreach (var m in modelCounts.Take(20))
        Console.WriteLine($"{m.Model,-20} Count={m.Count}");
    return;
}

if (args.Contains("--cli"))
{
    var csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath, 1_000_000);

    // ------------------------------------
    // Parse algorithm flags
    // ------------------------------------
    bool wantLinear = args.Contains("--linear");
    bool wantRidge = args.Contains("--ridge");
    bool wantTree = args.Contains("--tree");
    bool wantRF = args.Contains("--rf");
    bool wantGB = args.Contains("--gb");
    bool wantPredict = args.Contains("--predict");

    // Only one algorithm at a time
    // int picked = (wantLinear ? 1 : 0) + (wantRidge ? 1 : 0) + (wantTree ? 1 : 0) + (wantRF ? 1 : 0) + (wantGB ? 1 : 0);
    // if (picked != 1)
    // {
    //     Console.WriteLine("Pick exactly ONE algorithm flag.");
    //     return;
    // }

    // ------------------------------------
    // Get the target model
    // ------------------------------------
    string? specificModel = null;
    int mIdx = Array.FindIndex(args, a => a == "--model");
    if (mIdx >= 0 && mIdx + 1 < args.Length)
        specificModel = args[mIdx + 1]?.Trim().ToLower();

    if (string.IsNullOrEmpty(specificModel))
    {
        Console.WriteLine("Please provide a model name using --model <name>");
        return;
    }

    var filteredRows = vehicles.Where(v => ModelNormalizer.Normalize(v.Model) == specificModel).ToList();
    if (filteredRows.Count < 50)
    {
        Console.WriteLine($"Model {specificModel} has only {filteredRows.Count} rows (<50).");
        return;
    }

    Console.WriteLine($"Training algorithms for model '{specificModel}' ({filteredRows.Count} rows)...");

    // ------------------------------------
    // Prepare features and labels
    // ------------------------------------
    var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(filteredRows);
    var fScaler = new FeatureScaler();
    features = fScaler.FitTransform(features);
    var yScaler = new LabelScaler();
    var yScaled = yScaler.FitTransform(labels);
    var (trainX, trainY, testX, testY) = DataSplitter.Split(features, yScaled, 0.8);

    // Split train for hyperparam search
    var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, 0.75);

    // ------------------------------------
    // Train all algorithms
    // ------------------------------------
    var models = new List<(string Algo, IRegressor Model)>();

    if (wantLinear)
    {
        var linear = new LinearRegression(1e-4, 10_000);
        linear.Fit(trainX, trainY);
        models.Add(("Linear", linear));
    }

    if (wantRidge)
    {
        var ridge = RidgeRegression.TrainWithBestParams(tx, ty, vx, vy, yScaler);
        ridge.Fit(trainX, trainY);
        models.Add(("Ridge", ridge));
    }

    if (wantTree)
    {
        var tree = new DecisionTreeRegressor(6, 10, 5, 32);
        tree.Fit(trainX, trainY);
        models.Add(("Tree", tree));
    }

    if (wantRF)
    {
        var rf = RandomForestRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
        rf.Fit(trainX, trainY);
        models.Add(("RF", rf));
    }

    if (wantGB)
    {
        var gb = GradientBoostingRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
        models.Add(("GB", gb));
    }

    // ------------------------------------
    // Evaluate & predict manual row
    // ------------------------------------
    foreach (var (algo, model) in models)
    {
        var preds = yScaler.InverseTransform(model.Predict(testX));
        var truth = yScaler.InverseTransform(testY);
        var mae = Metrics.MeanAbsoluteError(truth, preds);
        var rmse = Metrics.RootMeanSquaredError(truth, preds);
        var r2 = Metrics.RSquared(truth, preds);

        Console.WriteLine($"{specificModel,-20} [{algo}] MAE={mae:F0} RMSE={rmse:F0} R²={r2:F3}");

        if (wantPredict)
        {
            var manualRow = EncodeManualInput(2016, 100_000, "gas", "automatic", fuels, transmissions, 2025);
            var scaledRow = fScaler.TransformRow(manualRow);
            var predVal = yScaler.InverseTransform(new[] { model.Predict(scaledRow) })[0];
            Console.WriteLine($"{specificModel,-20} [{algo}] Predicted manual price: {predVal:F0}");
        }
    }
}

app.Run();