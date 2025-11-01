interface AlgorithmMetric {
  metrics: {
    mse: number;
    mae: number;
    r2: number;
  };
  timing: {
    meanTrialMs: number | null;
    trials: number | null;
    totalMs: number | null;
  };
}

interface AlgorithmMetricsCardProps {
  metrics: {
    linear: AlgorithmMetric;
    ridge: AlgorithmMetric;
    ridge_rf: AlgorithmMetric;
    ridge_gb: AlgorithmMetric;
  };
}

const algorithmNames: Record<string, string> = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Gradient Boosting",
};

export function AlgorithmMetricsCard({ metrics }: AlgorithmMetricsCardProps) {
  return (
    <div className="bg-white p-6 rounded-lg border-2 border-gray-300 shadow-sm">
      <h3 className="text-xl font-bold text-gray-900 mb-4 text-center">
        Algorithm Performance Metrics
      </h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {Object.entries(metrics).map(([algorithm, data]) => (
          <div
            key={algorithm}
            className="border border-gray-200 rounded-lg p-4 bg-gray-50"
          >
            <h4 className="text-lg font-semibold text-gray-900 mb-3 text-center">
              {algorithmNames[algorithm]}
            </h4>
            <div className="space-y-2 text-sm text-gray-600">
              <div className="font-medium text-gray-700 mb-2">Metrics:</div>
              <div className="flex justify-between pl-2">
                <span>MSE:</span>
                <span className="font-semibold text-gray-900">
                  {data.metrics.mse.toFixed(2)}
                </span>
              </div>
              <div className="flex justify-between pl-2">
                <span>MAE:</span>
                <span className="font-semibold text-gray-900">
                  {data.metrics.mae.toFixed(2)}
                </span>
              </div>
              <div className="flex justify-between pl-2">
                <span>R²:</span>
                <span className="font-semibold text-gray-900">
                  {data.metrics.r2.toFixed(3)}
                </span>
              </div>

              <div className="font-medium text-gray-700 mt-3 mb-2">Timing:</div>
              <div className="flex justify-between pl-2">
                <span>Total Time:</span>
                <span className="font-semibold text-gray-900">
                  {data.timing.totalMs !== null
                    ? (data.timing.totalMs / 1000).toFixed(3)
                    : "N/A"}{" "}
                  s
                </span>
              </div>
              {data.timing.trials !== null && (
                <>
                  <div className="flex justify-between pl-2">
                    <span>Trials:</span>
                    <span className="font-semibold text-gray-900">
                      {data.timing.trials}
                    </span>
                  </div>
                  <div className="flex justify-between pl-2">
                    <span>Mean Trial:</span>
                    <span className="font-semibold text-gray-900">
                      {data.timing.meanTrialMs !== null
                        ? (data.timing.meanTrialMs / 1000).toFixed(3)
                        : "N/A"}{" "}
                      s
                    </span>
                  </div>
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
