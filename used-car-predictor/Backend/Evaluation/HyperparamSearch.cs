using used_car_predictor.Backend.Data;
using used_car_predictor.Backend.Models;

namespace used_car_predictor.Backend.Evaluation
{
    public static class HyperparamSearch
    {
        public static (IRegressor bestModel, double bestRmse, Dictionary<string, object> bestParams) GridSearch(
            Func<Dictionary<string, object>, IRegressor> modelFactory,
            List<Dictionary<string, object>> paramGrid,
            double[,] trainFeat, double[] trainLbl,
            double[,] valFeat, double[] valLbl,
            LabelScaler labelScaler)
        {
            IRegressor? bestModel = null;
            double bestRmse = double.PositiveInfinity;
            Dictionary<string, object>? bestParams = null;
            object gate = new();

            System.Threading.Tasks.Parallel.ForEach(paramGrid, paramSet =>
            {
                var model = modelFactory(paramSet);
                model.Fit(trainFeat, trainLbl);

                var preds = labelScaler.InverseTransform(model.Predict(valFeat));
                var truth = labelScaler.InverseTransform(valLbl);

                var rmse = Metrics.RootMeanSquaredError(truth, preds);
                var mae = Metrics.MeanAbsoluteError(truth, preds);
                var r2 = Metrics.RSquared(truth, preds);

                Console.WriteLine(
                    $"[{model.Name}] {string.Join(", ", paramSet.Select(kv => kv.Key + "=" + kv.Value))} -> RMSE={rmse:F2}, MAE={mae:F2}, R²={r2:F3}");

                lock (gate)
                {
                    if (rmse < bestRmse)
                    {
                        bestRmse = rmse;
                        bestModel = model;
                        bestParams = new Dictionary<string, object>(paramSet);
                    }
                }
            });

            if (bestModel == null) throw new InvalidOperationException("Grid search did not produce a model.");

            Console.WriteLine(
                $"[GridSearch] Best params: {string.Join(", ", bestParams!.Select(kv => kv.Key + "=" + kv.Value))}");
            return (bestModel, bestRmse, bestParams!);
        }
    }
}