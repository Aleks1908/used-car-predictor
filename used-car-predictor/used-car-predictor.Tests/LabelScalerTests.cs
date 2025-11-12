using Xunit;
using used_car_predictor.Backend.Data;

namespace used_car_predictor.Tests;

public class LabelScalerTests
{
    [Fact]
    public void RoundTrip_WithLog_WorksForPositiveLabels()
    {
        var y = new[] { 1000.0, 5000.0, 20000.0, 90000.0 };
        var scaler = new LabelScaler();

        var yScaled = scaler.FitTransform(y);
        var yBack = scaler.InverseTransform(yScaled);

        for (int i = 0; i < y.Length; i++)
            Assert.Equal(y[i], yBack[i], precision: 9);
    }

    [Fact]
    public void RoundTrip_WithoutLog_IsIdentity()
    {
        var y = new[] { 1.0, 2.5, -3.0, 0.0, 10.0 };
        var scaler = new LabelScaler();

        var yScaled = scaler.FitTransform(y);
        var yBack = scaler.InverseTransform(yScaled);

        for (int i = 0; i < y.Length; i++)
            Assert.Equal(y[i], yBack[i], precision: 12);
    }
}