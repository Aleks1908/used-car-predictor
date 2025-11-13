using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;
    public ControllerValidationTests(WebApplicationFactory<Program> app) => _app = app;

    [Fact]
    public async Task MissingRequiredField_Returns_400()
    {
        var client = _app.CreateClient();

        var badReq = new
        {
            manufacturer = "Toyota",
            // model = null,
            yearOfProduction = 2018,
            mileageKm = 90000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = 2030
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", badReq);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}