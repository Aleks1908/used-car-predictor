using System.Diagnostics.CodeAnalysis;
using Xunit;
using used_car_predictor.Backend.Data;

namespace used_car_predictor.Tests;

[ExcludeFromCodeCoverage]
public class FeatureScalerTests
{
    [Fact]
    public void FitTransform_ZeroMean_UnitVariance_OnTrain()
    {
        var X = new double[,]
        {
            { 1, 10 },
            { 2, 20 },
            { 3, 30 },
            { 4, 40 }
        };

        var scaler = new FeatureScaler();
        var Xs = scaler.FitTransform(X);

        for (int j = 0; j < 2; j++)
        {
            double mean = 0, var = 0;
            for (int i = 0; i < 4; i++) mean += Xs[i, j];
            mean /= 4.0;
            for (int i = 0; i < 4; i++) var += Math.Pow(Xs[i, j] - mean, 2);
            var /= 4.0;

            Assert.InRange(mean, -1e-10, 1e-10);
            Assert.InRange(var, 0.999, 1.001);
        }
    }

    [Fact]
    public void Transform_UsesTrainStats()
    {
        var train = new double[,]
        {
            { 0, 0 },
            { 10, 10 }
        };
        var test = new double[,]
        {
            { 5, 5 }
        };

        var scaler = new FeatureScaler();
        scaler.FitTransform(train);
        var Xt = scaler.Transform(test);

        Assert.InRange(Xt[0,0], -1e-7, 1e-7);
        Assert.InRange(Xt[0,1], -1e-7, 1e-7);
    }
}