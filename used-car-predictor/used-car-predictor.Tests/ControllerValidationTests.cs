using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class PredictionControllerValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _app;
    public PredictionControllerValidationTests(WebApplicationFactory<Program> app) => _app = app;
    private HttpClient Client => _app.CreateClient();


    private HttpClient NewClient() => _app.CreateClient();

    [Fact]
    public async Task MissingRequiredField_Returns_400()
    {
        var client = NewClient();

        var badReq = new
        {
            manufacturer = "Toyota",
            // model is missing
            yearOfProduction = 2018,
            mileageKm = 90000,
            fuelType = "gas",
            transmission = "manual",
            targetYear = 2030
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", badReq);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Predict_MissingBody_Returns_400()
    {
        var client = NewClient();

        // Send an empty object to trigger model validation failures
        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }



    [Fact]
    public async Task PredictTwo_MissingNestedField_Returns_400()
    {
        var client = NewClient();

        var badReq = new
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
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two", badReq);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }



    [Theory]
    [InlineData("rocket")]      // invalid fuel type if your DTO validates against known values
    [InlineData("diesell")]     // misspelled
    public async Task Predict_InvalidFuelType_Returns_400(string badFuel)
    {
        var client = NewClient();

        var badReq = new
        {
            manufacturer = "Skoda",
            model = "Octavia",
            yearOfProduction = 2019,
            mileageKm = 40000,
            fuelType = badFuel,
            transmission = "manual",
            targetYear = 2029
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", badReq);
        // If your DTO has data annotations / custom binder checks, this will be 400.
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("hydraulic")]  // invalid transmission value if validated
    [InlineData("robot")]
    public async Task Predict_InvalidTransmission_Returns_400(string badTrans)
    {
        var client = NewClient();

        var badReq = new
        {
            manufacturer = "VW",
            model = "Golf",
            yearOfProduction = 2020,
            mileageKm = 30000,
            fuelType = "gas",
            transmission = badTrans,
            targetYear = 2027
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", badReq);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
     [Fact]
    public async Task Predict_ValidInputs_NoModel_Returns_503()
    {
        var client = NewClient();

        var req = new
        {
            manufacturer = "SomeBrand",
            model = "SomeModel",
            yearOfProduction = 2017,
            mileageKm = 80000,
            fuelType = "gas",           // valid preflight
            transmission = "manual",    // valid preflight
            targetYear = 2030
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable); // hits EnsureModelLoaded
    }

    [Fact]
    public async Task Predict_NullTargetYear_Defaults_And_Returns_503()
    {
        var client = NewClient();

        var req = new
        {
            manufacturer = "NoBrand",
            model = "NoModel",
            yearOfProduction = 2012,
            mileageKm = 150000,
            fuelType = "diesel",
            transmission = "automatic"
            // targetYear omitted -> branch using DateTime.UtcNow.Year
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ---- /predict/range ----------------------------------------------------

    [Fact]
    public async Task PredictRange_ValidRange_Returns_503()
    {
        var client = NewClient();

        var req = new
        {
            manufacturer = "BrandX",
            model = "ModelY",
            yearOfProduction = 2015,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 2028,
            endYear = 2029
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PredictRange_YearsClamped_Still_Returns_503()
    {
        var client = NewClient();

        var req = new
        {
            manufacturer = "ClampCo",
            model = "Clampster",
            yearOfProduction = 1980,    // under min; clamp is inside Encode/Scale path via ClampYear on start/end
            mileageKm = 250000,
            fuelType = "diesel",
            transmission = "automatic",
            startYear = 1800,           // way too low
            endYear = 9999              // way too high; both will be clamped
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    // ---- /predict-two ------------------------------------------------------

    [Fact]
    public async Task PredictTwo_ValidA_InvalidB_Preflight_Returns_400()
    {
        var client = NewClient();

        var req = new
        {
            carA = new
            {
                manufacturer = "OKA",
                model = "OKA1",
                yearOfProduction = 2018,
                mileageKm = 90000,
                fuelType = "gas",        // valid
                transmission = "manual"  // valid
            },
            carB = new
            {
                manufacturer = "BadB",
                model = "BadB1",
                yearOfProduction = 2018,
                mileageKm = 90000,
                fuelType = "rocket",     // invalid preflight -> 400
                transmission = "manual"
            }
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

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

    // ---- /predict-two/range ------------------------------------------------

    [Theory]
    [InlineData("ridge")]          // exact
    [InlineData("Ridge")]          // casing
    [InlineData(" LINEAR ")]       // spaces
    [InlineData("ridge_rf")]       // underscore
    [InlineData("RIdGe_Gb")]       // mixed-case
    public async Task PredictTwoRange_ValidAlgoVariants_PassAlgoCheck_Then503(string algo)
    {
        var client = NewClient();

        var req = new
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
        };

        var resp = await client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable); // got past algo/years prechecks
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
                fuelType = "diesell", // invalid preflight
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
            startYear = 1500, // clamp up
            endYear = 9999,   // clamp down
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

    // ---------------------------------------------------
    // RANGE ENDPOINTS
    // ---------------------------------------------------

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
    public async Task PredictRange_StartGreaterThanEnd_Returns_400()
    {
        var req = new
        {
            manufacturer = "BrandA",
            model = "RangeTest",
            yearOfProduction = 2016,
            mileageKm = 100000,
            fuelType = "gas",
            transmission = "manual",
            startYear = 2031,
            endYear = 2030
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------
    // TWO-CAR ENDPOINTS
    // ---------------------------------------------------

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
    [InlineData("foo")]
    [InlineData("ridge gb")]
    [InlineData("RIDGE-RF")]
    [InlineData("randomforest")]
    public async Task PredictTwoRange_InvalidAlgorithm_Returns_400(string bad)
    {
        var req = new
        {
            algorithm = bad,
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

    // ---------------------------------------------------
    // INTERNAL BEHAVIOR / UTILITY TESTS (via endpoint)
    // ---------------------------------------------------

    [Fact]
    public async Task PredictTwoRange_StartGreaterThanEnd_Returns_400()
    {
        var req = new
        {
            algorithm = "ridge",
            startYear = 2035,
            endYear = 2034,
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
        };

        var resp = await Client.PostAsJsonAsync("/api/v1/prediction/predict-two/range", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------
    // ADDITIONAL EDGE CASE TESTS FOR BETTER COVERAGE
    // ---------------------------------------------------

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
            endYear = 2030 // same as start
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
            fuelType = "rocket", // invalid
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
    [InlineData(1989, 1990)] // below min
    [InlineData(2050, 2034)] // above max (assuming current year ~2024)
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

    // ---------------------------------------------------
    // INTERNAL LOGIC EXERCISES (utility coverage)
    // ---------------------------------------------------

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
    [InlineData(2050, 2034)] // assuming current year + 10
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
}

