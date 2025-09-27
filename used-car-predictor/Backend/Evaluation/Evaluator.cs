using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Backend.Evaluation
{
    public static class Evaluator
    {
        public static void Evaluate(
            IRegressor model,
            double[,] testFeatures,
            double[] testLabels,
            LabelScaler labelScaler)
        {
            var scaledPreds = model.Predict(testFeatures);
            
            var preds = labelScaler.InverseTransform(scaledPreds);
            var trueVals = labelScaler.InverseTransform(testLabels);
            
            var mae = Metrics.MeanAbsoluteError(trueVals, preds);
            var rmse = Metrics.RootMeanSquaredError(trueVals, preds);
            var r2 = Metrics.RSquared(trueVals, preds);

            Console.WriteLine($"{model.Name} evaluation:");
            Console.WriteLine($"  MAE  = {mae:F2}");
            Console.WriteLine($"  RMSE = {rmse:F2}");
            Console.WriteLine($"  R²   = {r2:F3}");
            Console.WriteLine();
        }
    }
}