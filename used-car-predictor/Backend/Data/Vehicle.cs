namespace used_car_predictor.Backend.Data;

public class Vehicle
{
    public int? Year { get; init; }
    public double? Price { get; init; }
    public double? Odometer { get; init; }
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? Fuel { get; init; }
    public string? Transmission { get; init; }
}