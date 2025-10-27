using System;
using System.Collections.Generic;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Serialization
{
    public class RidgeDto
    {
        public double[] Weights { get; set; } = Array.Empty<double>();
        public double Bias { get; set; }
        public double Alpha { get; set; } = 1.0;
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

    public sealed class RandomForestDto
    {
        public List<TreeNodeDto> Trees { get; set; } = new();
        public int NumTrees { get; set; } = 100;
        public int MaxDepth { get; set; } = 10;
        public int MinSamplesSplit { get; set; } = 2;
        public int MinSamplesLeaf { get; set; } = 1;
    }
}

public sealed class GradientBoostingDto
{
    public List<TreeNodeDto> Trees { get; set; } = new();

    public double LearningRate { get; set; } = 0.1;
    public int MaxDepth { get; set; } = 3;
    public int MinSamplesSplit { get; set; } = 2;
    public int MinSamplesLeaf { get; set; } = 1;
    public double Subsample { get; set; } = 1.0;
    public double InitValue { get; set; } = 0.0;

    public int BestIteration { get; set; } = -1;
}

public sealed class CarMetaDto
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
}

public sealed class PreprocessDto
{
    public List<string> Fuels { get; set; } = new();
    public List<string> Transmissions { get; set; } = new();

    public double[] FeatureMeans { get; set; } = Array.Empty<double>();
    public double[] FeatureStds { get; set; } = Array.Empty<double>();

    public double LabelMean { get; set; }
    public double LabelStd { get; set; }
    public bool LabelUseLog { get; set; }

    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }

    public int? AnchorTargetYear { get; set; }
}

public class BundleDto
{
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