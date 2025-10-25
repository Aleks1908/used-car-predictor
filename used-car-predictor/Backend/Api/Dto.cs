namespace used_car_predictor.Backend.Api;

public sealed class PredictRequest
{
    public string Make { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; }
    public int TargetYear { get; set; }
    public string Transmission { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int MileageKm { get; set; }
}

public sealed class PredictRangeRequest
{
    public string Make { get; set; } = default!;
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
    public string Algorithm { get; set; } = default!; // "linear" | "ridge" | "rf" | "gb"
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
    public string Make { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int YearOfProduction { get; set; } // ← include both years in the response
    public int TargetYear { get; set; } // ← include both years in the response
    public string Currency { get; set; } = "EUR";
    public List<ModelPredictionDto> Results { get; set; } = new();
    public ModelInfoDto ModelInfo { get; set; } = new();
}

public sealed class PredictRangeResponse
{
    public string Currency { get; set; } = "EUR";
    public List<PredictResponse> Items { get; set; } = new();
    public ModelInfoDto ModelInfo { get; set; } = new();
}

public sealed class ModelInfoDto
{
    public string Version { get; set; } = "unloaded";
    public DateTimeOffset TrainedAt { get; set; }
}

public sealed class CatalogItemDto
{
    public string ModelId { get; set; } = default!;

    public string Make { get; set; } = "";

    public string DisplayModel { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTimeOffset TrainedAt { get; set; }
    public string[] Algorithms { get; set; } = Array.Empty<string>();
}

public sealed class CatalogResponse
{
    public List<CatalogItemDto> Items { get; set; } = new();
}