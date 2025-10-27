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
  manufacturer: string;
  model: string;
  yearOfProduction: number;
  startYear: number;
  endYear: number;
  yearlyPredictions: {
    year: number;
    results: AlgorithmResult[];
  }[];
  modelInfo: {
    trainedAt: string;
    anchorTargetYear: number;
  };
}
