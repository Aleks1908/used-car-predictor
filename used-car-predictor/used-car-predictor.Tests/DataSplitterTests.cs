using System.Diagnostics.CodeAnalysis;
using Xunit;
using used_car_predictor.Backend.Data;

namespace used_car_predictor.Tests;

[ExcludeFromCodeCoverage]
public class DataSplitterTests
{
    [Fact]
    public void Split_SizesAndNoOverlap_AreCorrect()
    {
        var X = new double[,]
        {
            {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}
        };
        var y = new double[] {1,2,3,4,5,6,7,8,9,10};

        var (trainX, trainY, testX, testY) = DataSplitter.Split(X, y, trainRatio: 0.7);

        int n = X.GetLength(0);
        int trainN = trainX.GetLength(0);
        int testN  = testX.GetLength(0);

        Assert.Equal(n, trainN + testN);
        Assert.Equal(trainN, trainY.Length);
        Assert.Equal(testN, testY.Length);

        if (trainN > 0 && testN > 0)
            Assert.NotEqual(trainY[^1], testY[0]);
    }
}