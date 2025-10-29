using System.Text.Json.Serialization;
using used_car_predictor.Backend.Serialization;

namespace used_car_predictor.Backend.Api;

public sealed record PredictRequest
{
    public string Manufacturer { get; init; } = default!;
    public string Model { get; init; } = default!;
    public int YearOfProduction { get; init; }
    public string Transmission { get; init; } = default!;
    public string FuelType { get; init; } = default!;
    public int MileageKm { get; init; }
    public int? TargetYear { get; init; }
}

public sealed class PredictRangeRequest
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string Transmission { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int MileageKm { get; set; }
    public int YearOfProduction { get; set; }
    public int StartYear { get; set; }
    public int EndYear { get; set; }
}

public sealed class PredictResponse
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public List<ModelPredictionDto> Results { get; set; } = new();

    public ModelInfoDto? ModelInfo { get; set; }


    public Dictionary<string, AlgorithmMetricsDto>? Metrics { get; set; }
}

public sealed class ModelInfoDto
{
    public DateTimeOffset TrainedAt { get; set; }

    public int? AnchorTargetYear { get; set; }

    public int? TotalRows { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Mse { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Mae { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? R2 { get; set; }
}

public sealed class CatalogItemDto
{
    public string ModelId { get; set; } = default!;

    public string Manufacturer { get; set; } = "";

    public string DisplayModel { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public DateTimeOffset TrainedAt { get; set; }
    public string[] Algorithms { get; set; } = Array.Empty<string>();
}

public sealed class CatalogResponse
{
    public List<CatalogItemDto> Items { get; set; } = new();
}

public sealed class PredictRangeItem
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public List<ModelPredictionDto> Results { get; set; } = new();
}

public sealed class ManufacturerRequest
{
    public string Manufacturer { get; set; } = "";
}

public sealed class LabeledValueDto
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

public sealed class ModelFeatureMetaDto
{
    public LabeledValueDto[] Fuels { get; set; } = Array.Empty<LabeledValueDto>();
    public LabeledValueDto[] Transmissions { get; set; } = Array.Empty<LabeledValueDto>();

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public int? AnchorTargetYear { get; set; }
}

public sealed class TwoCarPredictRequest
{
    public PredictRequest CarA { get; set; } = default!;
    public PredictRequest CarB { get; set; } = default!;
}

public sealed class TwoCarPredictRangeRequest
{
    public PredictRequest CarA { get; set; } = default!;
    public PredictRequest CarB { get; set; } = default!;
    public int StartYear { get; set; }
    public int EndYear { get; set; }

    public string Algorithm { get; set; } = "ridge";
}

public sealed class YearlyPrediction
{
    public int Year { get; set; }
    public decimal PredictedPrice { get; set; }
}

public sealed class ModelPredictionDto
{
    public string Algorithm { get; set; } = default!;
    public decimal PredictedPrice { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelMetricsDto? Metrics { get; set; }
}

public sealed class ModelMetricsDto
{
    public double Mse { get; set; }
    public double Mae { get; set; }
    public double R2 { get; set; }
}

public sealed class AlgorithmMetricsDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelMetricsDto? Metrics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrainingTimeDto? Timing { get; set; }
}

public sealed class PredictRangeResponse
{
    public List<PredictRangeItem> Items { get; set; } = new();
    public ModelInfoDto ModelInfo { get; set; } = new();

    public Dictionary<string, AlgorithmMetricsDto>? Metrics { get; set; }
}

public sealed class TwoCarPredictResponse
{
    public PredictResponse CarA { get; set; } = default!;
    public PredictResponse CarB { get; set; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, AlgorithmMetricsDto>? MetricsA { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, AlgorithmMetricsDto>? MetricsB { get; set; }
}

public sealed class TwoCarPredictRangeResponse
{
    public string Algorithm { get; set; } = "";
    public List<YearlyPrediction> CarA { get; set; } = new();
    public List<YearlyPrediction> CarB { get; set; } = new();
    public ModelInfoDto ModelInfoA { get; set; } = new();
    public ModelInfoDto ModelInfoB { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, AlgorithmMetricsDto>? MetricsA { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, AlgorithmMetricsDto>? MetricsB { get; set; }
}