using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;
using used_car_predictor.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<ActiveModel>();
builder.Services.AddSingleton<IBundleResolver, StaticBundleResolver>();
builder.Services.AddSingleton<ModelHotLoader>();

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

app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(spaRoot),
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        ctx.Context.Response.Headers.Pragma = "no-cache";
        ctx.Context.Response.Headers.Expires = "0";
    }
});

app.MapGet("/_spa-root", () => new
{
    spaRoot,
    indexExists = File.Exists(Path.Combine(spaRoot, "index.html"))
});

static string EnsureDir(string path)
{
    Directory.CreateDirectory(path);
    return path;
}

static string? ArgValue(string[] a, string name)
{
    var i = Array.IndexOf(a, name);
    return (i >= 0 && i + 1 < a.Length) ? a[i + 1] : null;
}

if (args.Contains("--cli"))
{
    string datasetsRoot = Path.Combine(builder.Environment.ContentRootPath, "Backend", "datasets");
    var rawDir = Path.Combine(datasetsRoot, "raw");
    var processedDir = EnsureDir(Path.Combine(datasetsRoot, "processed"));

    var csvFromArg = ArgValue(args, "--csv");
    var csvPath = csvFromArg ?? Path.Combine(rawDir, "vehicles.csv");
    Console.WriteLine($"Loading vehicles from: {csvPath}");

    var maxRowsArg = ArgValue(args, "--max");
    int maxRows = int.TryParse(maxRowsArg, out var mr) ? mr : 1_000_000;

    var anchorYearArg = ArgValue(args, "--anchor-year") ?? ArgValue(args, "--anchor");
    int anchorYear = int.TryParse(anchorYearArg, out var ay) ? ay : 2030;
    anchorYear = Math.Clamp(anchorYear, 1990, DateTime.UtcNow.Year + 10);
    Console.WriteLine($"[Train] Anchor target year = {anchorYear} (prices modeled relative to this year)");

    bool wantLinear = args.Contains("--linear");
    bool wantRidge = args.Contains("--ridge");
    bool wantRF = args.Contains("--rf");
    bool wantGB = args.Contains("--gb");
    bool wantPredict = args.Contains("--predict");
    if (!wantLinear && !wantRidge && !wantRF && !wantGB)
        wantLinear = wantRidge = wantRF = wantGB = true;

    string manualFuel = "gas", manualTrans = "automatic";
    var manualIdx = Array.FindIndex(args, a => a == "--manual");
    if (manualIdx >= 0 && manualIdx + 4 < args.Length)
    {
        manualFuel = args[manualIdx + 3] ?? "gas";
        manualTrans = args[manualIdx + 4] ?? "automatic";
        wantPredict = true;
    }


    var vehicles = CsvLoader.LoadVehicles(csvPath, maxRows);

    const int MinCount = 50;
    string? specificModel = null;
    var mIdx = Array.FindIndex(args, a => a == "--model");
    if (mIdx >= 0 && mIdx + 1 < args.Length)
        specificModel = args[mIdx + 1]?.Trim().ToLower();

    var modelCounts = vehicles
        .Where(v => !string.IsNullOrWhiteSpace(v.Model))
        .GroupBy(v => ModelNormalizer.Normalize(v.Model))
        .Select(g => new { Model = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .ToList();

    List<(string Model, int Count)> trainList;
    if (!string.IsNullOrEmpty(specificModel))
    {
        var normalized = ModelNormalizer.Normalize(specificModel);
        var entry = modelCounts.FirstOrDefault(x => x.Model == normalized);
        if (entry == null || entry.Count < MinCount)
        {
            Console.WriteLine($"No rows or insufficient data for model '{normalized}'");
            return;
        }

        trainList = new List<(string, int)> { (entry.Model, entry.Count) };
    }
    else
    {
        trainList = modelCounts.Where(x => x.Count >= MinCount).Select(x => (x.Model, x.Count)).ToList();
    }

    int trained = 0;

    foreach (var m in trainList)
    {
        var rows = vehicles.Where(v => ModelNormalizer.Normalize(v.Model) == m.Model).ToList();

        var yearVals = rows.Where(r => r.Year.HasValue).Select(r => r.Year!.Value).ToList();
        int? minYear = yearVals.Count > 0 ? yearVals.Min() : (int?)null;
        int? maxYear = yearVals.Count > 0 ? yearVals.Max() : (int?)null;

        var (rawX, rawY, fuels, transmissions) = Preprocessor.ToMatrix(rows, targetYear: anchorYear);

        var (trainRawX, trainRawY, testRawX, testRawY) = DataSplitter.Split(rawX, rawY, trainRatio: 0.8);

        var fScaler = new FeatureScaler();
        var yScaler = new LabelScaler();

        var trainX = fScaler.FitTransform(trainRawX);
        var testX = fScaler.Transform(testRawX);

        var trainY = yScaler.FitTransform(trainRawY);
        var testY = yScaler.Transform(testRawY);

        MetricsDto EvaluateAndPredict(string name, IRegressor model)
        {
            var preds = yScaler.InverseTransform(model.Predict(testX));
            var truth = yScaler.InverseTransform(testY);

            double mae = Metrics.MeanAbsoluteError(truth, preds);
            double rmse = Metrics.RootMeanSquaredError(truth, preds);
            double r2 = Metrics.RSquared(truth, preds);

            Console.WriteLine($"{m.Model,-22} [{name}] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");


            return new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
        }

        var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);

        LinearRegression? linearModel = null;
        RidgeRegression? ridgeModel = null;
        RandomForestRegressor? rfModel = null;
        GradientBoostingRegressor? gbModel = null;

        var bundleMetrics = new Dictionary<string, MetricsDto>();

        if (wantLinear)
        {
            linearModel = new LinearRegression();
            linearModel.Fit(trainX, trainY);
            bundleMetrics["linear"] = EvaluateAndPredict("Linear", linearModel);
        }

        if (wantRidge)
        {
            ridgeModel = RidgeRegression.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            ridgeModel.Fit(trainX, trainY);
            bundleMetrics["ridge"] = EvaluateAndPredict("Ridge", ridgeModel);
        }

        if (wantRF)
        {
            rfModel = RandomForestRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler, null
            );
            bundleMetrics["rf"] = EvaluateAndPredict("RF", rfModel);
        }

        if (wantGB)
        {
            gbModel = GradientBoostingRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            bundleMetrics["gb"] = EvaluateAndPredict("GB", gbModel);
        }

        if (ridgeModel != null && rfModel != null && gbModel != null)
        {
            var id = ModelNormalizer.Normalize(m.Model);
            var outPath = Path.Combine(processedDir, $"{id}.json");

            var dominantManufacturer = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Manufacturer))
                .GroupBy(r => r.Manufacturer?.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "";

            var displayModel = rows.FirstOrDefault()?.Model?.Trim() ?? m.Model;

            int totalRows = rows.Count;

            var bundle = ModelPersistence.ExportBundle(
                ridgeModel, rfModel, gbModel,
                fScaler, yScaler,
                fuels, transmissions,
                notes: $"model={m.Model}, rows={rows.Count}; anchorTargetYear={anchorYear}, totalRows={totalRows}",
                totalRows: totalRows
            );


            bundle.Car = new CarMetaDto
            {
                Manufacturer = dominantManufacturer,
                Model = displayModel,
                MinYear = minYear,
                MaxYear = maxYear
            };

            if (bundle.Preprocess != null)
            {
                bundle.Preprocess.AnchorTargetYear = anchorYear;
                bundle.Preprocess.MinYear = minYear;
                bundle.Preprocess.MaxYear = maxYear;
            }

            if (linearModel != null)
                bundle.Linear = ModelPersistence.ExportLinear(linearModel);

            bundle.Metrics = bundleMetrics;

            ModelPersistence.SaveBundle(bundle, outPath);
            Console.WriteLine($"Saved model bundle -> {outPath}");
        }
        else
        {
            Console.WriteLine($"Skipping save for '{m.Model}': not all models were trained.");
        }

        trained++;
        if (specificModel != null) break;
    }

    Console.WriteLine(specificModel == null
        ? $"Trained models: {trained}/{trainList.Count}"
        : $"Trained model: {trained}/1");

    return;
}

var defaultStartupBundlePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "Backend", "datasets", "processed", "current.bundle.json");

var startupBundlePath = builder.Configuration["Model:BundlePath"] ?? defaultStartupBundlePath;
var startupAlgorithm = builder.Configuration["Model:Algorithm"] ?? "ridge";

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