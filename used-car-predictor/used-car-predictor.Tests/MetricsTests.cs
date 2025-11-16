using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using used_car_predictor.Backend.Evaluation;
using Assert = Xunit.Assert;

namespace used_car_predictor.Tests;

[ExcludeFromCodeCoverage]
public class MetricsTests
{
    [Fact]
    public void Mae_IsExact_OnSmallArray()
    {
        var y  = new[] { 10.0, 12.0, 14.0, 16.0 };
        var yhat = new[] { 11.0, 13.0, 13.0, 15.0 };
        // abs errors: 1,1,1,1 => MAE = 1
        var mae = Metrics.MeanAbsoluteError(y, yhat);
        Assert.Equal(1.0, mae, precision: 10);
    }

    [Fact]
    public void Rmse_IsExact_OnSmallArray()
    {
        var y  = new[] { 2.0, 2.0, 2.0 };
        var yhat = new[] { 1.0, 2.0, 3.0 };
        var rmse = Metrics.RootMeanSquaredError(y, yhat);
        Assert.Equal(Math.Sqrt(2.0/3.0), rmse, precision: 10);
    }

    [Fact]
    public void R2_PerfectFit_IsOne()
    {
        var y = new[] { 1.0, 2.0, 3.0, 4.0 };
        var yhat = new[] { 1.0, 2.0, 3.0, 4.0 };
        var r2 = Metrics.RSquared(y, yhat);
        Assert.Equal(1.0, r2, precision: 12);
    }

    [Fact]
    public void R2_WorseThanMean_IsNegative()
    {
        var y = new[] { 0.0, 0.0, 10.0, 10.0 };
        var yhat = new[] { 100.0, 100.0, -100.0, -100.0 };
        var r2 = Metrics.RSquared(y, yhat);
        Assert.True(r2 < 0);
    }
}