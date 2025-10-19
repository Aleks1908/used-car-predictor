// Backend/Serialization/Dto.cs

using System;
using System.Collections.Generic;

namespace used_car_predictor.Backend.Serialization
{
    public class RidgeDto
    {
        public double[] Weights { get; set; } = Array.Empty<double>();
        public double Bias { get; set; }
    }

    public class LinearDto
    {
        public double[] Weights { get; set; } = Array.Empty<double>();
        public double Bias { get; set; }
    }

    public class MetricsDto
    {
        public double MAE { get; set; }
        public double RMSE { get; set; }
        public double R2 { get; set; }
    }

    public class TreeNodeDto
    {
        public int FeatureIndex { get; set; }
        public double Threshold { get; set; }
        public double Value { get; set; }
        public TreeNodeDto? Left { get; set; }
        public TreeNodeDto? Right { get; set; }
    }

    public class RandomForestDto
    {
        public int NEstimators { get; set; }
        public int MaxDepth { get; set; }
        public int MinSamplesSplit { get; set; }
        public int MinSamplesLeaf { get; set; }
        public bool Bootstrap { get; set; }
        public double SampleRatio { get; set; }
        public int RandomSeed { get; set; }
        public List<TreeNodeDto> Trees { get; set; } = new List<TreeNodeDto>();
    }

    public class GradientBoostingDto
    {
        public int NEstimators { get; set; }
        public double LearningRate { get; set; }
        public int MaxDepth { get; set; }
        public int MinSamplesSplit { get; set; }
        public int MinSamplesLeaf { get; set; }
        public double Subsample { get; set; }
        public int RandomSeed { get; set; }
        public double Init { get; set; }
        public List<TreeNodeDto> Trees { get; set; } = new List<TreeNodeDto>();
    }

    public class PreprocessDto
    {
        public List<string> Fuels { get; set; } = new List<string>();
        public List<string> Transmissions { get; set; } = new List<string>();
        public double[] FeatureMeans { get; set; } = Array.Empty<double>();
        public double[] FeatureStds { get; set; } = Array.Empty<double>();
        public double LabelMean { get; set; }
        public double LabelStd { get; set; }
        public bool LabelUseLog { get; set; }
    }

    public class BundleDto
    {
        public string Version { get; set; } = "v1";
        public DateTime TrainedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public CarMetaDto? Car { get; set; }

        public PreprocessDto Preprocess { get; set; } = new PreprocessDto();

        public LinearDto Linear { get; set; } = new LinearDto();
        public RidgeDto Ridge { get; set; } = new RidgeDto();
        public RandomForestDto RandomForest { get; set; } = new RandomForestDto();
        public GradientBoostingDto GradientBoosting { get; set; } = new GradientBoostingDto();

        public Dictionary<string, MetricsDto> Metrics { get; set; } = new Dictionary<string, MetricsDto>();
    }

    public sealed class CarMetaDto
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
    }
}