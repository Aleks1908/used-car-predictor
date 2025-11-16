using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using used_car_predictor.Backend.Data;
using Xunit;

[ExcludeFromCodeCoverage]
public class LabelScalerEdgeTests
{
    [Fact]
    public void ZeroValues_AreHandled_WhenUseLogTrue()
    {
        var scaler = new LabelScaler(); 
        var y = new[] { 0.0, 1000.0, 25000.0 };

        var yt = scaler.FitTransform(y);

        yt.Should().OnlyContain(v => double.IsFinite(v));

        var back = scaler.InverseTransform(yt);
        back.Length.Should().Be(y.Length);

        for (int i = 0; i < y.Length; i++)
        {
            back[i].Should().BeApproximately(y[i], 1e-6);
        }
    }

    [Fact]
    public void Transform_DoesNotProduceNaNOrInfinity_ForSmallOrZeroInputs()
    {
        var scaler = new LabelScaler(); 
        var y = new[] { 0.0, 1e-12, 1.0 };

        var yt = scaler.FitTransform(y);
        yt.Should().OnlyContain(v => double.IsFinite(v));

        var back = scaler.InverseTransform(yt);
        back.Should().OnlyContain(v => double.IsFinite(v));

        // Monotonicity sanity check after round-trip
        back[0].Should().BeLessThanOrEqualTo(back[1]);
        back[1].Should().BeLessThanOrEqualTo(back[2]);
    }
}