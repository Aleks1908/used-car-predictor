using System.Diagnostics.CodeAnalysis;
using Xunit;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Tests;

[ExcludeFromCodeCoverage]
public class RidgeRegressionTests
{
    [Fact]
    public void ClosedForm_FitsSimpleLinearFunction()
    {
        var X = new double[,]
        {
            {0}, {1}, {2}, {3}, {4}
        };
        var y = new double[] {1, 3, 5, 7, 9};

        var rr = new RidgeRegression(lambda: 1e-6, useClosedForm: true);
        rr.Fit(X, y);
        
        var pred = rr.Predict(new double[] {5});
        Assert.InRange(pred, 10.999, 11.001);

        Assert.InRange(rr.Bias, 0.999, 1.001);
        Assert.InRange(rr.Weights[0], 1.999, 2.001);
    }
}