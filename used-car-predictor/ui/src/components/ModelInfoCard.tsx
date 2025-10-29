interface ModelInfoCardProps {
  carName: string;
  modelInfo: {
    trainedAt: string;
    anchorTargetYear: number;
    totalRows: number;
    mse: number;
    mae: number;
    r2: number;
  };
}

export function ModelInfoCard({ carName, modelInfo }: ModelInfoCardProps) {
  return (
    <div className="bg-white p-4 rounded-lg border-2 border-gray-300 shadow-sm">
      <h4 className="text-lg font-bold text-gray-900 mb-3 text-center">
        {carName} - Model Info
      </h4>
      <div className="space-y-2 text-sm text-gray-600">
        <div className="flex justify-between">
          <span>Model trained:</span>
          <span className="font-semibold text-gray-900">
            {new Date(modelInfo.trainedAt).toLocaleDateString()}
          </span>
        </div>
        <div className="flex justify-between">
          <span>Anchor year:</span>
          <span className="font-semibold text-gray-900">
            {modelInfo.anchorTargetYear}
          </span>
        </div>
        <div className="flex justify-between">
          <span>Training data:</span>
          <span className="font-semibold text-gray-900">
            {modelInfo.totalRows} rows
          </span>
        </div>
        <div className="pt-2 mt-2 border-t border-gray-200 space-y-1">
          <div className="flex justify-between">
            <span>MSE:</span>
            <span className="font-semibold text-gray-900">
              {modelInfo.mse.toFixed(2)}
            </span>
          </div>
          <div className="flex justify-between">
            <span>MAE:</span>
            <span className="font-semibold text-gray-900">
              {modelInfo.mae.toFixed(2)}
            </span>
          </div>
          <div className="flex justify-between">
            <span>R²:</span>
            <span className="font-semibold text-gray-900">
              {modelInfo.r2.toFixed(3)}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
