using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace used_car_predictor.Backend.Data;

public class CsvLoader
{
    public static List<Vehicle> LoadVehicles(string filePath, int? maxRows = null)
    {
        using var reader = new StreamReader(filePath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        var records = new List<Vehicle>();

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            try
            {
                var vehicle = new Vehicle
                {
                    Year = csv.GetField<int?>("year"),
                    Price = csv.GetField<double?>("price"),
                    Odometer = csv.GetField<double?>("odometer"),
                    Manufacturer = csv.GetField<string>("manufacturer"),
                    Model = csv.GetField<string>("model"),
                    Fuel = csv.GetField<string>("fuel"),
                    Transmission = csv.GetField<string>("transmission")
                };

                if (vehicle.Price is > 0 && vehicle.Year is not null && vehicle.Odometer is not null)
                {
                    records.Add(vehicle);
                }

                Console.WriteLine($"Row: year={vehicle.Year}, price={vehicle.Price}, odo={vehicle.Odometer}");

                if (maxRows != null && records.Count >= maxRows.Value)
                    break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Skipped row: {ex.Message}");
            }
        }

        return records;
    }

}