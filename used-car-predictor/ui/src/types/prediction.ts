export interface PredictionMetrics {
  mse: number;
  mae: number;
  r2: number;
}

export interface AlgorithmResult {
  algorithm: string;
  predictedPrice: number;
  metrics?: PredictionMetrics;
}

export interface AlgorithmMetric {
  metrics: {
    mse: number;
    mae: number;
    r2: number;
  };
  timing: {
    meanTrialMs: number | null;
    trials: number | null;
    totalMs: number;
  };
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
    totalRows: number;
  };
  metrics: {
    linear: AlgorithmMetric;
    ridge: AlgorithmMetric;
    ridge_rf: AlgorithmMetric;
    ridge_gb: AlgorithmMetric;
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
  metrics: {
    linear: AlgorithmMetric;
    ridge: AlgorithmMetric;
    ridge_rf: AlgorithmMetric;
    ridge_gb: AlgorithmMetric;
  };
}
