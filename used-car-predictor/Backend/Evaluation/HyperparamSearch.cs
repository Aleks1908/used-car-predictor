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
            double[,] valFeat, double[] valLbl,
            LabelScaler labelScaler)
        {
            IRegressor? bestModel = null;
            double bestRmse = double.PositiveInfinity;
            object gate = new object();

            System.Threading.Tasks.Parallel.ForEach(paramGrid, paramSet =>
            {
                var model = modelFactory(paramSet);
                model.Fit(trainFeat, trainLbl);

                var preds = labelScaler.InverseTransform(model.Predict(valFeat));
                var truth = labelScaler.InverseTransform(valLbl);

                var rmse = Metrics.RootMeanSquaredError(truth, preds);

                lock (gate)
                {
                    if (rmse < bestRmse)
                    {
                        bestRmse = rmse;
                        bestModel = model;
                    }
                }
            });

            if (bestModel == null) throw new InvalidOperationException("Grid search did not produce a model.");
            return (bestModel, bestRmse);
        }
    }
}