using System.Reflection;
using System.Text.Json;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;


namespace used_car_predictor.Backend.Serialization
{
    public static class ModelPersistence
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };


        public static void SaveBundle(BundleDto dto, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(path, json);
        }

        public static BundleDto LoadBundle(string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<BundleDto>(json, JsonOptions)
                      ?? throw new InvalidOperationException($"Could not parse bundle at {path}");

            return dto;
        }


        public static BundleDto ExportBundle(
            RidgeRegression ridge,
            RandomForestRegressor rf,
            GradientBoostingRegressor gb,
            FeatureScaler fScaler,
            LabelScaler yScaler,
            List<string> fuels,
            List<string> transmissions,
            string? notes = null,
            int? anchorTargetYear = null,
            int? minYear = null,
            int? maxYear = null, int totalRows = 0)
        {
            return new BundleDto
            {
                TrainedAtUtc = DateTime.UtcNow,
                Notes = notes,
                Preprocess = ExportPreprocess(
                    fScaler, yScaler, fuels, transmissions,
                    anchorTargetYear, minYear, maxYear, totalRows),
                Ridge = ExportRidge(ridge),
                RandomForest = ExportRandomForest(rf),
                GradientBoosting = ExportGradientBoosting(gb)
            };
        }


        public static PreprocessDto ExportPreprocess(
            FeatureScaler fScaler,
            LabelScaler yScaler,
            List<string> fuels,
            List<string> transmissions,
            int? anchorTargetYear = null,
            int? minYear = null,
            int? maxYear = null,
            int totalRows = 0)
        {
            var (means, stds) = GetFeatureScalerState(fScaler);
            var (yMean, yStd, yUseLog) = GetLabelScalerState(yScaler);

            return new PreprocessDto
            {
                Fuels = fuels?.ToList() ?? [],
                Transmissions = transmissions?.ToList() ?? [],
                FeatureMeans = means,
                FeatureStds = stds,
                LabelMean = yMean,
                LabelStd = yStd,
                LabelUseLog = yUseLog,
                AnchorTargetYear = anchorTargetYear,
                MinYear = minYear,
                MaxYear = maxYear,
                TotalRows = totalRows
            };
        }

        private static (double[] Means, double[] Stds) GetFeatureScalerState(FeatureScaler fScaler)
        {
            var t = typeof(FeatureScaler);
            var fMeans = t.GetField("means", Inst) ?? t.GetField("_means", Inst);
            var fStds = t.GetField("stds", Inst) ?? t.GetField("_stds", Inst);
            if (fMeans == null || fStds == null)
                throw new MissingFieldException("FeatureScaler fields for means not found.");

            var means = (double[])fMeans.GetValue(fScaler)!;
            var stds = (double[])fStds.GetValue(fScaler)!;
            if (means == null || stds == null) throw new NullReferenceException("Scaler state is null.");
            return (means, stds);
        }

        private static (double Mean, double Std, bool UseLog) GetLabelScalerState(LabelScaler yScaler)
        {
            var t = typeof(LabelScaler);
            var fMean = t.GetField("_mean", Inst) ?? t.GetField("mean", Inst);
            var fStd = t.GetField("_std", Inst) ?? t.GetField("std", Inst);
            var fUseLog = t.GetField("_useLog", Inst) ?? t.GetField("useLog", Inst);

            if (fMean == null || fStd == null)
                throw new MissingFieldException("LabelScaler fields for mean/std not found.");

            var mean = (double)fMean.GetValue(yScaler)!;
            var std = (double)fStd.GetValue(yScaler)!;
            bool useLog = fUseLog != null && (bool)fUseLog.GetValue(yScaler)!;
            return (mean, std, useLog);
        }


        public static LinearDto ExportLinear(LinearRegression model)
        {
            var t = typeof(LinearRegression);

            var weightsField = t.GetField("_weights", Inst)
                               ?? t.GetField("weights", Inst)
                               ?? t.GetField("Coefficients", Inst);

            var biasField = t.GetField("_bias", Inst)
                            ?? t.GetField("bias", Inst)
                            ?? t.GetField("Intercept", Inst);

            if (weightsField == null)
                throw new InvalidOperationException("Could not locate weights field on LinearRegression.");

            var weightsObj = weightsField.GetValue(model);
            var weights = (double[])(weightsObj ?? Array.Empty<double>());

            double bias = 0.0;
            if (biasField != null) bias = (double)biasField.GetValue(model)!;

            return new LinearDto { Weights = weights, Bias = bias };
        }

        public static LinearRegression ImportLinear(LinearDto dto)
        {
            var model = new LinearRegression();
            var t = typeof(LinearRegression);

            var weightsField = t.GetField("_weights", Inst)
                               ?? t.GetField("weights", Inst)
                               ?? t.GetField("Coefficients", Inst);

            var biasField = t.GetField("_bias", Inst)
                            ?? t.GetField("bias", Inst)
                            ?? t.GetField("Intercept", Inst);

            if (weightsField == null)
                throw new InvalidOperationException("Could not locate weights field on LinearRegression.");

            weightsField.SetValue(model, dto.Weights);
            if (biasField != null) biasField.SetValue(model, dto.Bias);
            return model;
        }


        public static RidgeDto ExportRidge(RidgeRegression model)
        {
            var w = (double[])typeof(RidgeRegression).GetField("_weights", Inst)!.GetValue(model)!;
            var b = (double)typeof(RidgeRegression).GetField("_bias", Inst)!.GetValue(model)!;

            double alpha = 1.0;
            var alphaF = typeof(RidgeRegression).GetField("_alpha", Inst);
            if (alphaF != null)
            {
                var av = alphaF.GetValue(model);
                if (av is double d) alpha = d;
            }

            return new RidgeDto
            {
                Weights = w,
                Bias = b,
                Alpha = alpha
            };
        }

        public static RidgeRegression ImportRidge(RidgeDto dto)
        {
            var model = new RidgeRegression();
            typeof(RidgeRegression).GetField("_weights", Inst)!.SetValue(model, dto.Weights);
            typeof(RidgeRegression).GetField("_bias", Inst)!.SetValue(model, dto.Bias);

            var alphaF = typeof(RidgeRegression).GetField("_alpha", Inst);
            if (alphaF != null) alphaF.SetValue(model, dto.Alpha);

            return model;
        }


        public static RandomForestDto ExportRandomForest(RandomForestRegressor model)
        {
            var dto = new RandomForestDto();

            dto.NumTrees = (int)(typeof(RandomForestRegressor).GetField("_nEstimators", Inst)?.GetValue(model) ?? 0);
            dto.MaxDepth = (int)(typeof(RandomForestRegressor).GetField("_maxDepth", Inst)?.GetValue(model) ?? 10);
            dto.MinSamplesSplit =
                (int)(typeof(RandomForestRegressor).GetField("_minSamplesSplit", Inst)?.GetValue(model) ?? 2);
            dto.MinSamplesLeaf =
                (int)(typeof(RandomForestRegressor).GetField("_minSamplesLeaf", Inst)?.GetValue(model) ?? 1);

            var trees = (IEnumerable<DecisionTreeRegressor>)typeof(RandomForestRegressor)
                .GetField("_trees", Inst)!.GetValue(model)!;

            dto.Trees = trees.Select(ExportDecisionTree).ToList();
            return dto;
        }

        public static RandomForestRegressor ImportRandomForest(RandomForestDto dto)
        {
            var model = new RandomForestRegressor(
                nEstimators: dto.NumTrees,
                maxDepth: dto.MaxDepth,
                minSamplesSplit: dto.MinSamplesSplit,
                minSamplesLeaf: dto.MinSamplesLeaf,
                bootstrap: true,
                sampleRatio: 1.0
            );

            var listField = typeof(RandomForestRegressor).GetField("_trees", Inst)!;
            var list = (IList<DecisionTreeRegressor>)listField.GetValue(model)!;
            list.Clear();
            foreach (var treeDto in dto.Trees)
                list.Add(ImportDecisionTree(treeDto));

            return model;
        }


        public static GradientBoostingDto ExportGradientBoosting(GradientBoostingRegressor model)
        {
            var dto = new GradientBoostingDto
            {
                LearningRate =
                    (double)typeof(GradientBoostingRegressor).GetField("_learningRate", Inst)!.GetValue(model)!,
                MaxDepth = (int)typeof(GradientBoostingRegressor).GetField("_maxDepth", Inst)!.GetValue(model)!,
                MinSamplesSplit =
                    (int)typeof(GradientBoostingRegressor).GetField("_minSamplesSplit", Inst)!.GetValue(model)!,
                MinSamplesLeaf =
                    (int)typeof(GradientBoostingRegressor).GetField("_minSamplesLeaf", Inst)!.GetValue(model)!,
                Subsample = (double)typeof(GradientBoostingRegressor).GetField("_subsample", Inst)!.GetValue(model)!,
                InitValue = (double)typeof(GradientBoostingRegressor).GetField("_init", Inst)!.GetValue(model)!
            };

            var biObj = typeof(GradientBoostingRegressor).GetProperty("BestIteration", Inst)?.GetValue(model);
            dto.BestIteration = biObj is int bi ? bi : -1;

            var trees = (IEnumerable<DecisionTreeRegressor>)typeof(GradientBoostingRegressor)
                .GetField("_trees", Inst)!.GetValue(model)!;

            dto.Trees = trees.Select(ExportDecisionTree).ToList();
            return dto;
        }

        public static GradientBoostingRegressor ImportGradientBoosting(GradientBoostingDto dto)
        {
            var model = new GradientBoostingRegressor(
                nEstimators: dto.Trees?.Count ?? 0,
                learningRate: dto.LearningRate,
                maxDepth: dto.MaxDepth,
                minSamplesSplit: dto.MinSamplesSplit,
                minSamplesLeaf: dto.MinSamplesLeaf,
                subsample: dto.Subsample
            );

            typeof(GradientBoostingRegressor).GetField("_init", Inst)!.SetValue(model, dto.InitValue);

            var listField = typeof(GradientBoostingRegressor).GetField("_trees", Inst)!;
            var list = (IList<DecisionTreeRegressor>)listField.GetValue(model)!;
            list.Clear();
            if (dto.Trees != null)
            {
                foreach (var treeDto in dto.Trees)
                    list.Add(ImportDecisionTree(treeDto));
            }

            return model;
        }


        private static TreeNodeDto ExportNode(object? nodeObj)
        {
            if (nodeObj == null) return new TreeNodeDto();

            var t = nodeObj.GetType();
            var left = t.GetField("Left", Inst)!.GetValue(nodeObj);
            var right = t.GetField("Right", Inst)!.GetValue(nodeObj);

            return new TreeNodeDto
            {
                FeatureIndex = (int)t.GetField("FeatureIndex", Inst)!.GetValue(nodeObj)!,
                Threshold = (double)t.GetField("Threshold", Inst)!.GetValue(nodeObj)!,
                Value = (double)t.GetField("Value", Inst)!.GetValue(nodeObj)!,
                Left = left != null ? ExportNode(left) : null,
                Right = right != null ? ExportNode(right) : null
            };
        }

        private static TreeNodeDto ExportDecisionTree(DecisionTreeRegressor tree)
        {
            var nodeField = typeof(DecisionTreeRegressor).GetField("_root", Inst)!;
            var node = nodeField.GetValue(tree);
            return ExportNode(node);
        }

        private static DecisionTreeRegressor ImportDecisionTree(TreeNodeDto dto)
        {
            var tree = new DecisionTreeRegressor();
            var nodeField = typeof(DecisionTreeRegressor).GetField("_root", Inst)!;
            var nodeType = typeof(DecisionTreeRegressor).GetNestedType("Node", Inst)!;

            object? Build(TreeNodeDto? d)
            {
                if (d == null) return null;
                var n = Activator.CreateInstance(nodeType, true)!;
                nodeType.GetField("FeatureIndex", Inst)!.SetValue(n, d.FeatureIndex);
                nodeType.GetField("Threshold", Inst)!.SetValue(n, d.Threshold);
                nodeType.GetField("Value", Inst)!.SetValue(n, d.Value);
                nodeType.GetField("Left", Inst)!.SetValue(n, Build(d.Left));
                nodeType.GetField("Right", Inst)!.SetValue(n, Build(d.Right));
                return n;
            }

            var root = Build(dto);
            nodeField.SetValue(tree, root);
            return tree;
        }
    }
}