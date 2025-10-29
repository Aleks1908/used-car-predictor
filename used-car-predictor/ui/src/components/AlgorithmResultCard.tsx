import type { AlgorithmResult } from "@/types/prediction";

interface AlgorithmResultCardProps {
  result: AlgorithmResult;
}

const algorithmNames: Record<string, string> = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Ridge GB",
};

export function AlgorithmResultCard({ result }: AlgorithmResultCardProps) {
  return (
    <div className="bg-white p-4 rounded-lg border-2 border-gray-300 shadow-sm">
      <h3 className="text-lg font-bold text-gray-900 mb-3 text-center capitalize">
        {algorithmNames[result.algorithm] || result.algorithm}
      </h3>

      <div className="space-y-2">
        <div className="text-center  ">
          <p className="text-sm text-gray-600 mb-1">Predicted Price</p>
          <p className="text-2xl font-bold text-gray-900">
            ${result.predictedPrice.toLocaleString()}
          </p>
        </div>

        {result.metrics && (
          <div className="pt-5 space-y-1 border-t border-gray-200">
            <div className="flex justify-between">
              <span className="text-xs text-gray-600">MSE:</span>
              <span className="text-xs font-semibold text-gray-900">
                {result.metrics.mse.toFixed(2)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-xs text-gray-600">MAE:</span>
              <span className="text-xs font-semibold text-gray-900">
                {result.metrics.mae.toFixed(2)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-xs text-gray-600">R²:</span>
              <span className="text-xs font-semibold text-gray-900">
                {result.metrics.r2.toFixed(3)}
              </span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
