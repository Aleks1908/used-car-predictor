using Microsoft.Extensions.Hosting;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;
using used_car_predictor.Backend.Services;

namespace used_car_predictor.Backend.Training
{
    public static class CliTrainer
    {
        public static int Run(string[] args, IHostEnvironment env)
        {
            var opts = TrainingOptions.Parse(args);
            if (Environment.ExitCode != 0) return Environment.ExitCode;

            var datasetsRoot = Path.Combine(env.ContentRootPath, "Backend", "datasets");
            var rawDir = Path.Combine(datasetsRoot, "raw");
            var processedDir = EnsureDir(Path.Combine(datasetsRoot, "processed"));

            var csvPath = opts.CsvPath ?? Path.Combine(rawDir, "vehicles.csv");
            Console.WriteLine($"[CLI] Loading vehicles from: {csvPath}");
            Console.WriteLine($"[QualityGate] Defaults -> min R² = {opts.MinR2:F2}" +
                              (opts.MaxMAE.HasValue ? $", max MAE = {opts.MaxMAE.Value:F0}" : "") +
                              (opts.MaxRMSE.HasValue ? $", max RMSE = {opts.MaxRMSE.Value:F0}" : ""));

            var vehicles = CsvLoader.LoadVehicles(csvPath, opts.MaxRows);

            const int minCount = 50;
            var pairCounts = vehicles
                .Where(v => !string.IsNullOrWhiteSpace(v.Manufacturer) && !string.IsNullOrWhiteSpace(v.Model))
                .GroupBy(v => (
                    Make: ModelNormalizer.Normalize(v.Manufacturer!),
                    Model: ModelNormalizer.Normalize(v.Model!)
                ))
                .Select(g => new { g.Key.Make, g.Key.Model, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            List<(string Make, string Model, int Count)> trainList;
            if (!string.IsNullOrEmpty(opts.SpecificModelNormalized))
            {
                var entry = pairCounts.FirstOrDefault(x =>
                    x.Model == opts.SpecificModelNormalized &&
                    x.Make == opts.SpecificManufacturerNormalized);

                if (entry == null || entry.Count < minCount)
                {
                    Console.WriteLine($"No rows or insufficient data for '{opts.SpecificManufacturerNormalized} {opts.SpecificModelNormalized}'.");
                    return 1;
                }

                trainList = new List<(string, string, int)> { (entry.Make, entry.Model, entry.Count) };
            }
            else if (!string.IsNullOrEmpty(opts.SpecificManufacturerNormalized))
            {
                trainList = pairCounts
                    .Where(x => x.Make == opts.SpecificManufacturerNormalized && x.Count >= minCount)
                    .Select(x => (x.Make, x.Model, x.Count))
                    .ToList();

                if (trainList.Count == 0)
                {
                    Console.WriteLine($"No trainable models (>= {minCount} rows) for manufacturer '{opts.SpecificManufacturerNormalized}'.");
                    return 1;
                }
            }
            else
            {
                trainList = pairCounts
                    .Where(x => x.Count >= minCount)
                    .Select(x => (x.Make, x.Model, x.Count))
                    .ToList();

                if (trainList.Count == 0)
                {
                    Console.WriteLine($"No trainable (make, model) pairs with at least {minCount} rows.");
                    return 1;
                }
            }

            int trained = 0;
            int skipped = 0;

            foreach (var m in trainList)
            {
                var rows = vehicles
                    .Where(v =>
                        ModelNormalizer.Normalize(v.Manufacturer ?? "") == m.Make &&
                        ModelNormalizer.Normalize(v.Model ?? "") == m.Model)
                    .ToList();

                var yearVals = rows.Where(r => r.Year.HasValue).Select(r => r.Year!.Value).ToList();
                int? minYear = yearVals.Count > 0 ? yearVals.Min() : (int?)null;
                int? maxYear = yearVals.Count > 0 ? yearVals.Max() : (int?)null;

                var (rawX, rawY, fuels, transmissions) =
                    Preprocessor.ToMatrix(rows, targetYear: opts.AnchorYear, anchorTargetYear: opts.AnchorYear);

                if (fuels.Count == 0 || transmissions.Count == 0)
                {
                    Console.WriteLine($" SKIP -> {m.Make} {m.Model} because fuels/transmissions are empty.");
                    skipped++;
                    continue;
                }
                
                var (trainRawX, trainRawY, testRawX, testRawY) = DataSplitter.Split(rawX, rawY, trainRatio: 0.8);

                var fScaler = new FeatureScaler();
                var lScaler = new LabelScaler();

                var trainX = fScaler.FitTransform(trainRawX);
                var testX = fScaler.Transform(testRawX);

                var trainY = lScaler.FitTransform(trainRawY);
                var testY = lScaler.Transform(testRawY);

                static MetricsDto EvaluateAndTest(string displayName, IRegressor model,
                    double[,] testX, double[] testY, LabelScaler yScaler, string make, string modelName)
                {
                    var preds = yScaler.InverseTransform(model.Predict(testX));
                    var truth = yScaler.InverseTransform(testY);

                    double mae = Metrics.MeanAbsoluteError(truth, preds);
                    double rmse = Metrics.RootMeanSquaredError(truth, preds);
                    double r2 = Metrics.RSquared(truth, preds);

                    Console.WriteLine($"{make} {modelName,-18} [{displayName}] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
                    return new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
                }

                var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);

                LinearRegression? linearModel = null;
                RidgeRegression? ridgeModel = null;
                GradientBoostingRegressor? gbModel = null;
                RandomForestRegressor? rfModel = null;

                var bundleMetrics = new Dictionary<string, MetricsDto>();
                var trainingTimes = new Dictionary<string, TrainingTimeDto>
                {
                    ["Linear"] = new TrainingTimeDto { MeanTrialMs = null, Trials = null, TotalMs = null },
                    ["Ridge"] = new TrainingTimeDto { MeanTrialMs = null, Trials = null, TotalMs = null },
                    ["RandomForest"] = new TrainingTimeDto { MeanTrialMs = null, Trials = null, TotalMs = null },
                    ["GradientBoosting"] = new TrainingTimeDto { MeanTrialMs = null, Trials = null, TotalMs = null }
                };
                
                linearModel = new LinearRegression();
                linearModel.Fit(trainX, trainY);
                bundleMetrics["linear"] = EvaluateAndTest("Linear", linearModel, testX, testY, lScaler, m.Make, m.Model);
                trainingTimes["Linear"] = new TrainingTimeDto { TotalMs = linearModel.TotalMs };
                
                {
                    var (ridgeTrainedModel, ridgeMeanMs, ridgeTotalMs, ridgeTrials) =
                        RidgeRegression.TrainWithBestParamsKFold(
                            trainX, trainY, lScaler,
                            kFolds: 5, minExp: -9, maxExp: 3, alphaSteps: 40);

                    ridgeModel = ridgeTrainedModel;
                    ridgeModel.Fit(trainX, trainY);
                    bundleMetrics["ridge"] = EvaluateAndTest("Ridge", ridgeModel, testX, testY, lScaler, m.Make, m.Model);

                    trainingTimes["Ridge"] = new TrainingTimeDto
                    {
                        MeanTrialMs = ridgeMeanMs,
                        TotalMs = ridgeTotalMs,
                        Trials = ridgeTrials
                    };
                }
                
                static double[] ZPred(double[,] X, IRegressor model) => model.Predict(X);
                var ty_ridge = ZPred(tx, ridgeModel!);
                var vy_ridge = ZPred(vx, ridgeModel!);

                var trainRes = new double[ty.Length];
                var valRes = new double[vy.Length];
                for (int i = 0; i < ty.Length; i++) trainRes[i] = ty[i] - ty_ridge[i];
                for (int i = 0; i < vy.Length; i++) valRes[i] = vy[i] - vy_ridge[i];

                {
                    var (rfTrainedModel, rfMeanMs, rfTotalMs, rfTrials) =
                        RandomForestRegressor.TrainResidualsWithBestParams(
                            tx, trainRes, vx, valRes,
                            maxConfigs: opts.MaxConfigs,
                            searchSeed: null);

                    rfModel = rfTrainedModel;

                    var zTestRidge = ridgeModel!.Predict(testX);
                    var zTestRfRes = rfModel.Predict(testX);
                    var zCombined = new double[zTestRidge.Length];
                    for (int i = 0; i < zCombined.Length; i++) zCombined[i] = zTestRidge[i] + zTestRfRes[i];

                    var predsCombined = lScaler.InverseTransform(zCombined);
                    var truth = lScaler.InverseTransform(testY);

                    double mae = Metrics.MeanAbsoluteError(truth, predsCombined);
                    double rmse = Metrics.RootMeanSquaredError(truth, predsCombined);
                    double r2 = Metrics.RSquared(truth, predsCombined);

                    trainingTimes["RandomForest"] = new TrainingTimeDto
                    {
                        MeanTrialMs = rfMeanMs,
                        TotalMs = rfTotalMs,
                        Trials = rfTrials
                    };

                    bundleMetrics["ridge_rf"] = new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
                    Console.WriteLine($"{m.Make} {m.Model,-18} [Ridge+RF]  MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
                }

                {
                    var (gbTrainedModel, gbMeanTrialMs, gbTrials, gbTotalTime) =
                        GradientBoostingRegressor.TrainResidualsWithBestParams(
                            tx, trainRes, vx, valRes,
                            maxConfigs: opts.MaxConfigs,
                            searchSeed: null);

                    gbModel = gbTrainedModel;
                    Console.WriteLine($"[GB(res) search] avg {gbMeanTrialMs:F1} ms/trial over {gbTrials} trials");

                    trainingTimes["GradientBoosting"] = new TrainingTimeDto
                    {
                        MeanTrialMs = gbMeanTrialMs,
                        Trials = gbTrials,
                        TotalMs = gbTotalTime
                    };

                    var zTestRidge = ridgeModel!.Predict(testX);
                    var zTestGbRes = gbModel.Predict(testX);
                    var zCombined = new double[zTestRidge.Length];
                    for (int i = 0; i < zCombined.Length; i++) zCombined[i] = zTestRidge[i] + zTestGbRes[i];

                    var predsCombined = lScaler.InverseTransform(zCombined);
                    var truth = lScaler.InverseTransform(testY);

                    double mae = Metrics.MeanAbsoluteError(truth, predsCombined);
                    double rmse = Metrics.RootMeanSquaredError(truth, predsCombined);
                    double r2 = Metrics.RSquared(truth, predsCombined);

                    bundleMetrics["ridge_gb"] = new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
                    Console.WriteLine($"{m.Make} {m.Model,-18} [Ridge+GB]  MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
                }

                bool FailsThresholds(MetricsDto mm)
                    => (opts.MinR2.HasValue && mm.R2 < opts.MinR2.Value)
                       || (opts.MaxMAE.HasValue && mm.MAE > opts.MaxMAE.Value)
                       || (opts.MaxRMSE.HasValue && mm.RMSE > opts.MaxRMSE.Value);

                var failing = bundleMetrics
                    .Where(kv => FailsThresholds(kv.Value))
                    .Select(kv => $"{kv.Key} (R2={kv.Value.R2:F3}, MAE={kv.Value.MAE:F0}, RMSE={kv.Value.RMSE:F0})")
                    .ToList();

                if (failing.Count > 0)
                {
                    var displayRow = rows.FirstOrDefault();
                    var displayModel = displayRow?.Model?.Trim() ?? m.Model;
                    var displayMake  = displayRow?.Manufacturer?.Trim() ?? m.Make;

                    Console.WriteLine($"[QualityGate] SKIP -> {displayMake} {displayModel} " +
                                      $"(normalized: {m.Make} {m.Model}) due to failing metrics: {string.Join("; ", failing)}");
                    skipped++;
                    continue; 
                }

                {
                    var displayRow = rows.FirstOrDefault();
                    var displayModel = displayRow?.Model?.Trim() ?? m.Model;
                    var displayMake  = displayRow?.Manufacturer?.Trim() ?? m.Make;
                    int totalRows = rows.Count;

                    var bundle = ModelPersistence.ExportBundle(
                        ridgeModel!,
                        rfModel!,
                        gbModel!,
                        fScaler, lScaler, fuels, transmissions,
                        notes:
                        $"make={displayMake}, model={displayModel}, rows={rows.Count}; anchorTargetYear={opts.AnchorYear}, totalRows={totalRows}"
                    );

                    bundle.Car = new CarMetaDto
                    {
                        Manufacturer = displayMake,
                        Model = displayModel,
                        MinYear = minYear,
                        MaxYear = maxYear
                    };

                    if (bundle.Preprocess != null)
                    {
                        bundle.Preprocess.AnchorTargetYear = opts.AnchorYear;
                        bundle.Preprocess.MinYear = minYear;
                        bundle.Preprocess.MaxYear = maxYear;
                        try { bundle.Preprocess.TotalRows = totalRows; } catch { /* ignore */ }
                    }

                    if (linearModel != null)
                        bundle.Linear = ModelPersistence.ExportLinear(linearModel);

                    bundle.Metrics = bundleMetrics;
                    bundle.TrainingTimes = trainingTimes;

                    var fileId = BundleId.From(m.Make, m.Model);
                    var outPath = Path.Combine(processedDir, $"{fileId}.json");

                    ModelPersistence.SaveBundle(bundle, outPath);                    ModelPersistence.SaveBundle(bundle, outPath);
                    Console.WriteLine($"Saved model bundle -> {outPath}");

                    trained++;
                    if (opts.SpecificModelNormalized != null) break;
                }
            }

            if (opts.RequestedAnchorYear != opts.AnchorYear)
            {
                Console.WriteLine($"[Warning] Anchor year {opts.RequestedAnchorYear} was clamped to {opts.AnchorYear} ");
            }

            Console.WriteLine($"[Train] Anchor target year = {opts.AnchorYear}");
            Console.WriteLine($"[Train] Max hyperparameter search configs = {opts.MaxConfigs}");
            Console.WriteLine($"[Done] Training finished. Bundles trained: {trained}, skipped (quality): {skipped}");
            return 0;
        }

        private static string EnsureDir(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private static int ClampYear(int y)
        {
            int max = DateTime.UtcNow.Year + 10;
            return Math.Clamp(y, 1990, max);
        }

        private sealed class TrainingOptions
        {
            public string? CsvPath { get; private init; }
            public int MaxRows { get; private init; } = 1_000_000;

            public int RequestedAnchorYear { get; private init; } = 2030;
            public int AnchorYear { get; private init; } = 2030;

            public string? SpecificManufacturerNormalized { get; private init; }
            public string? SpecificModelNormalized { get; private init; }
            public int MaxConfigs { get; private init; } = 60;

            public double? MinR2 { get; private init; } = 0.5;
            public double? MaxMAE { get; private init; }
            public double? MaxRMSE { get; private init; }

            public static TrainingOptions Parse(string[] args)
            {
                static string? ArgValue(string[] a, string name)
                {
                    var i = Array.IndexOf(a, name);
                    return (i >= 0 && i + 1 < a.Length) ? a[i + 1] : null;
                }

                var csv           = ArgValue(args, "--csv");
                var maxRowsArg    = ArgValue(args, "--max");
                var anchorYearArg = ArgValue(args, "--anchor-year");
                var modelArg      = ArgValue(args, "--model");
                var makeArg       = ArgValue(args, "--manufacturer") ?? ArgValue(args, "--make");
                var maxConfigsArg = ArgValue(args, "--max-configs");

                var minR2Arg      = ArgValue(args, "--min-r2");
                var maxMaeArg     = ArgValue(args, "--max-mae");
                var maxRmseArg    = ArgValue(args, "--max-rmse");

                int maxRows = int.TryParse(maxRowsArg, out var mr) ? mr : 1_000_000;

                int requestedAnchorYear = int.TryParse(anchorYearArg, out var ayRaw) ? ayRaw : 2030;
                int anchorYear = ClampYear(requestedAnchorYear);

                int maxConfigs = int.TryParse(maxConfigsArg, out var mc) ? Math.Max(1, mc) : 60;

                string? modelNorm = null;
                if (!string.IsNullOrWhiteSpace(modelArg))
                    modelNorm = ModelNormalizer.Normalize(modelArg.Trim());

                string? makeNorm = null;
                if (!string.IsNullOrWhiteSpace(makeArg))
                    makeNorm = ModelNormalizer.Normalize(makeArg.Trim());

                if (modelNorm != null && makeNorm == null)
                {
                    Console.WriteLine("Error: when using --model, you must also pass --manufacturer <name>.");
                    Environment.ExitCode = 2;
                }

                double? minR2 = 0.5;
                if (double.TryParse(minR2Arg, out var minr2Parsed)) minR2 = minr2Parsed;

                double? maxMae = null;
                if (double.TryParse(maxMaeArg, out var maxMaeParsed)) maxMae = maxMaeParsed;

                double? maxRmse = null;
                if (double.TryParse(maxRmseArg, out var maxRmseParsed)) maxRmse = maxRmseParsed;

                return new TrainingOptions
                {
                    CsvPath = csv,
                    MaxRows = maxRows,
                    RequestedAnchorYear = requestedAnchorYear,
                    AnchorYear = anchorYear,
                    SpecificModelNormalized = modelNorm,
                    SpecificManufacturerNormalized = makeNorm,
                    MaxConfigs = maxConfigs,
                    MinR2 = minR2,
                    MaxMAE = maxMae,
                    MaxRMSE = maxRmse
                };
            }
        }
    }
}