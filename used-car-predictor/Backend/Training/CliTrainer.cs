using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Training
{
    public static class CliTrainer
    {
        public static int Run(string[] args, IHostEnvironment env)
        {
            var opts = TrainingOptions.Parse(args);

            var datasetsRoot = Path.Combine(env.ContentRootPath, "Backend", "datasets");
            var rawDir = Path.Combine(datasetsRoot, "raw");
            var processedDir = EnsureDir(Path.Combine(datasetsRoot, "processed"));

            var csvPath = opts.CsvPath ?? Path.Combine(rawDir, "vehicles.csv");
            Console.WriteLine($"[CLI] Loading vehicles from: {csvPath}");
            var vehicles = CsvLoader.LoadVehicles(csvPath, opts.MaxRows);

            const int minCount = 50;
            var modelCounts = vehicles
                .Where(v => !string.IsNullOrWhiteSpace(v.Model))
                .GroupBy(v => ModelNormalizer.Normalize(v.Model))
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            List<(string Model, int Count)> trainList;
            if (!string.IsNullOrEmpty(opts.SpecificModelNormalized))
            {
                var entry = modelCounts.FirstOrDefault(x => x.Model == opts.SpecificModelNormalized);
                if (entry == null || entry.Count < minCount)
                {
                    Console.WriteLine($"No rows or insufficient data for model '{opts.SpecificModelNormalized}'");
                    return 1;
                }

                trainList = [(entry.Model, entry.Count)];
            }
            else
            {
                trainList = modelCounts.Where(x => x.Count >= minCount)
                    .Select(x => (x.Model, x.Count)).ToList();
            }

            var trained = 0;

            foreach (var m in trainList)
            {
                var rows = vehicles.Where(v => ModelNormalizer.Normalize(v.Model) == m.Model).ToList();

                var yearVals = rows.Where(r => r.Year.HasValue).Select(r => r.Year!.Value).ToList();
                int? minYear = yearVals.Count > 0 ? yearVals.Min() : null;
                int? maxYear = yearVals.Count > 0 ? yearVals.Max() : null;

                var (rawX, rawY, fuels, transmissions) =
                    Preprocessor.ToMatrix(rows, targetYear: opts.AnchorYear, anchorTargetYear: opts.AnchorYear);

                var (trainRawX, trainRawY, testRawX, testRawY) = DataSplitter.Split(rawX, rawY, trainRatio: 0.8);

                var fScaler = new FeatureScaler();
                var lScaler = new LabelScaler();

                var trainX = fScaler.FitTransform(trainRawX);
                var testX = fScaler.Transform(testRawX);

                var trainY = lScaler.FitTransform(trainRawY);
                var testY = lScaler.Transform(testRawY);

                static MetricsDto EvaluateAndTest(string modelName, string displayName,
                    IRegressor model, double[,] testX, double[] testY, LabelScaler yScaler)
                {
                    var preds = yScaler.InverseTransform(model.Predict(testX));
                    var truth = yScaler.InverseTransform(testY);

                    double mae = Metrics.MeanAbsoluteError(truth, preds);
                    double rmse = Metrics.RootMeanSquaredError(truth, preds);
                    double r2 = Metrics.RSquared(truth, preds);

                    Console.WriteLine($"{modelName,-22} [{displayName}] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
                    return new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
                }

                var (tx, ty, vx, vy) = DataSplitter.Split(trainX, trainY, trainRatio: 0.75);

                RandomForestRegressor? rfModel;
                GradientBoostingRegressor? gbModel;

                var bundleMetrics = new Dictionary<string, MetricsDto>();

                var trainingTimes = new Dictionary<string, TrainingTimeDto>
                {
                    ["Linear"] = new TrainingTimeDto(),
                    ["Ridge"] = new TrainingTimeDto(),
                    ["RandomForest"] = new TrainingTimeDto(),
                    ["GradientBoosting"] = new TrainingTimeDto()
                };
                
                var linearModel = new LinearRegression();
                linearModel.Fit(trainX, trainY);
                bundleMetrics["linear"] = EvaluateAndTest(m.Model, "Linear", linearModel, testX, testY, lScaler);
                trainingTimes["Linear"] = new TrainingTimeDto { TotalMs = linearModel.TotalMs };

                var (ridgeTrainedModel, ridgeMeanMs, ridgeTotalMs, ridgeTrials) =
                    RidgeRegression.TrainWithBestParamsKFold(
                        trainX, trainY, lScaler,
                        kFolds: 5, minExp: -9, maxExp: 3, alphaSteps: 40);

                var ridgeModel = ridgeTrainedModel;
                ridgeModel.Fit(trainX, trainY);
                bundleMetrics["ridge"] = EvaluateAndTest(m.Model, "Ridge", ridgeModel, testX, testY, lScaler);

                trainingTimes["Ridge"] = new TrainingTimeDto
                {
                    MeanTrialMs = ridgeMeanMs,
                    TotalMs = ridgeTotalMs,
                    Trials = ridgeTrials
                };

                static double[] ZPred(double[,] x, IRegressor mod) => mod.Predict(x);
                var tyRidge = ZPred(tx, ridgeModel);
                var vyRidge = ZPred(vx, ridgeModel);

                var trainRes = new double[ty.Length];
                var valRes = new double[vy.Length];
                for (int i = 0; i < ty.Length; i++) trainRes[i] = ty[i] - tyRidge[i];
                for (int i = 0; i < vy.Length; i++) valRes[i] = vy[i] - vyRidge[i];

                {
                    var (rfTrainedModel, rfMeanMs, rfTotalMs, rfTrials) =
                        RandomForestRegressor.TrainResidualsWithBestParams(
                            tx, trainRes, vx, valRes,
                            maxConfigs: opts.MaxConfigs,
                            searchSeed: null);

                    rfModel = rfTrainedModel;

                    var zTestRidge = ridgeModel.Predict(testX);
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
                    Console.WriteLine($"{m.Model,-22} [Ridge+RF] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
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

                    var zTestRidge = ridgeModel.Predict(testX);
                    var zTestGbRes = gbModel.Predict(testX);
                    var zCombined = new double[zTestRidge.Length];
                    for (int i = 0; i < zCombined.Length; i++) zCombined[i] = zTestRidge[i] + zTestGbRes[i];

                    var predsCombined = lScaler.InverseTransform(zCombined);
                    var truth = lScaler.InverseTransform(testY);

                    double mae = Metrics.MeanAbsoluteError(truth, predsCombined);
                    double rmse = Metrics.RootMeanSquaredError(truth, predsCombined);
                    double r2 = Metrics.RSquared(truth, predsCombined);

                    bundleMetrics["ridge_gb"] = new MetricsDto { MAE = mae, RMSE = rmse, R2 = r2 };
                    Console.WriteLine($"{m.Model,-22} [Ridge+GB] MAE={mae,7:F0} RMSE={rmse,7:F0} R²={r2,5:F3}");
                }

                var id = ModelNormalizer.Normalize(m.Model);
                var outPath = Path.Combine(processedDir, $"{id}.json");

                var dominantManufacturer = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Manufacturer))
                    .GroupBy(r => r.Manufacturer!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "";

                var displayModel = rows.FirstOrDefault()?.Model?.Trim() ?? m.Model;
                int totalRows = rows.Count;

                var bundle = ModelPersistence.ExportBundle(
                    ridgeModel,
                    rfModel, 
                    gbModel, 
                    fScaler, lScaler, fuels, transmissions,
                    notes:
                    $"model={m.Model}, rows={rows.Count}; anchorTargetYear={opts.AnchorYear}, totalRows={totalRows}"
                );

                bundle.Car = new CarMetaDto
                {
                    Manufacturer = dominantManufacturer,
                    Model = displayModel,
                    MinYear = minYear,
                    MaxYear = maxYear
                };

                bundle.Preprocess.AnchorTargetYear = opts.AnchorYear;
                bundle.Preprocess.MinYear = minYear;
                bundle.Preprocess.MaxYear = maxYear;
                try
                {
                    bundle.Preprocess.TotalRows = totalRows;
                }
                catch
                {
                    /* ignore */
                }

                bundle.Linear = ModelPersistence.ExportLinear(linearModel);
                bundle.Metrics = bundleMetrics;
                bundle.TrainingTimes = trainingTimes;

                ModelPersistence.SaveBundle(bundle, outPath);
                Console.WriteLine($"Saved model bundle -> {outPath}");

                trained++;
                if (opts.SpecificModelNormalized != null) break;
            }

            if (opts.RequestedAnchorYear != opts.AnchorYear)
            {
                Console.WriteLine(
                    $"[Warning] Anchor year {opts.RequestedAnchorYear} was clamped to {opts.AnchorYear} ");
            }

            Console.WriteLine($"[Train] Anchor target year = {opts.AnchorYear}");
            Console.WriteLine($"[Train] Max hyperparameter search configs = {opts.MaxConfigs}");
            Console.WriteLine($"[Done] Trained finished");
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

            public string? SpecificModelNormalized { get; private init; }
            public int MaxConfigs { get; private init; } = 60;

            public static TrainingOptions Parse(string[] args)
            {
                static string? ArgValue(string[] a, string name)
                {
                    var i = Array.IndexOf(a, name);
                    return (i >= 0 && i + 1 < a.Length) ? a[i + 1] : null;
                }

                var csv = ArgValue(args, "--csv");
                var maxRowsArg = ArgValue(args, "--max");
                var anchorYearArg = ArgValue(args, "--anchor-year");
                var modelArg = ArgValue(args, "--model");
                var maxConfigsArg = ArgValue(args, "--max-configs");

                int maxRows = int.TryParse(maxRowsArg, out var mr) ? mr : 1_000_000;

                int requestedAnchorYear = int.TryParse(anchorYearArg, out var ayRaw) ? ayRaw : 2030;
                int anchorYear = ClampYear(requestedAnchorYear);

                int maxConfigs = int.TryParse(maxConfigsArg, out var mc) ? Math.Max(1, mc) : 60;

                string? specific = null;
                if (!string.IsNullOrWhiteSpace(modelArg))
                    specific = ModelNormalizer.Normalize(modelArg.Trim());

                return new TrainingOptions
                {
                    CsvPath = csv,
                    MaxRows = maxRows,
                    RequestedAnchorYear = requestedAnchorYear,
                    AnchorYear = anchorYear,
                    SpecificModelNormalized = specific,
                    MaxConfigs = maxConfigs
                };
            }
        }
    }
}