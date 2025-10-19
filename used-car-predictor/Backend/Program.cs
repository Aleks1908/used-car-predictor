using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;
using used_car_predictor.Backend.Services;
using used_car_predictor.Backend.Api;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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


app.MapGet("/health", (ActiveModel active) =>
{
    return active.IsLoaded
        ? Results.Ok(new { status = "ok", modelVersion = active.Version, trainedAt = active.TrainedAt })
        : Results.StatusCode(503);
});

app.MapPost("/api/v1/predict", async (PredictRequest req, ActiveModel active, ModelHotLoader hotLoader) =>
    {
        await hotLoader.EnsureLoadedAsync(req.Make, req.Model);
        if (!active.IsLoaded) return Results.Problem("No active model loaded.", statusCode: 503);

        var raw = EncodeManualInput(
            req.Year, req.MileageKm, req.FuelType, req.Transmission,
            active.Fuels.ToList(), active.Transmissions.ToList(),
            targetYear: req.Year);

        var x = ScaleRow(raw, active.FeatureMeans, active.FeatureStds);

        var zByAlgo = active.PredictAllScaled(x);

        var results = new List<ModelPredictionDto>(zByAlgo.Count);
        foreach (var (algo, z) in zByAlgo)
        {
            var price = InverseLabel(z, active.LabelMean, active.LabelStd, active.LabelUseLog);
            active.MetricsByAlgo.TryGetValue(algo, out var m);

            results.Add(new ModelPredictionDto
            {
                Algorithm = algo,
                PredictedPrice = Math.Round((decimal)price, 2),
                Metrics = new ModelMetricsDto { Mse = m.Mse, Mae = m.Mae, R2 = m.R2 }
            });
        }

        var info = new ModelInfoDto { Version = active.Version, TrainedAt = active.TrainedAt };

        return Results.Ok(new PredictResponse
        {
            Make = req.Make, Model = req.Model, Year = req.Year,
            Currency = "EUR",
            Results = results,
            ModelInfo = info
        });
    })
    .WithName("PredictPrice");

app.MapPost("/api/v1/predict/range", async (PredictRangeRequest req, ActiveModel active, ModelHotLoader hotLoader) =>
    {
        await hotLoader.EnsureLoadedAsync(req.Make, req.Model);
        if (!active.IsLoaded) return Results.Problem("No active model loaded.", statusCode: 503);
        if (req.FromYear > req.ToYear) return Results.BadRequest(new { error = "FromYear must be <= ToYear" });

        var items = new List<PredictResponse>();
        for (int y = req.FromYear; y <= req.ToYear; y++)
        {
            var raw = EncodeManualInput(
                y, req.MileageKm, req.FuelType, req.Transmission,
                active.Fuels.ToList(), active.Transmissions.ToList(),
                targetYear: y);

            var x = ScaleRow(raw, active.FeatureMeans, active.FeatureStds);
            var zByAlgo = active.PredictAllScaled(x);

            var results = new List<ModelPredictionDto>(zByAlgo.Count);
            foreach (var (algo, z) in zByAlgo)
            {
                var price = InverseLabel(z, active.LabelMean, active.LabelStd, active.LabelUseLog);
                active.MetricsByAlgo.TryGetValue(algo, out var m);

                results.Add(new ModelPredictionDto
                {
                    Algorithm = algo,
                    PredictedPrice = Math.Round((decimal)price, 2),
                    Metrics = new ModelMetricsDto { Mse = m.Mse, Mae = m.Mae, R2 = m.R2 }
                });
            }

            items.Add(new PredictResponse
            {
                Make = req.Make, Model = req.Model, Year = y,
                Currency = "EUR",
                Results = results,
                ModelInfo = new ModelInfoDto { Version = active.Version, TrainedAt = active.TrainedAt }
            });
        }

        var info = new ModelInfoDto { Version = active.Version, TrainedAt = active.TrainedAt };

        return Results.Ok(new PredictRangeResponse
        {
            Currency = "EUR",
            Items = items,
            ModelInfo = info
        });
    })
    .WithName("PredictPriceRange");


