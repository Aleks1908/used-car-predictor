using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Controllers;
using used_car_predictor.Backend.Services;
using Xunit;

[ExcludeFromCodeCoverage]
public sealed class BundleIdTests
{
    [Theory]
    [InlineData("toyota", "toyota_4runner", "toyota_4runner")]
    [InlineData("toyota", "4runner", "toyota_4runner")]
    [InlineData("toyota", "4runner sr5", "toyota_4runner_sr5")]
    [InlineData("toyota", "4runner-limited-sport", "toyota_4runner_limited_sport")]
    [InlineData("toyota", "utility sport sr5 toyota_4runner", "toyota_4runner_utility_sport_sr5")]
    [InlineData("toyota", "toyota_4runner_utility_sport_sr5  ", "toyota_4runner_utility_sport_sr5")]
    public void From_Canonicalizes_And_Rotates_Make(string make, string model, string expected)
    {
        var id = BundleId.From(make, model);
        id.Should().Be(expected);
    }
}

[ExcludeFromCodeCoverage]
public sealed class BundleLabelTests
{
    [Theory]
    [InlineData("toyota_camry_xle", "Toyota Camry Xle")]
    [InlineData("toyota_4runner_utility_sport_sr5", "Toyota 4Runner Utility Sport Sr5")]
    [InlineData("  toyota-prius  ", "Toyota Prius")]
    [InlineData("", "")]
    public void From_Returns_TitleCased_Label(string input, string expected)
    {
        var label = BundleId.BundleLabel.From(input);
        label.Should().Be(expected);
    }
}

[ExcludeFromCodeCoverage]
public sealed class StaticBundleResolverTests
{
    private sealed class MockEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider("/");
        public string ContentRootPath { get; set; } = "/";
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = "/";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider("/");
    }

    private static IConfiguration Cfg() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Model:Algorithm"] = "ridge"
        }).Build();

    [Fact]
    public void Resolve_ExactCanonicalFile_ReturnsIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "resolver-tests-" + Guid.NewGuid().ToString("N"));
        var processed = Path.Combine(root, "Backend", "datasets", "processed");
        Directory.CreateDirectory(processed);

        var canonicalId = "toyota_4runner_utility_sport_sr5";
        var canonicalPath = Path.Combine(processed, canonicalId + ".json");
        File.WriteAllText(canonicalPath, "{}");

        var env = new MockEnv { ContentRootPath = root };
        var resolver = new StaticBundleResolver(env, Cfg());

        var (path, algo) = resolver.Resolve("toyota", "toyota_4runner_utility_sport_sr5");
        path.Should().Be(canonicalPath);
        algo.Should().Be("ridge");

        try { Directory.Delete(root, true); } catch { }
    }
}

[ExcludeFromCodeCoverage]
public sealed class ModelsControllerCanonicalizationTests
{
    private sealed class MockEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider("/");
        public string ContentRootPath { get; set; } = "/";
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = "/";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider("/");
    }

    private static string Processed(string root)
        => Path.Combine(root, "Backend", "datasets", "processed");

    private static void WriteBundle(string path,
        string manufacturer,
        string model,
        string[]? fuels = null,
        string[]? trans = null,
        int? minYear = 2000,
        int? maxYear = 2030,
        int? anchor = 2030)
    {
        var obj = new
        {
            Car = new
            {
                Manufacturer = manufacturer,
                Model = model,
                MinYear = minYear,
                MaxYear = maxYear
            },
            Preprocess = new
            {
                Fuels = fuels ?? new[] { "gas", "hybrid" },
                Transmissions = trans ?? new[] { "automatic", "manual" },
                MinYear = minYear,
                MaxYear = maxYear,
                AnchorTargetYear = anchor
            }
        };

        File.WriteAllText(path, JsonSerializer.Serialize(obj));
    }

    [Fact]
    public void ListByManufacturer_Returns_CanonicalValues_NoTrailingSpaces()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-list-" + Guid.NewGuid().ToString("N"));
        var dir = Processed(root);
        Directory.CreateDirectory(dir);

        WriteBundle(Path.Combine(dir, "toyota_4runner_limited_sport.json"),
            "toyota", "4Runner Limited Sport");
        WriteBundle(Path.Combine(dir, "toyota_crewmax_tundra.json"),
            "toyota", "Crewmax Tundra");

        var ctrl = new ModelsController(new MockEnv { ContentRootPath = root });
        var res = ctrl.ListByManufacturer(new ManufacturerRequest { Manufacturer = "toyota" });
        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<LabeledValueDto>>(ok.Value);

        list.Should().Contain(x => x.Value == "toyota_4runner_limited_sport");
        list.Should().Contain(x => x.Value == "toyota_crewmax_tundra");
        list.Select(x => x.Value).Should().OnlyContain(v => !v.EndsWith(" "));
        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void Details_Matches_OrderInsensitive_ModelId()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-details-" + Guid.NewGuid().ToString("N"));
        var dir = Processed(root);
        Directory.CreateDirectory(dir);

        var filename = "toyota_4runner_sport_sr5_utility.json";
        WriteBundle(Path.Combine(dir, filename),
            "toyota", "4Runner Sr5 Sport Utility",
            fuels: new[] { "gas", "hybrid" },
            trans: new[] { "automatic", "manual" },
            minYear: 2010, maxYear: 2024, anchor: 2030);

        var ctrl = new ModelsController(new MockEnv { ContentRootPath = root });
        var req = new ModelsController.ModelDetailsRequest
        {
            Manufacturer = "toyota",
            Model = "toyota_4runner_utility_sport_sr5"
        };
        var res = ctrl.GetModelDetails(req);

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<ModelFeatureMetaDto>(ok.Value);
        dto.Fuels.Should().Contain(f => f.Value == "gas");
        dto.Transmissions.Should().Contain(t => t.Value == "automatic");

        try { Directory.Delete(root, true); } catch { }
    }
}