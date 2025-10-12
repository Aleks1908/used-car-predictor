using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Backend.Serialization
{
    public static class ModelPersistence
    {
        private static readonly BindingFlags
            Inst = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // ---------------- JSON helpers ----------------
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
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
            return JsonSerializer.Deserialize<BundleDto>(json, JsonOptions)
                   ?? throw new InvalidOperationException($"Could not parse bundle at {path}");
        }

        // --------------- Export (objects -> DTOs) ----------------
        public static BundleDto ExportBundle(
            RidgeRegression ridge,
            RandomForestRegressor rf,
            GradientBoostingRegressor gb,
            FeatureScaler fScaler,
            LabelScaler yScaler,
            List<string> fuels,
            List<string> transmissions,
            string? notes = null)
        {
            return new BundleDto
            {
                TrainedAtUtc = DateTime.UtcNow,
                Notes = notes,
                Preprocess = ExportPreprocess(fScaler, yScaler, fuels, transmissions),
                Ridge = ExportRidge(ridge),
                RandomForest = ExportRandomForest(rf),
                GradientBoosting = ExportGradientBoosting(gb)
            };
        }

        public static PreprocessDto ExportPreprocess(FeatureScaler fScaler, LabelScaler yScaler, List<string> fuels,
            List<string> transmissions)
        {
            var (means, stds) = GetFeatureScalerState(fScaler);
            var (yMean, yStd, yUseLog) = GetLabelScalerState(yScaler);

            return new PreprocessDto
            {
                Fuels = fuels.ToList(),
                Transmissions = transmissions.ToList(),
                FeatureMeans = means,
                FeatureStds = stds,
                LabelMean = yMean,
                LabelStd = yStd,
                LabelUseLog = yUseLog
            };
        }

        public static RidgeDto ExportRidge(RidgeRegression model)
        {
            var weights = (double[])typeof(RidgeRegression).GetField("_weights", Inst)!.GetValue(model)!;
            var bias = (double)typeof(RidgeRegression).GetField("_bias", Inst)!.GetValue(model)!;
            return new RidgeDto { Weights = weights, Bias = bias };
        }

        public static RandomForestDto ExportRandomForest(RandomForestRegressor model)
        {
            var dto = new RandomForestDto();
            dto.NEstimators = (int)typeof(RandomForestRegressor).GetField("_nEstimators", Inst)!.GetValue(model)!;
            dto.MaxDepth = (int)typeof(RandomForestRegressor).GetField("_maxDepth", Inst)!.GetValue(model)!;
            dto.MinSamplesSplit =
                (int)typeof(RandomForestRegressor).GetField("_minSamplesSplit", Inst)!.GetValue(model)!;
            dto.MinSamplesLeaf = (int)typeof(RandomForestRegressor).GetField("_minSamplesLeaf", Inst)!.GetValue(model)!;
            dto.Bootstrap = (bool)typeof(RandomForestRegressor).GetField("_bootstrap", Inst)!.GetValue(model)!;
            dto.SampleRatio = (double)typeof(RandomForestRegressor).GetField("_sampleRatio", Inst)!.GetValue(model)!;
            dto.RandomSeed = (int)typeof(RandomForestRegressor).GetField("_randomSeed", Inst)!.GetValue(model)!;

            var trees = (IEnumerable<DecisionTreeRegressor>)typeof(RandomForestRegressor)
                .GetField("_trees", Inst)!.GetValue(model)!;

            dto.Trees = trees.Select(ExportDecisionTree).ToList();
            return dto;
        }

        public static GradientBoostingDto ExportGradientBoosting(GradientBoostingRegressor model)
        {
            var dto = new GradientBoostingDto();

            // Required fields in your class
            dto.NEstimators = (int)typeof(GradientBoostingRegressor).GetField("_nEstimators", Inst)!.GetValue(model)!;
            dto.LearningRate =
                (double)typeof(GradientBoostingRegressor).GetField("_learningRate", Inst)!.GetValue(model)!;
            dto.MaxDepth = (int)typeof(GradientBoostingRegressor).GetField("_maxDepth", Inst)!.GetValue(model)!;
            dto.MinSamplesSplit =
                (int)typeof(GradientBoostingRegressor).GetField("_minSamplesSplit", Inst)!.GetValue(model)!;
            dto.MinSamplesLeaf =
                (int)typeof(GradientBoostingRegressor).GetField("_minSamplesLeaf", Inst)!.GetValue(model)!;
            dto.Subsample = (double)typeof(GradientBoostingRegressor).GetField("_subsample", Inst)!.GetValue(model)!;
            dto.Init = (double)typeof(GradientBoostingRegressor).GetField("_init", Inst)!.GetValue(model)!;

            // _randomSeed might not exist in your implementation; default to 42 if missing.
            var fSeed = typeof(GradientBoostingRegressor).GetField("_randomSeed", Inst);
            dto.RandomSeed = fSeed != null ? (int)fSeed.GetValue(model)! : 42;

            // Trees
            var treesField = typeof(GradientBoostingRegressor).GetField("_trees", Inst)!;
            var treesObj = treesField.GetValue(model);
            var trees = treesObj as IEnumerable<DecisionTreeRegressor> ?? Array.Empty<DecisionTreeRegressor>();
            dto.Trees = trees.Select(ExportDecisionTree).ToList();

            return dto;
        }

        private static TreeNodeDto ExportDecisionTree(DecisionTreeRegressor tree)
        {
            var nodeField = typeof(DecisionTreeRegressor).GetField("_root", Inst)!;
            var node = nodeField.GetValue(tree);
            return ExportNode(node);
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

        // --------------- Import (DTOs -> objects) ----------------
        public static RidgeRegression ImportRidge(RidgeDto dto)
        {
            var model = new RidgeRegression();
            typeof(RidgeRegression).GetField("_weights", Inst)!.SetValue(model, dto.Weights);
            typeof(RidgeRegression).GetField("_bias", Inst)!.SetValue(model, dto.Bias);
            return model;
        }

        public static RandomForestRegressor ImportRandomForest(RandomForestDto dto)
        {
            var model = new RandomForestRegressor(
                nEstimators: dto.NEstimators,
                maxDepth: dto.MaxDepth,
                minSamplesSplit: dto.MinSamplesSplit,
                minSamplesLeaf: dto.MinSamplesLeaf,
                bootstrap: dto.Bootstrap,
                sampleRatio: dto.SampleRatio,
                randomSeed: dto.RandomSeed
            );

            var listField = typeof(RandomForestRegressor).GetField("_trees", Inst)!;
            var list = (IList<DecisionTreeRegressor>)listField.GetValue(model)!;
            list.Clear();
            foreach (var treeDto in dto.Trees)
            {
                list.Add(ImportDecisionTree(treeDto));
            }

            return model;
        }

        public static GradientBoostingRegressor ImportGradientBoosting(GradientBoostingDto dto)
        {
            var model = new GradientBoostingRegressor(
                nEstimators: dto.NEstimators,
                learningRate: dto.LearningRate,
                maxDepth: dto.MaxDepth,
                minSamplesSplit: dto.MinSamplesSplit,
                minSamplesLeaf: dto.MinSamplesLeaf,
                subsample: dto.Subsample,
                randomSeed: dto.RandomSeed
            );

            typeof(GradientBoostingRegressor).GetField("_init", Inst)!.SetValue(model, dto.Init);

            var listField = typeof(GradientBoostingRegressor).GetField("_trees", Inst)!;
            var list = (IList<DecisionTreeRegressor>)listField.GetValue(model)!;
            list.Clear();
            foreach (var treeDto in dto.Trees)
            {
                list.Add(ImportDecisionTree(treeDto));
            }

            return model;
        }

        private static DecisionTreeRegressor ImportDecisionTree(TreeNodeDto dto)
        {
            var tree = new DecisionTreeRegressor(); // depth etc. irrelevant at predict time
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

        public static FeatureScaler ImportFeatureScaler(double[] means, double[] stds)
        {
            var scaler = new FeatureScaler();
            var meansField = typeof(FeatureScaler).GetField("means", Inst)!;
            var stdsField = typeof(FeatureScaler).GetField("stds", Inst)!;
            meansField.SetValue(scaler, means);
            stdsField.SetValue(scaler, stds);
            return scaler;
        }

        public static LabelScaler ImportLabelScaler(double mean, double std, bool useLog)
        {
            var scaler = (LabelScaler)Activator.CreateInstance(typeof(LabelScaler), new object[] { useLog })!;
            typeof(LabelScaler).GetField("_mean", Inst)!.SetValue(scaler, mean);
            typeof(LabelScaler).GetField("_std", Inst)!.SetValue(scaler, std);
            return scaler;
        }

        private static (double[] Means, double[] Stds) GetFeatureScalerState(FeatureScaler fScaler)
        {
            var means = (double[])typeof(FeatureScaler).GetField("means", Inst)!.GetValue(fScaler)!;
            var stds = (double[])typeof(FeatureScaler).GetField("stds", Inst)!.GetValue(fScaler)!;
            return (means, stds);
        }

        private static (double Mean, double Std, bool UseLog) GetLabelScalerState(LabelScaler yScaler)
        {
            var mean = (double)typeof(LabelScaler).GetField("_mean", Inst)!.GetValue(yScaler)!;
            var std = (double)typeof(LabelScaler).GetField("_std", Inst)!.GetValue(yScaler)!;
            var ctor = typeof(LabelScaler).GetConstructor(new[] { typeof(bool) })!;
            // We don't store 'UseLog' anywhere internal, get it by checking ctor arg? There's a readonly field.
            // We can infer from behavior by checking whether InverseTransform(Math.E - 1) equals 1 +/- epsilon,
            // but it's simpler to read private field if present.
            var fUseLog = typeof(LabelScaler).GetField("_useLog", Inst);
            bool useLog = fUseLog != null && (bool)fUseLog.GetValue(yScaler)!;
            return (mean, std, useLog);
        }

        // Export the trained LinearRegression model to DTO
        public static LinearDto ExportLinear(LinearRegression model)
        {
            var t = typeof(LinearRegression);

            // Try common field names to be robust
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
            if (biasField != null)
                bias = (double)biasField.GetValue(model)!;

            return new LinearDto
            {
                Weights = weights,
                Bias = bias
            };
        }

// Rehydrate a LinearRegression model from the DTO
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
    }
}