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

// -------------------- EncodeManualInput --------------------
double[] EncodeManualInput(
    int year,
    int odometer,
    string fuel,
    string transmission,
    List<string> fuels,
    List<string> transmissions,
    int targetYear = 2025)
{
    var row = new double[9 + fuels.Count + transmissions.Count];
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

    // interaction / polynomial
    row[5 + fuels.Count + transmissions.Count] = age * mileagePerYear;
    row[6 + fuels.Count + transmissions.Count] = age * logOdometer;
    row[7 + fuels.Count + transmissions.Count] = Math.Pow(age, 3);
    row[8 + fuels.Count + transmissions.Count] = Math.Pow(mileagePerYear, 3);

    var f = (fuel ?? "").Trim().ToLower();
    for (int j = 0; j < fuels.Count; j++)
        row[5 + j] = f == fuels[j] ? 1.0 : 0.0;

    var t = (transmission ?? "").Trim().ToLower();
    int baseIdx = 5 + fuels.Count;
    for (int j = 0; j < transmissions.Count; j++)
        row[baseIdx + j] = t == transmissions[j] ? 1.0 : 0.0;

    return row;
}

// -------------------- CLI --------------------
if (args.Contains("--cli"))
{
    var csvPath = Path.Combine(AppContext.BaseDirectory, "Backend", "datasets", "raw", "vehicles.csv");
    var vehicles = CsvLoader.LoadVehicles(csvPath, 1_000_000);

    bool wantLinear = args.Contains("--linear");
    bool wantRidge = args.Contains("--ridge");
    bool wantTree = args.Contains("--tree");
    bool wantRF = args.Contains("--rf");
    bool wantGB = args.Contains("--gb");
    bool wantPredict = args.Contains("--predict");

    const int MinCount = 50;
    string? specificModel = null;
    int mIdx = Array.FindIndex(args, a => a == "--model");
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


        void EvaluateAndPredict(string name, IRegressor model)
        {
            var preds = yScaler.InverseTransform(model.Predict(testX));
            var truth = yScaler.InverseTransform(testY);
            Console.WriteLine(
                $"{m.Model,-22} [{name}] MAE={Metrics.MeanAbsoluteError(truth, preds),7:F0} RMSE={Metrics.RootMeanSquaredError(truth, preds),7:F0} R²={Metrics.RSquared(truth, preds),5:F3}");

            if (wantPredict)
            {
                var manualRow = EncodeManualInput(2016, 100000, "gas", "automatic", fuels, transmissions);
                var manualPred = yScaler.InverseTransform(new double[] { model.Predict(manualRow) })[0];
                Console.WriteLine($"{m.Model,-22} [{name}] Predicted manual price: {manualPred:F0}");
            }
        }


        // Train/validation split for hyperparameter tuning
        var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);

        if (wantRidge)
        {
            var ridge = RidgeRegression.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            ridge.Fit(trainX, trainY);
            EvaluateAndPredict("Ridge", ridge);
        }

        if (wantRF)
        {
            var rf = RandomForestRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            EvaluateAndPredict("RF", rf);
        }

        if (wantGB)
        {
            var gb = GradientBoostingRegressor.TrainWithBestParams(tx, ty, vx, vy, yScaler);
            EvaluateAndPredict("GB", gb);
        }

        trained++;
        if (specificModel != null) break;
    }

    Console.WriteLine(specificModel == null
        ? $"Trained models: {trained}/{trainList.Count}"
        : $"Trained model: {trained}/1");

    return;
}

app.Run();