app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from .NET 9!" }));

app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(reactBuildPath)
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

double[] EncodeManualInput(
    int year,
    int odometer,
    string fuel,
    string transmission,
    List<string> fuels,
    List<string> transmissions,
    int targetYear = 2030)
{
    var row = new double[9 + fuels.Count + transmissions.Count];

    int age = Math.Max(0, targetYear - year);
    double odo = Math.Max(0.0, odometer);
    double mileagePerYear = odo / (age + 1.0);
    double logOdometer = Math.Log(odo + 1.0);
    double age2 = age * age;

    row[0] = age;
    row[1] = odo;
    row[2] = mileagePerYear;
    row[3] = logOdometer;
    row[4] = age2;

    var f = (fuel ?? "").Trim().ToLowerInvariant();
    var t = (transmission ?? "").Trim().ToLowerInvariant();

    for (int j = 0; j < fuels.Count; j++)
        row[5 + j] = f == fuels[j] ? 1.0 : 0.0;

    int baseIdx = 5 + fuels.Count;
    for (int j = 0; j < transmissions.Count; j++)
        row[baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

    int k = baseIdx + transmissions.Count;
    row[k + 0] = age * logOdometer;
    row[k + 1] = mileagePerYear * mileagePerYear;
    row[k + 2] = age * age * age;
    row[k + 3] = mileagePerYear * mileagePerYear * mileagePerYear;

    return row;
}

static double[] ScaleRow(double[] raw, double[] means, double[] stds)
{
    if (means.Length != raw.Length || stds.Length != raw.Length)
        throw new InvalidOperationException("Feature scaler shape mismatch.");
    var x = new double[raw.Length];
    for (int i = 0; i < raw.Length; i++)
        x[i] = stds[i] > 0 ? (raw[i] - means[i]) / stds[i] : 0.0;
    return x;
}

static double InverseLabel(double yScaled, double mean, double std, bool useLog)
{
    var unscaled = yScaled * std + mean;
    return useLog ? Math.Exp(unscaled) - 1.0 : unscaled;
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

    bool wantLinear = args.Contains("--linear");
    bool wantRidge = args.Contains("--ridge");
    bool wantRF = args.Contains("--rf");
    bool wantGB = args.Contains("--gb");
    bool wantPredict = args.Contains("--predict");
    if (!wantLinear && !wantRidge && !wantRF && !wantGB)
        wantLinear = wantRidge = wantRF = wantGB = true;

    int manualYear = 2016, manualOdo = 100000;
    string manualFuel = "gas", manualTrans = "automatic";
    var manualIdx = Array.FindIndex(args, a => a == "--manual");
    if (manualIdx >= 0 && manualIdx + 4 < args.Length)
    {
        int.TryParse(args[manualIdx + 1], out manualYear);
        int.TryParse(args[manualIdx + 2], out manualOdo);
        manualFuel = args[manualIdx + 3] ?? "gas";
        manualTrans = args[manualIdx + 4] ?? "automatic";
        wantPredict = true;
    }

    bool debug = args.Contains("--debug");

    var loadBundleArg = ArgValue(args, "--load-bundle");
    if (!string.IsNullOrEmpty(loadBundleArg))
    {
        string bundlePath = loadBundleArg.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(loadBundleArg)
            : Path.Combine(processedDir, ModelNormalizer.Normalize(loadBundleArg) + ".json");

        if (!File.Exists(bundlePath))
        {
            Console.WriteLine($"Bundle not found: {bundlePath}");
            return;
        }

        var bundle = ModelPersistence.LoadBundle(bundlePath);

        LinearRegression? linear = null;
        if (bundle.Linear?.Weights != null && bundle.Linear.Weights.Length > 0)
            linear = ModelPersistence.ImportLinear(bundle.Linear);

        var ridge = ModelPersistence.ImportRidge(bundle.Ridge);
        var rf = ModelPersistence.ImportRandomForest(bundle.RandomForest);
        var gb = ModelPersistence.ImportGradientBoosting(bundle.GradientBoosting);
        var fScaler =
            ModelPersistence.ImportFeatureScaler(bundle.Preprocess.FeatureMeans, bundle.Preprocess.FeatureStds);
        var yScaler = ModelPersistence.ImportLabelScaler(bundle.Preprocess.LabelMean, bundle.Preprocess.LabelStd,
            bundle.Preprocess.LabelUseLog);
        var fuels = bundle.Preprocess.Fuels;
        var transmissions = bundle.Preprocess.Transmissions;

        var row = EncodeManualInput(manualYear, manualOdo, manualFuel, manualTrans, fuels, transmissions);
        var x = fScaler.TransformRow(row);

        int p = x.Length;
        if (bundle.Preprocess.FeatureMeans.Length != p)
        {
            Console.WriteLine(
                $"[error] Feature means length ({bundle.Preprocess.FeatureMeans.Length}) != feature length ({p})");
            return;
        }

        if (bundle.Ridge?.Weights == null || bundle.Ridge.Weights.Length != p)
        {
            Console.WriteLine(
                $"[error] Ridge weights length ({bundle.Ridge?.Weights?.Length ?? 0}) != feature length ({p})");
            return;
        }

        if (linear != null && (bundle.Linear?.Weights?.Length ?? 0) != p)
        {
            Console.WriteLine(
                $"[error] Linear weights length ({bundle.Linear!.Weights.Length}) != feature length ({p})");
            return;
        }

        if (debug)
        {
            Console.WriteLine(
                $"[debug] dims: row={row.Length}, scaled={x.Length}, means={bundle.Preprocess.FeatureMeans.Length}, " +
                $"ridge.w={bundle.Ridge.Weights.Length}, rf.trees={bundle.RandomForest?.Trees?.Count ?? 0}, " +
                $"gb.trees={bundle.GradientBoosting?.Trees?.Count ?? 0}");
        }

        var preds = new List<(string key, string label, double val)>();

        if (linear != null)
        {
            var pl = yScaler.InverseTransform(new[] { linear.Predict(x) })[0];
            preds.Add(("linear", "Linear", pl));
        }

        var pr = yScaler.InverseTransform(new[] { ridge.Predict(x) })[0];
        preds.Add(("ridge", "Ridge", pr));

        var pf = yScaler.InverseTransform(new[] { rf.Predict(x) })[0];
        preds.Add(("rf", "RF", pf));

        var pg = yScaler.InverseTransform(new[] { gb.Predict(x) })[0];
        preds.Add(("gb", "GB", pg));

        if (debug)
        {
            var zRidge = ridge.Predict(x);
            var yLogRidge = bundle.Preprocess.LabelMean + bundle.Preprocess.LabelStd * zRidge;
            var yRidge = bundle.Preprocess.LabelUseLog ? Math.Exp(yLogRidge) - 1.0 : yLogRidge;

            double dot = bundle.Ridge.Bias;
            for (int j = 0; j < p; j++) dot += bundle.Ridge.Weights[j] * x[j];

            Console.WriteLine($"[debug] ridge z={zRidge:F4} dot={dot:F4} -> y={yRidge:F2}");
        }

        foreach (var (key, label, val) in preds)
            Console.WriteLine($"{label.ToLower()}={val:F0}");

        double mean = 0;
        foreach (var t in preds) mean += t.val;
        mean /= preds.Count;
        Console.WriteLine($"mean={mean:F0}");

        if (bundle.Metrics != null && bundle.Metrics.Count > 0)
        {
            void Print(string k, string label)
            {
                if (bundle.Metrics.TryGetValue(k, out var m))
                    Console.WriteLine($"[{label}] metrics: MAE={m.MAE:F0} RMSE={m.RMSE:F0} R²={m.R2:F3}");
            }

            Print("linear", "Linear");
            Print("ridge", "Ridge");
            Print("rf", "RF");
            Print("gb", "GB");
        }
        else
        {
            Console.WriteLine("[info] No metrics stored in bundle.");
        }

        return;
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
        var (features, labels, fuels, transmissions) = Preprocessor.ToMatrix(rows);

        var fScaler = new FeatureScaler();
        features = fScaler.FitTransform(features);
        var yScaler = new LabelScaler();
        var yScaled = yScaler.FitTransform(labels);
        var (trainX, trainY, testX, testY) = DataSplitter.Split(features, yScaled, trainRatio: 0.8);

        MetricsDto EvaluateAndPredict(string name, IRegressor model)
        {
            var preds = yScaler.InverseTransform(model.Predict(testX));
            var truth = yScaler.InverseTransform(testY);

            double mae = Metrics.MeanAbsoluteError(truth, preds);
            double rmse = Metrics.RootMeanSquaredError(truth, preds);
            double r2 = Metrics.RSquared(truth, preds);

            Console.WriteLine($"{m.Model,-22} [{name}] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");

            if (wantPredict)
            {
                var manualRow = EncodeManualInput(manualYear, manualOdo, manualFuel, manualTrans, fuels, transmissions);
                var manualX = fScaler.TransformRow(manualRow);
                var manualPred = yScaler.InverseTransform(new double[] { model.Predict(manualX) })[0];
                Console.WriteLine($"{m.Model,-22} [{name}] Predicted manual price: {manualPred:F0}");
            }

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
            rfModel = RandomForestRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
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

            var bundle = ModelPersistence.ExportBundle(
                ridgeModel, rfModel, gbModel,
                fScaler, yScaler,
                fuels, transmissions,
                notes: $"model={m.Model}, rows={rows.Count}"
            );

            if (linearModel != null)
                bundle.Linear = ModelPersistence.ExportLinear(linearModel);

            bundle.Metrics = bundleMetrics;

            ModelPersistence.SaveBundle(bundle, outPath);
            Console.WriteLine($"Saved model bundle -> {outPath}");
        }
        else
        {
            Console.WriteLine(
                $"Skipping save for '{m.Model}': not all models were trained. (Use no flags or include --linear --ridge --rf --gb)");
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
        Console.WriteLine(
            $"[Model] Loaded '{startupAlgorithm}' bundle: {active.Version} (trained {active.TrainedAt:u})");
    }
    else
    {
        Console.WriteLine(
            $"[Model] Bundle not found at '{startupBundlePath}'. Endpoints will return 503 until loaded.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Model] Failed to load bundle: {ex.Message}");
}


app.Run();


public interface IBundleResolver
{
    (string Path, string Algorithm) Resolve(string make, string model);
}

public sealed class StaticBundleResolver : IBundleResolver
{
    private readonly string _processedDir;
    private readonly string _defaultAlgorithm;

    public StaticBundleResolver(IHostEnvironment env, IConfiguration cfg)
    {
        _processedDir = Path.Combine(env.ContentRootPath, "Backend", "datasets", "processed");
        _defaultAlgorithm = cfg["Model:Algorithm"] ?? "ridge";
    }

    public (string Path, string Algorithm) Resolve(string make, string model)
    {
        var id = ModelNormalizer.Normalize(model);
        var path = Path.Combine(_processedDir, $"{id}.json");
        return (path, _defaultAlgorithm);
    }
}

public sealed class ModelHotLoader
{
    private readonly ActiveModel _active;
    private readonly IBundleResolver _resolver;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _currentKey;

    public ModelHotLoader(ActiveModel active, IBundleResolver resolver)
    {
        _active = active;
        _resolver = resolver;
    }

    public async Task EnsureLoadedAsync(string make, string model, CancellationToken ct = default)
    {
        var key = ModelNormalizer.Normalize(model);

        if (_active.IsLoaded && _currentKey == key) return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_active.IsLoaded && _currentKey == key) return;

            var (path, algorithm) = _resolver.Resolve(make, model);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Bundle not found: {path}");

            _active.LoadFromBundle(path, algorithm);
            _currentKey = key;
            Console.WriteLine($"[Model] Hot-swapped: '{key}' via {algorithm} ({_active.Version})");
        }
        finally
        {
            _gate.Release();
        }
    }
}