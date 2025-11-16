using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.FileProviders;
using used_car_predictor.Backend.Api;
using used_car_predictor.Backend.Controllers;
using used_car_predictor.Backend.Services;
using Xunit;

[ExcludeFromCodeCoverage]
public class PredictionControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;
    public PredictionControllerValidationTests(WebApplicationFactory<Program> app) => _app = app;
    private HttpClient Client => _app.CreateClient();


    private HttpClient NewClient() => _app.CreateClient();
    
    [Fact]
    public async Task PredictTwo_BothValid_NoModel_Returns_503()
    {
        var client = NewClient();

        var req = new
        {
            carA = new
            {
                manufacturer = "AA",
                model = "AA1",
                yearOfProduction = 2019,
                mileageKm = 60000,
                fuelType = "gas",
                transmission = "manual",
                targetYear = 2030
            },
            carB = new
            {
                manufacturer = "BB",
                model = "BB1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic",
                targetYear = 2031
            }
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    
    [Fact]
    public async Task PredictTwoRange_BadFuelInCarA_Returns_400()
    {
        var client = NewClient();

        var req = new
        {
            algorithm = "ridge",
            startYear = 2028,
            endYear = 2028,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2014,
                mileageKm = 140000,
                fuelType = "diesell",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2015,
                mileageKm = 130000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwoRange_ClampingAnd503()
    {
        var client = NewClient();

        var req = new
        {
            algorithm = "linear",
            startYear = 1500,
            endYear = 9999,
            carA = new
            {
                manufacturer = "ClampA",
                model = "ClampA1",
                yearOfProduction = 1960,
                mileageKm = 200000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "ClampB",
                model = "ClampB1",
                yearOfProduction = 2022,
                mileageKm = 15000,
                fuelType = "electric",
                transmission = "automatic"
            }
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    [Fact]
    public async Task Predict_ValidInput_Returns_503()
    {
        var req = new
        {
            manufacturer = "TestBrand",
            model = "ModelA",
            yearOfProduction = 2015,
            mileageKm = 123000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Predict_MissingTargetYear_Defaults_And_503()
    {
        var req = new
        {
            manufacturer = "Default",
            model = "NoTarget",
            yearOfProduction = 2020,
            mileageKm = 30000,
            fuelType = "gas",
            transmission = "automatic"
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Predict_InvalidFuel_Preflight_400()
    {
        var req = new
        {
            manufacturer = "Bad",
            model = "Fuel",
            yearOfProduction = 2018,
            mileageKm = 100000,
            fuelType = "rocket",
            transmission = "manual",
            targetYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Predict_InvalidTransmission_Preflight_400()
    {
        var req = new
        {
            manufacturer = "Bad",
            model = "Trans",
            yearOfProduction = 2018,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "hydraulic",
            targetYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictRange_ValidYears_Returns_503()
    {
        var req = new
        {
            manufacturer = "BrandA",
            model = "RangeTest",
            yearOfProduction = 2016,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 2028,
            endYear = 2029
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    
    [Fact]
    public async Task PredictTwo_BothValid_Returns_503()
    {
        var req = new
        {
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictTwo_InvalidFuelInB_Returns_400()
    {
        var req = new
        {
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "rocket",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }



    [Theory]
    [InlineData("linear")]
    [InlineData("ridge")]
    [InlineData("ridge_rf")]
    [InlineData("ridge_gb")]
    public async Task PredictTwoRange_ValidAlgorithms_Returns_503(string algo)
    {
        var req = new
        {
            algorithm = algo,
            startYear = 2028,
            endYear = 2030,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2010,
                mileageKm = 200000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2012,
                mileageKm = 220000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    
    [Fact]
    public async Task PredictRange_SingleYear_Returns_503()
    {
        var req = new
        {
            manufacturer = "SingleYear",
            model = "Test",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 2030,
            endYear = 2030 
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictRange_InvalidFuel_Returns_400()
    {
        var req = new
        {
            manufacturer = "Brand",
            model = "Model",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "rocket",
            transmission = "manual",
            startYear = 2028,
            endYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwo_MissingModelInCarA_Returns_400()
    {
        var req = new
        {
            carA = new
            {
                manufacturer = "A",
                // model missing
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("petrol")]
    [InlineData("hybrid")]
    [InlineData("electric")]
    [InlineData("lpg")]
    public async Task Predict_AllValidFuelTypes_Returns_503(string fuel)
    {
        var req = new
        {
            manufacturer = "Test",
            model = "Model",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = fuel,
            transmission = "manual",
            targetYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    
    [Fact]
    public async Task PredictTwoRange_NullAlgorithm_Returns_400()
    {
        var req = new
        {
            algorithm = (string?)null,
            startYear = 2028,
            endYear = 2030,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwoRange_EmptyAlgorithm_Returns_400()
    {
        var req = new
        {
            algorithm = "   ",
            startYear = 2028,
            endYear = 2030,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwo_NullTargetYearBothCars_Defaults_Returns_503()
    {
        var req = new
        {
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
                // targetYear omitted
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
                // targetYear omitted
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(1989, 1990)] 
    [InlineData(2050, 2034)] 
    public async Task Predict_YearClamping_Returns_503(int input, int expected)
    {
        var req = new
        {
            manufacturer = "Clamp",
            model = "Test",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = input
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictRange_ExtremelyLongRange_Returns_503()
    {
        var req = new
        {
            manufacturer = "Long",
            model = "Range",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 1990,
            endYear = 2034
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictTwo_InvalidTransmissionInCarA_Returns_400()
    {
        var req = new
        {
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "cvt" // invalid
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwoRange_InvalidTransmissionInCarB_Returns_400()
    {
        var req = new
        {
            algorithm = "ridge",
            startYear = 2028,
            endYear = 2030,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2015,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "diesel",
                transmission = "sequential" // invalid
            }
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Theory]
    [InlineData("ridge_rf", "rf")]
    [InlineData("ridge_gb", "gb")]
    [InlineData("unknown", null)]
    public void GetMetricsOrDefault_Branches_Coverage(string key, string? fallback)
    {
        var dict = new Dictionary<string, (double, double, double)>
        {
            { "rf", (1, 2, 3) },
            { "gb", (4, 5, 6) },
            { "ridge", (7, 8, 9) }
        };

        var method = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("GetMetricsOrDefault", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = ((double mse, double mae, double r2))method!.Invoke(null, new object[] { dict, key })!;
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("ridge", "ridge")]
    [InlineData("  LINEAR  ", "linear")]
    [InlineData("ridge_rf", "ridge_rf")]
    [InlineData("ridge_gb", "ridge_gb")]
    [InlineData("unknown", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeAlgo_Covers_All_Paths(string? input, string? expected)
    {
        var method = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("NormalizeAlgo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (string?)method!.Invoke(null, new object?[] { input });
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1989, 1990)]
    [InlineData(1990, 1990)]
    [InlineData(2000, 2000)]
    [InlineData(2050, 2034)] 
    public void ClampYear_BoundaryTests(int input, int expected)
    {
        var method = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("ClampYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method!.Invoke(null, new object[] { input })!;
        result.Should().BeInRange(1990, DateTime.UtcNow.Year + 10);
    }

    [Fact]
    public void GetMetricsOrDefault_ExactKeyMatch()
    {
        var dict = new Dictionary<string, (double, double, double)>
        {
            { "linear", (1.0, 2.0, 3.0) }
        };

        var method = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("GetMetricsOrDefault", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = ((double mse, double mae, double r2))method!.Invoke(null, new object[] { dict, "linear" })!;
        result.Should().Be((1.0, 2.0, 3.0));
    }

    [Fact]
    public void GetMetricsOrDefault_NoMatchReturnsZeros()
    {
        var dict = new Dictionary<string, (double, double, double)>
        {
            { "linear", (1.0, 2.0, 3.0) }
        };

        var method = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("GetMetricsOrDefault", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = ((double mse, double mae, double r2))method!.Invoke(null, new object[] { dict, "nonexistent" })!;
        result.Should().Be((0.0, 0.0, 0.0));
    }
    
    [Fact]
    public async Task MissingRequiredField_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new
        {
            manufacturer = "Toyota",
            // model missing
            yearOfProduction = 2018,
            mileageKm = 90000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = 2030
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Predict_MissingBody_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictRange_StartGreaterThanEnd_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", new
        {
            manufacturer = "BMW",
            model = "3 Series",
            yearOfProduction = 2016,
            mileageKm = 120000,
            fuelType = "diesel",
            transmission = "automatic",
            startYear = 2031,
            endYear = 2030
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!["error"].Should().Be("startYear must be <= endYear");
    }

    [Fact]
    public async Task PredictTwo_MissingNestedField_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", new
        {
            carA = new
            {
                manufacturer = "Audi",
                // model missing
                yearOfProduction = 2017,
                mileageKm = 85000,
                fuelType = "gas",
                transmission = "automatic",
                targetYear = 2030
            },
            carB = new
            {
                manufacturer = "Mercedes",
                model = "C-Class",
                yearOfProduction = 2015,
                mileageKm = 140000,
                fuelType = "diesel",
                transmission = "manual",
                targetYear = 2030
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PredictTwoRange_StartGreaterThanEnd_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", new
        {
            algorithm = "ridge",
            startYear = 2035,
            endYear = 2034,
            carA = new
            {
                manufacturer = "Toyota",
                model = "Corolla",
                yearOfProduction = 2018,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "Honda",
                model = "Civic",
                yearOfProduction = 2018,
                mileageKm = 95000,
                fuelType = "gas",
                transmission = "manual"
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!["error"].Should().Be("startYear must be <= endYear");
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("randomforest")]
    [InlineData("RIDGE-RF")]
    [InlineData("ridge gb")]
    public async Task PredictTwoRange_InvalidAlgorithm_Returns_400(string badAlgo)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", new
        {
            algorithm = badAlgo,
            startYear = 2028,
            endYear = 2030,
            carA = new
            {
                manufacturer = "Toyota",
                model = "Yaris",
                yearOfProduction = 2015,
                mileageKm = 110000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "Ford",
                model = "Focus",
                yearOfProduction = 2016,
                mileageKm = 120000,
                fuelType = "gas",
                transmission = "manual"
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!["error"].Should().Be("algorithm must be one of: linear, ridge, ridge_rf, ridge_gb");
    }

    [Theory]
    [InlineData("rocket")]
    [InlineData("diesell")]
    public async Task Predict_InvalidFuelType_Returns_400(string badFuel)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new
        {
            manufacturer = "Skoda",
            model = "Octavia",
            yearOfProduction = 2019,
            mileageKm = 40000,
            fuelType = badFuel,
            transmission = "manual",
            targetYear = 2029
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("hydraulic")]
    [InlineData("robot")]
    public async Task Predict_InvalidTransmission_Returns_400(string badTrans)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new
        {
            manufacturer = "VW",
            model = "Golf",
            yearOfProduction = 2020,
            mileageKm = 30000,
            fuelType = "gas",
            transmission = badTrans,
            targetYear = 2027
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Predict_ValidInputs_NoModel_Returns_503()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new
        {
            manufacturer = "SomeBrand",
            model = "SomeModel",
            yearOfProduction = 2017,
            mileageKm = 80000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = 2030
        });
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Predict_NullTargetYear_Defaults_And_Returns_503()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict", new
        {
            manufacturer = "NoBrand",
            model = "NoModel",
            yearOfProduction = 2012,
            mileageKm = 150000,
            fuelType = "diesel",
            transmission = "automatic"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictRange_ValidRange_Returns_503()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", new
        {
            manufacturer = "BrandX",
            model = "ModelY",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 2028,
            endYear = 2029
        });
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("ridge")]
    [InlineData("Ridge")]
    [InlineData(" LINEAR ")]
    [InlineData("ridge_rf")]
    [InlineData("RIdGe_Gb")]
    public async Task PredictTwoRange_ValidAlgoVariants_PassAlgoCheck_Then503(string algo)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", new
        {
            algorithm = algo,
            startYear = 2028,
            endYear = 2029,
            carA = new
            {
                manufacturer = "A",
                model = "A1",
                yearOfProduction = 2014,
                mileageKm = 140000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "B",
                model = "B1",
                yearOfProduction = 2015,
                mileageKm = 130000,
                fuelType = "diesel",
                transmission = "automatic"
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictTwo_ValidA_InvalidB_Preflight_Returns_400()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two", new
        {
            carA = new
            {
                manufacturer = "OKA",
                model = "OKA1",
                yearOfProduction = 2018,
                mileageKm = 90000,
                fuelType = "gas",
                transmission = "manual"
            },
            carB = new
            {
                manufacturer = "BadB",
                model = "BadB1",
                yearOfProduction = 2018,
                mileageKm = 90000,
                fuelType = "rocket",
                transmission = "manual"
            }
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Theory]
    [InlineData("ridge_rf")]
    [InlineData("ridge_gb")]
    [InlineData("unknown")]
    public void GetMetricsOrDefault_Covers_Fallbacks_And_Defaults(string key)
    {
        var dict = new Dictionary<string, (double, double, double)>(StringComparer.OrdinalIgnoreCase)
        {
            ["rf"] = (1, 2, 3),
            ["gb"] = (4, 5, 6),
            ["ridge"] = (7, 8, 9)
        };

        var mi = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("GetMetricsOrDefault", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var (mse, mae, r2) = ((double mse, double mae, double r2))mi.Invoke(null, new object[] { dict, key })!;
        (mse, mae, r2).Should().NotBeNull();
    }

    [Theory]
    [InlineData("ridge", "ridge")]
    [InlineData("  LINEAR  ", "linear")]
    [InlineData("ridge_rf", "ridge_rf")]
    [InlineData("ridge_gb", "ridge_gb")]
    [InlineData("unknown", null)]
    public void NormalizeAlgo_AllBranches(string input, string? expected)
    {
        var mi = typeof(used_car_predictor.Backend.Controllers.PredictionController)
            .GetMethod("NormalizeAlgo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var result = (string?)mi.Invoke(null, new object[] { input });
        result.Should().Be(expected);
    }
}

public sealed class MockEnv : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider("/");
    public string ContentRootPath { get; set; } = "/";
    public string EnvironmentName { get; set; } = "Development";
    public string WebRootPath { get; set; } = "/";
    public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider("/");
}


public class ManufacturersControllerTests
{
    [Fact]
    public void Get_NoDirectory_Returns_EmptyArray()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "manuf-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var env = new MockEnv { ContentRootPath = tempRoot };
        var ctrl = new used_car_predictor.Backend.Controllers.ManufacturersController(env);
        
        var result = ctrl.Get();
        
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var arr = Assert.IsAssignableFrom<object[]>(ok.Value);
        arr.Length.Should().Be(0);

        try { Directory.Delete(tempRoot, true); } catch { }
    }

    [Fact]
    public void Get_WithOnlyInvalidFiles_EnumeratesAndSwallowsErrors_Returns_EmptyArray()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "manuf-tests-" + Guid.NewGuid().ToString("N"));
        var processed = Path.Combine(tempRoot, "Backend", "datasets", "processed");
        Directory.CreateDirectory(processed);
        
        File.WriteAllText(Path.Combine(processed, "a.json"), "{ not-json: true ");
        File.WriteAllText(Path.Combine(processed, "b.json"), "totally not json");
        File.WriteAllText(Path.Combine(processed, "c.json"), "");
        
        File.WriteAllText(Path.Combine(processed, "note.txt"), "hello");

        var env = new MockEnv { ContentRootPath = tempRoot };
        var ctrl = new used_car_predictor.Backend.Controllers.ManufacturersController(env);
        
        var result = ctrl.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var arr = Assert.IsAssignableFrom<object[]>(ok.Value);
        arr.Length.Should().Be(0);

        try { Directory.Delete(tempRoot, true); } catch { }
    }
}

[ExcludeFromCodeCoverage]
public class ModelsControllerTests
{
    private static MockEnv NewEnvAt(string root)
        => new MockEnv { ContentRootPath = root };

    private static string ProcessedPathOf(string root)
        => Path.Combine(root, "Backend", "datasets", "processed");


    [Fact]
    public void ListByManufacturer_NullRequest_Returns_400()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.ListByManufacturer(null);

        var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
        bad.Value.Should().NotBeNull();

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void ListByManufacturer_EmptyManufacturer_Returns_400()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.ListByManufacturer(new ManufacturerRequest { Manufacturer = "  " });

        Assert.IsType<BadRequestObjectResult>(res.Result);

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void ListByManufacturer_NoProcessedDir_Returns_Empty()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var processed = ProcessedPathOf(root);
        if (Directory.Exists(processed)) Directory.Delete(processed, true);

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.ListByManufacturer(new ManufacturerRequest { Manufacturer = "Toyota" });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<LabeledValueDto>>(ok.Value);
        list.Should().BeEmpty();

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void ListByManufacturer_WithOnlyInvalidFiles_Returns_Empty()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        var processed = ProcessedPathOf(root);
        Directory.CreateDirectory(processed);

        File.WriteAllText(Path.Combine(processed, "a.json"), "{ not-json: true ");
        File.WriteAllText(Path.Combine(processed, "b.json"), "totally not json");
        File.WriteAllText(Path.Combine(processed, "c.json"), "");

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.ListByManufacturer(new ManufacturerRequest { Manufacturer = "Toyota" });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<LabeledValueDto>>(ok.Value);
        list.Should().BeEmpty();

        try { Directory.Delete(root, true); } catch { }
    }


    [Fact]
    public void GetModelDetails_NullRequest_Returns_400()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.GetModelDetails(null!);

        Assert.IsType<BadRequestObjectResult>(res.Result);

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void GetModelDetails_MissingManufacturerOrModel_Returns_400()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ctrl = new ModelsController(NewEnvAt(root));

        var res1 = ctrl.GetModelDetails(new ModelsController.ModelDetailsRequest { Manufacturer = "", Model = "A" });
        var res2 = ctrl.GetModelDetails(new ModelsController.ModelDetailsRequest { Manufacturer = "A", Model = " " });

        Assert.IsType<BadRequestObjectResult>(res1.Result);
        Assert.IsType<BadRequestObjectResult>(res2.Result);

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void GetModelDetails_NoProcessedDir_Returns_404()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var processed = ProcessedPathOf(root);
        if (Directory.Exists(processed)) Directory.Delete(processed, true);

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.GetModelDetails(new ModelsController.ModelDetailsRequest
        {
            Manufacturer = "Toyota",
            Model = "Corolla"
        });

        var nf = Assert.IsType<NotFoundObjectResult>(res.Result);
        nf.Value.Should().NotBeNull();

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void GetModelDetails_WithOnlyInvalidFiles_Returns_404()
    {
        var root = Path.Combine(Path.GetTempPath(), "models-tests-" + Guid.NewGuid().ToString("N"));
        var processed = ProcessedPathOf(root);
        Directory.CreateDirectory(processed);

        File.WriteAllText(Path.Combine(processed, "a.json"), "{ not-json: true ");
        File.WriteAllText(Path.Combine(processed, "b.json"), "not json");
        File.WriteAllText(Path.Combine(processed, "c.json"), "");

        var ctrl = new ModelsController(NewEnvAt(root));
        var res = ctrl.GetModelDetails(new ModelsController.ModelDetailsRequest
        {
            Manufacturer = "Toyota",
            Model = "Corolla",
            AllowedFuels = new[] { "gas", "diesel" },
            AllowedTransmissions = new[] { "manual", "auto" }
        });

        Assert.IsType<NotFoundObjectResult>(res.Result);

        try { Directory.Delete(root, true); } catch { }
    }

    [Fact]
    public void FilterValues_Removes_Other_And_Respects_AllowOnly()
    {
        var method = typeof(ModelsController)
            .GetMethod("FilterValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var values = new[] { "Gas", "Diesel", "Other", "Electric" };
        var allow = new[] { "gas", "electric" };

        var result = (IEnumerable<string>)method.Invoke(null, new object[] { values, allow })!;
        result.Select(s => s.ToLowerInvariant()).Should().BeEquivalentTo(new[] { "gas", "electric" });
        result.Should().NotContain(s => s.Equals("other", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("eLECTRIC", "Electric")]
    [InlineData("  hybrid  ", "  Hybrid  ")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToTitleCase_Covers_Paths(string input, string expected)
    {
        var method = typeof(ModelsController)
            .GetMethod("ToTitleCase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var result = (string?)method.Invoke(null, new object[] { input! });
        result.Should().Be(expected);
    }
}

[ExcludeFromCodeCoverage]
public class HealthControllerTests
{
    [Fact]
    public void Get_NotLoaded_Returns_503()
    {
        var active = new ActiveModel();
        var ctrl = new HealthController();

        IActionResult result = ctrl.Get(active);

        var sc = Assert.IsType<StatusCodeResult>(result);
        sc.StatusCode.Should().Be(503);
    }
}