export interface PredictionMetrics {
  mse: number;
  mae: number;
  r2: number;
}

export interface AlgorithmResult {
  algorithm: string;
  predictedPrice: number;
  metrics: PredictionMetrics;
}

export interface PredictionResponse {
  manufacturer: string;
  model: string;
  yearOfProduction: number;
  targetYear: number;
  results: AlgorithmResult[];
  modelInfo: {
    trainedAt: string;
    anchorTargetYear: number;
  };
}

export interface RangePredictionResponse {
  items: {
    manufacturer: string;
    model: string;
    yearOfProduction: number;
    targetYear: number;
    results: AlgorithmResult[];
  }[];
  modelInfo: {
    trainedAt: string;
    anchorTargetYear: number;
    totalRows: number;
  };
}
