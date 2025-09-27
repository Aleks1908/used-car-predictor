using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Backend.Evaluation
{
    public static class HyperparamSearch
    {
        public static (IRegressor bestModel, double bestRmse) GridSearch(
            Func<Dictionary<string, object>, IRegressor> modelFactory,
            List<Dictionary<string, object>> paramGrid,
            double[,] trainFeat, double[] trainLbl,
            double[,] valFeat,   double[] valLbl,
            LabelScaler labelScaler)
        {
            IRegressor? bestModel = null;
            double bestRmse = double.PositiveInfinity;

            foreach (var paramSet in paramGrid)
            {
                var model = modelFactory(paramSet);
                model.Fit(trainFeat, trainLbl);

                var scaledPreds = model.Predict(valFeat);
                var preds = labelScaler.InverseTransform(scaledPreds);
                var trueVals = labelScaler.InverseTransform(valLbl);
 
                var rmse = Metrics.RootMeanSquaredError(trueVals, preds);
                var mae  = Metrics.MeanAbsoluteError(trueVals, preds);
                var r2   = Metrics.RSquared(trueVals, preds);

                Console.WriteLine($"[{model.Name}] {string.Join(", ", paramSet.Select(kv => kv.Key + "=" + kv.Value))} " +
                                  $"-> RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                if (rmse < bestRmse)
                {
                    bestRmse = rmse;
                    bestModel = model;
                }
            }

            if (bestModel == null)
                throw new InvalidOperationException("Grid search did not produce a model.");

            return (bestModel, bestRmse);
        }
    }
}