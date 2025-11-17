using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using used_car_predictor.Backend.Training;
using Xunit;

namespace used_car_predictor.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class CliTrainerTests
    {

        private sealed class FakeHostEnv : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; } = "used-car-predictor";
            public string ContentRootPath { get; set; } = default!;
            public IFileProvider ContentRootFileProvider { get; set; } = default!;
        }

        private static (string Root, string RawDir, string ProcessedDir, string CsvPath, IHostEnvironment Env)
            MakeSandbox(string testName)
        {
            var root = Path.Combine(Path.GetTempPath(), "cli-trainer-tests", Guid.NewGuid().ToString("N"), testName);
            var backend = Path.Combine(root, "Backend");
            var datasets = Path.Combine(backend, "datasets");
            var rawDir = Path.Combine(datasets, "raw");
            var processedDir = Path.Combine(datasets, "processed");
            Directory.CreateDirectory(rawDir);
            Directory.CreateDirectory(processedDir);

            var csvPath = Path.Combine(rawDir, "vehicles.csv");

            var env = new FakeHostEnv
            {
                ContentRootPath = root,
                ContentRootFileProvider = new PhysicalFileProvider(root)
            };

            return (root, rawDir, processedDir, csvPath, env);
        }

        
        private static void WriteVehiclesCsv(string path, string make, string model, int rows)
        {
            var header = "id,url,region,region_url,price,year,manufacturer,model,condition,cylinders,fuel,odometer,title_status,transmission,VIN,drive,size,type,paint_color,image_url,description,county,state,lat,long,posting_date";
            using var sw = new StreamWriter(path);
            sw.WriteLine(header);

            for (int i = 0; i < rows; i++)
            {
                var id = 7000000000 + i;
                var price = 10000 + i * 10;
                var year  = 2015 + (i % 5);
                var fuel  = "gas";
                var trans = "automatic";
                var odo   = 50000 + i * 100;

                sw.WriteLine(string.Join(",",
                    id.ToString(),
                    $"https://example.com/{id}",
                    "x",
                    "https://example.com",
                    price.ToString(),
                    year.ToString(),
                    make,
                    model,
                    "", 
                    "", 
                    fuel,
                    odo.ToString(),
                    "", 
                    trans,
                    "", 
                    "", 
                    "", 
                    "", 
                    "", 
                    "", 
                    "", 
                    "", 
                    "state",
                    "", 
                    "", 
                    DateTime.UtcNow.ToString("yyyy-MM-dd")
                ));
            }
        }

        [Fact]
        public void Run_TrainsBundle_ForSpecificMakeAndModel()
        {
            Environment.ExitCode = 0;

            var (root, _, processed, csv, env) = MakeSandbox(nameof(Run_TrainsBundle_ForSpecificMakeAndModel));
            WriteVehiclesCsv(csv, "Toyota", "Yaris", rows: 60);

            var args = new[]
            {
                "--csv", csv,
                "--manufacturer", "Toyota",
                "--model", "Yaris",
                "--anchor-year", "2030",
                "--max-configs", "2"
            };

            var exit = CliTrainer.Run(args, env);
            Assert.Equal(0, exit);

            var bundle = Directory.GetFiles(Path.Combine(root, "Backend", "datasets", "processed"), "*.json").Single();
            Assert.Contains("Toyota", File.ReadAllText(bundle));
        }

        [Fact]
        public void Run_Returns2_WhenModelGivenWithoutManufacturer()
        {
            Environment.ExitCode = 0; 

            var (_, _, _, csv, env) = MakeSandbox(nameof(Run_Returns2_WhenModelGivenWithoutManufacturer));
            WriteVehiclesCsv(csv, "Toyota", "Yaris", 55);

            var args = new[] { "--csv", csv, "--model", "Yaris" };
            var exit = CliTrainer.Run(args, env);
            Assert.Equal(2, exit);

            Environment.ExitCode = 0; 
        }

        [Fact]
        public void Run_Returns1_WhenNoTrainablePairs()
        {
            Environment.ExitCode = 0; 

            var (_, _, _, csv, env) = MakeSandbox(nameof(Run_Returns1_WhenNoTrainablePairs));
            WriteVehiclesCsv(csv, "Mazda", "2", rows: 10);

            var args = new[] { "--csv", csv, "--manufacturer", "Mazda", "--model", "2" };
            var exit = CliTrainer.Run(args, env);
            Assert.Equal(1, exit);
        }
    }
}