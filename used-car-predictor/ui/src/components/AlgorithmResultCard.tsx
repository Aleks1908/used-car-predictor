import type { AlgorithmResult } from "@/types/prediction";

interface AlgorithmResultCardProps {
  result: AlgorithmResult;
}

const algorithmNames: Record<string, string> = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Gradient Boosting",
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
      </div>
    </div>
  );
}
