using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Evaluation;
using used_car_predictor.Backend.Models;
using Xunit;

[ExcludeFromCodeCoverage]
public class ResidualLearningImprovementTests
{
    [Fact]
    public void RidgePlusRF_Beats_Ridge_On_QuadraticSignal()
    {
        var rnd = new Random(42);
        int n = 240;
        var rawX = new double[n,1];
        var rawY = new double[n];

        for (int i = 0; i < n; i++)
        {
            double x = -4 + 8.0 * i / (n - 1);
            rawX[i,0] = x;
            rawY[i]   = 3.0 * x + 0.5 * x * x + rnd.NextDouble()*0.2 - 0.1;
        }

        var (trainX, trainY, testX, testY) = DataSplitter.Split(rawX, rawY, trainRatio: 0.7);

        var fScaler = new FeatureScaler();
        var lScaler = new LabelScaler();

        var Xtr = fScaler.FitTransform(trainX);
        var Xte = fScaler.Transform(testX);
        var ytr = lScaler.FitTransform(trainY);
        var yte = lScaler.Transform(testY);
        
        var (ridge, _, _, _) = RidgeRegression.TrainWithBestParamsKFold(
            Xtr, ytr, lScaler,
            kFolds: 5, minExp: -6, maxExp: 2, alphaSteps: 40, seed: 123);

        var ridgePred = lScaler.InverseTransform(ridge.Predict(Xte));
        var ridgeRmse = Metrics.RootMeanSquaredError(testY, ridgePred);
        
        var (tx, ty, vx, vy) = DataSplitter.Split(Xtr, ytr, trainRatio: 0.75);

        var ridgeTrainPred = ridge.Predict(tx);
        var ridgeValPred   = ridge.Predict(vx);

        double[] trainRes = Residuals(ty, ridgeTrainPred);
        double[] valRes   = Residuals(vy, ridgeValPred);
        
        var (rfModel, _, _, _) = RandomForestRegressor.TrainResidualsWithBestParams(
            tx, trainRes, vx, valRes, maxConfigs: 30, searchSeed: 123);
        
        var combined = ridge.Predict(Xte);
        var rfRes    = rfModel.Predict(Xte);
        for (int i = 0; i < combined.Length; i++) combined[i] += rfRes[i];

        var combinedPred = lScaler.InverseTransform(combined);
        var combinedRmse = Metrics.RootMeanSquaredError(testY, combinedPred);

        combinedRmse.Should().BeLessThan(ridgeRmse * 0.9);
    }

    private static double[] Residuals(double[] truth, double[] pred)
    {
        var r = new double[truth.Length];
        for (int i = 0; i < r.Length; i++) r[i] = truth[i] - pred[i];
        return r;
    }
}