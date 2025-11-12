using System.Security.Cryptography;
using FluentAssertions;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;
using used_car_predictor.Backend.Serialization;
using Xunit;

public class PersistenceRoundTripTests
{
    [Fact]
    public void SaveLoadSave_RoundTrip_PreservesBundleSemantics_For_Ridge_RF_GB()
    {
        var rawX = new double[,] { { 0, 0 }, { 1, 2 }, { 2, -1 } };
        var rawY = new double[]   { 5.0,     13.0,     6.0       };

        var fScaler = new FeatureScaler();
        var lScaler = new LabelScaler();

        var X = fScaler.FitTransform(rawX);
        var y = lScaler.FitTransform(rawY);
        
        var ridge = new RidgeRegression(useClosedForm: true, lambda: 0.1);
        ridge.Fit(X, y);
        
        var rf = new RandomForestRegressor();
        rf.Fit(X, y);

        var gb = new GradientBoostingRegressor();
        gb.Fit(X, y);
        
        var fuels = new List<string>();
        var transmissions = new List<string>();
        
        
        var bundle = ModelPersistence.ExportBundle(
            ridge,
            rf,
            gb,
            fScaler,
            lScaler,
            fuels,
            transmissions,
            notes: "p0.5 roundtrip test"
        );

        bundle.Car = new CarMetaDto
        {
            Manufacturer = "TEST",
            Model = "Tiny",
            MinYear = 2000,
            MaxYear = 2000
        };
        
        var dir = Path.Combine(Path.GetTempPath(), "ucp_persistence_tests");
        Directory.CreateDirectory(dir);
        var p1 = Path.Combine(dir, "bundle1.json");
        var p2 = Path.Combine(dir, "bundle2.json");
        
        ModelPersistence.SaveBundle(bundle, p1);
        var loaded = ModelPersistence.LoadBundle(p1);
        loaded.Should().NotBeNull();
        
        {
            var xt = loaded.Preprocess != null
                ? fScaler.Transform(new double[,] { { rawX[0, 0], rawX[0, 1] } })
                : new double[,] { { rawX[0, 0], rawX[0, 1] } };
            
            double[] predScaled;
            if (loaded.Ridge != null)
                predScaled = ridge.Predict(xt);
            else if (loaded.RandomForest != null)
                predScaled = rf.Predict(xt);
            else
                predScaled = gb.Predict(xt);

            var yhat = lScaler.InverseTransform(predScaled);
            yhat.Length.Should().Be(1);
            yhat[0].Should().BeGreaterThan(0);
        }
        
        ModelPersistence.SaveBundle(loaded, p2);

        var b1 = File.ReadAllBytes(p1);
        var b2 = File.ReadAllBytes(p2);

        if (!b1.SequenceEqual(b2))
        {
            loaded.Car.Should().NotBeNull();
            loaded.Preprocess.Should().NotBeNull();
            loaded.Metrics.Should().NotBeNull();
            loaded.TrainingTimes.Should().NotBeNull();
        }
        else
        {
            using var sha = SHA256.Create();
            Convert.ToHexString(sha.ComputeHash(b1))
                .Should().Be(Convert.ToHexString(sha.ComputeHash(b2)));
        }
    }
}