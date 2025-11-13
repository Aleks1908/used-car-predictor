
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ControllerIntegrationSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;

    public ControllerIntegrationSmokeTests(WebApplicationFactory<Program> app) => _app = app;

    [Fact]
    public async Task SinglePrediction_Returns_200_And_BasicShape()
    {
        var client = _app.CreateClient();
        var req = new
        {
            manufacturer    = "Toyota",
            model           = "Yaris",
            yearOfProduction= 2018,
            mileageKm       = 90_000,
            fuelType        = "gas",
            transmission    = "manual",
            targetYear      = 2030
        };

        // Act
        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.IsSuccessStatusCode.Should().BeTrue();

        var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        doc.Should().NotBeNull();
        var root = doc!.RootElement;

        root.TryGetProperty("targetYear", out var targetYear).Should().BeTrue();
        targetYear.GetInt32().Should().BeGreaterThan(0);

        root.TryGetProperty("results", out var results).Should().BeTrue();
        results.ValueKind.Should().Be(JsonValueKind.Array);
        results.GetArrayLength().Should().BeGreaterThan(0);

        var first = results[0];
        first.TryGetProperty("algorithm", out var algo).Should().BeTrue();
        algo.GetString().Should().NotBeNullOrWhiteSpace();

        first.TryGetProperty("predictedPrice", out var price).Should().BeTrue();
        price.ValueKind.Should().Be(JsonValueKind.Number);
        price.GetDecimal().Should().BeGreaterThan(0);

        // Optional quick checks (existence only)
        root.TryGetProperty("modelInfo", out _).Should().BeTrue();
        root.TryGetProperty("metrics", out _).Should().BeTrue();
    }
}