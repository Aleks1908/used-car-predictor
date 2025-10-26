namespace used_car_predictor.Backend.Api;

public sealed class PredictRequest
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public string Transmission { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int MileageKm { get; set; }
}

public sealed class PredictRangeRequest
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string Transmission { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int MileageKm { get; set; }
    public int YearOfProduction { get; set; }
    public int FromYear { get; set; }
    public int ToYear { get; set; }
}

public sealed class ModelPredictionDto
{
    public string Algorithm { get; set; } = default!;
    public decimal PredictedPrice { get; set; }
    public ModelMetricsDto Metrics { get; set; } = new();
}

public sealed class ModelMetricsDto
{
    public double Mse { get; set; }
    public double Mae { get; set; }
    public double R2 { get; set; }
}

public sealed class PredictResponse
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public List<ModelPredictionDto> Results { get; set; } = new();

    public ModelInfoDto? ModelInfo { get; set; }
}

public sealed class ModelInfoDto
{
    public DateTimeOffset TrainedAt { get; set; }
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

public sealed class ManufacturerRequest
{
    public string Manufacturer { get; set; } = string.Empty;
}

public sealed class ModelDetailDto
{
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTimeOffset TrainedAt { get; set; }
    public string[] Fuels { get; set; } = Array.Empty<string>();
    public string[] Transmissions { get; set; } = Array.Empty<string>();
    public string[] Algorithms { get; set; } = Array.Empty<string>();
}

public sealed class PredictRangeItem
{
    public string Manufacturer { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public List<ModelPredictionDto> Results { get; set; } = new();
}

public sealed class PredictRangeResponse
{
    public List<PredictRangeItem> Items { get; set; } = new();
    public ModelInfoDto ModelInfo { get; set; } = new(); // only at the top-level
}