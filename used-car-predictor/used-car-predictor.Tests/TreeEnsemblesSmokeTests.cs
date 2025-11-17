using System.Diagnostics.CodeAnalysis;
using Xunit;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Tests;

[ExcludeFromCodeCoverage]
public class TreeEnsemblesSmokeTests
{
    [Fact]
    public void RandomForest_Smoke_FitsAndPredictsFinite()
    {
        var X = new double[,]
        {
            {0}, {1}, {2}, {3}, {4}, {5}
        };
        var y = new double[] {0, 2, 4, 6, 8, 10};

        var rf = new RandomForestRegressor(
            maxDepth: 4,
            minSamplesSplit: 2);

        rf.Fit(X, y);
        var preds = rf.Predict(X);

        Assert.Equal(y.Length, preds.Length);
        foreach (var v in preds) Assert.False(double.IsNaN(v) || double.IsInfinity(v));
    }

    [Fact]
    public void GradientBoosting_Smoke_FitsAndPredictsFinite()
    {
        var X = new double[,]
        {
            {0}, {1}, {2}, {3}, {4}, {5}
        };
        var y = new double[] {0, 2, 4, 6, 8, 10};

        var gb = new GradientBoostingRegressor(
            nEstimators: 20,
            learningRate: 0.1,
            maxDepth: 3);

        gb.Fit(X, y);
        var preds = gb.Predict(X);

        Assert.Equal(y.Length, preds.Length);
        foreach (var v in preds) Assert.False(double.IsNaN(v) || double.IsInfinity(v));
    }
}