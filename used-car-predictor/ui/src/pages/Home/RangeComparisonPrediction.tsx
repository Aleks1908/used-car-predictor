import { Button } from "@/components/ui/button";
import { useState } from "react";
import { PredictionForm } from "@/components/PredictionForm";
import { usePredictionData } from "@/hooks/usePredictionData";
import { Field } from "@/components/ui/field";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { RangeComparisonChart } from "@/components/RangeComparisonChart";
import { AlgorithmResultCard } from "@/components/AlgorithmResultCard";
import { ModelInfoCard } from "@/components/ModelInfoCard";
import { formatCarName } from "@/utils/formatting";
import type { AlgorithmMetric } from "@/types/prediction";

interface RangeComparisonResponse {
  algorithm: string;
  carA: {
    year: number;
    predictedPrice: number;
  }[];
  carB: {
    year: number;
    predictedPrice: number;
  }[];
  modelInfoA: {
    trainedAt: string;
    anchorTargetYear: number;
    totalRows: number;
  };
  modelInfoB: {
    trainedAt: string;
    anchorTargetYear: number;
    totalRows: number;
  };
  metricsA: {
    [key: string]: AlgorithmMetric;
  };
  metricsB: {
    [key: string]: AlgorithmMetric;
  };
}

interface RangeComparisonPredictionProps {
  onBack: () => void;
}

export function RangeComparisonPrediction({
  onBack,
}: RangeComparisonPredictionProps) {
  const [step, setStep] = useState<1 | 2 | 3>(1);

  const carA = usePredictionData();
  const [carAMileageKm, setCarAMileageKm] = useState("");
  const [carAYearOfProduction, setCarAYearOfProduction] = useState("");

  const carB = usePredictionData();
  const [carBMileageKm, setCarBMileageKm] = useState("");
  const [carBYearOfProduction, setCarBYearOfProduction] = useState("");

  const [algorithm, setAlgorithm] = useState<string>("ridge_gb");
  const [startYear, setStartYear] = useState("");
  const [endYear, setEndYear] = useState("");

  const [result, setResult] = useState<RangeComparisonResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const isCarComplete = (
    car: ReturnType<typeof usePredictionData>,
    mileage: string,
    yearOfProduction: string
  ) => {
    return !!(
      car.selectedManufacturer &&
      car.selectedModel &&
      yearOfProduction &&
      car.selectedFuel &&
      car.selectedTransmission &&
      mileage
    );
  };

  const handleCarANext = () => {
    if (!isCarComplete(carA, carAMileageKm, carAYearOfProduction)) {
      alert("Please fill in all fields for Car A");
      return;
    }
    setStep(2);
  };

  const handleCarBNext = () => {
    if (!isCarComplete(carB, carBMileageKm, carBYearOfProduction)) {
      alert("Please fill in all fields for Car B");
      return;
    }
    setStep(3);
  };

  const handlePredict = async () => {
    if (!startYear || !endYear || !algorithm) {
      alert("Please fill in all fields");
      return;
    }

    const start = parseInt(startYear);
    const end = parseInt(endYear);
    const minYear = Math.max(
      carA.details?.minYear || 0,
      carB.details?.minYear || 0
    );

    if (start < minYear) {
      setError(`Start year must be at least ${minYear}`);
      return;
    }
    if (end < start) {
      setError("End year must be greater than or equal to start year");
      return;
    }

    setIsLoading(true);
    setError("");
    setResult(null);

    try {
      const payload = {
        carA: {
          targetYear: parseInt(startYear),
          manufacturer: carA.selectedManufacturer,
          model: carA.selectedModel,
          yearOfProduction: parseInt(carAYearOfProduction),
          transmission: carA.selectedTransmission,
          fuelType: carA.selectedFuel,
          mileageKm: parseInt(carAMileageKm),
        },
        carB: {
          targetYear: parseInt(startYear),
          manufacturer: carB.selectedManufacturer,
          model: carB.selectedModel,
          yearOfProduction: parseInt(carBYearOfProduction),
          transmission: carB.selectedTransmission,
          fuelType: carB.selectedFuel,
          mileageKm: parseInt(carBMileageKm),
        },
        startYear: parseInt(startYear),
        endYear: parseInt(endYear),
        algorithm,
      };

      const response = await fetch("/api/v1/prediction/predict-two/range", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: RangeComparisonResponse = await response.json();
      setResult(data);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "An error occurred during prediction"
      );
    } finally {
      setIsLoading(false);
    }
  };

  if (result !== null) {
    const carALabel = formatCarName(
      carA.selectedManufacturer,
      carA.selectedModel
    );
    const carBLabel = formatCarName(
      carB.selectedManufacturer,
      carB.selectedModel
    );

    return (
      <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
        <div className="w-full max-w-6xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
          <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
            Range Comparison Results
          </h1>

          <div className="mb-8 flex gap-4">
            <div className="flex-1">
              <ModelInfoCard
                carName={carALabel}
                modelInfo={result.modelInfoA}
                carDetails={{
                  yearOfProduction: parseInt(carAYearOfProduction),
                  transmission: carA.selectedTransmission,
                  fuelType: carA.selectedFuel,
                  mileageKm: parseInt(carAMileageKm),
                }}
              />
            </div>
            <div className="flex-1">
              <ModelInfoCard
                carName={carBLabel}
                modelInfo={result.modelInfoB}
                carDetails={{
                  yearOfProduction: parseInt(carBYearOfProduction),
                  transmission: carB.selectedTransmission,
                  fuelType: carB.selectedFuel,
                  mileageKm: parseInt(carBMileageKm),
                }}
              />
            </div>
          </div>

          <div className="mb-12">
            <RangeComparisonChart
              carAData={result.carA}
              carBData={result.carB}
              carALabel={carALabel}
              carBLabel={carBLabel}
              algorithm={result.algorithm}
            />
          </div>

          <div className="space-y-8 mb-8">
            {result.carA.map((carAItem, index) => {
              const carBItem = result.carB[index];
              return (
                <div key={carAItem.year}>
                  <h2 className="text-2xl font-bold text-gray-900 mb-4 text-center">
                    Year {carAItem.year}
                  </h2>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900 mb-3 text-center">
                        {carALabel}
                      </h3>
                      <AlgorithmResultCard
                        result={{
                          algorithm: result.algorithm,
                          predictedPrice: carAItem.predictedPrice,
                        }}
                      />
                    </div>
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900 mb-3 text-center">
                        {carBLabel}
                      </h3>
                      <AlgorithmResultCard
                        result={{
                          algorithm: result.algorithm,
                          predictedPrice: carBItem.predictedPrice,
                        }}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          <div className="bg-white p-6 rounded-lg border-2 border-gray-300 shadow-sm mb-8">
            <h3 className="text-2xl font-bold text-center text-gray-900 mb-6">
              Algorithm Performance Metrics
            </h3>
            <div className="grid grid-cols-2 gap-6">
              {Object.entries(result.metricsA).map(([algorithmKey, data]) => {
                const algorithmNames: { [key: string]: string } = {
                  linear: "Linear Regression",
                  ridge: "Ridge Regression",
                  ridge_rf: "Random Forest",
                  ridge_gb: "Ridge GB",
                };
                return (
                  <div
                    key={`carA-${algorithmKey}`}
                    className="border border-gray-200 rounded-lg p-4 bg-gray-50"
                  >
                    <div className="text-center mb-4">
                      <h3 className="text-xl font-bold text-gray-900">
                        {carALabel}
                      </h3>
                      <h4 className="text-lg font-semibold text-gray-700 mt-1">
                        {algorithmNames[algorithmKey] || algorithmKey}
                      </h4>
                    </div>

                    <div className="space-y-4">
                      <div>
                        <div className="text-sm font-semibold text-gray-700 mb-2">
                          Metrics:
                        </div>
                        <div className="space-y-1 text-sm text-gray-600">
                          <div className="flex justify-between">
                            <span>MSE:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.mse.toFixed(2)}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span>MAE:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.mae.toFixed(2)}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span>R²:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.r2.toFixed(3)}
                            </span>
                          </div>
                        </div>
                      </div>

                      <div>
                        <div className="text-sm font-semibold text-gray-700 mb-2">
                          Timing:
                        </div>
                        <div className="space-y-1 text-sm text-gray-600">
                          <div className="flex justify-between">
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
                              <div className="flex justify-between">
                                <span>Trials:</span>
                                <span className="font-semibold text-gray-900">
                                  {data.timing.trials}
                                </span>
                              </div>
                              <div className="flex justify-between">
                                <span>Mean Trial:</span>
                                <span className="font-semibold text-gray-900">
                                  {data.timing.meanTrialMs !== null
                                    ? (data.timing.meanTrialMs / 1000).toFixed(
                                        3
                                      )
                                    : "N/A"}{" "}
                                  s
                                </span>
                              </div>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}

              {Object.entries(result.metricsB).map(([algorithmKey, data]) => {
                const algorithmNames: { [key: string]: string } = {
                  linear: "Linear Regression",
                  ridge: "Ridge Regression",
                  ridge_rf: "Random Forest",
                  ridge_gb: "Ridge GB",
                };
                return (
                  <div
                    key={`carB-${algorithmKey}`}
                    className="border border-gray-200 rounded-lg p-4 bg-gray-50"
                  >
                    <div className="text-center mb-4">
                      <h3 className="text-xl font-bold text-gray-900">
                        {carBLabel}
                      </h3>
                      <h4 className="text-lg font-semibold text-gray-700 mt-1">
                        {algorithmNames[algorithmKey] || algorithmKey}
                      </h4>
                    </div>

                    <div className="space-y-4">
                      <div>
                        <div className="text-sm font-semibold text-gray-700 mb-2">
                          Metrics:
                        </div>
                        <div className="space-y-1 text-sm text-gray-600">
                          <div className="flex justify-between">
                            <span>MSE:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.mse.toFixed(2)}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span>MAE:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.mae.toFixed(2)}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span>R²:</span>
                            <span className="font-semibold text-gray-900">
                              {data.metrics.r2.toFixed(3)}
                            </span>
                          </div>
                        </div>
                      </div>

                      <div>
                        <div className="text-sm font-semibold text-gray-700 mb-2">
                          Timing:
                        </div>
                        <div className="space-y-1 text-sm text-gray-600">
                          <div className="flex justify-between">
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
                              <div className="flex justify-between">
                                <span>Trials:</span>
                                <span className="font-semibold text-gray-900">
                                  {data.timing.trials}
                                </span>
                              </div>
                              <div className="flex justify-between">
                                <span>Mean Trial:</span>
                                <span className="font-semibold text-gray-900">
                                  {data.timing.meanTrialMs !== null
                                    ? (data.timing.meanTrialMs / 1000).toFixed(
                                        3
                                      )
                                    : "N/A"}{" "}
                                  s
                                </span>
                              </div>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="flex justify-center gap-4">
            <Button
              onClick={() => setResult(null)}
              variant="outline"
              className="border-gray-400 text-gray-900 hover:bg-gray-50"
            >
              ← Back to Form
            </Button>
            <Button
              onClick={onBack}
              variant="outline"
              className="border-gray-400 text-gray-900 hover:bg-gray-50"
            >
              ⾕ Back to Home
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
      <div className="w-full max-w-5xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
        <h1 className="text-4xl font-bold text-center text-gray-900 mb-4">
          Range Car Comparison
        </h1>
        <h2 className="text-2xl font-semibold text-center text-gray-700 mb-12">
          {step === 1
            ? "First Car"
            : step === 2
            ? "Second Car"
            : "Algorithm & Range"}
        </h2>

        {step === 1 && (
          <div>
            <PredictionForm
              manufacturers={carA.manufacturers}
              models={carA.models}
              details={carA.details}
              selectedManufacturer={carA.selectedManufacturer}
              selectedModel={carA.selectedModel}
              selectedFuel={carA.selectedFuel}
              selectedTransmission={carA.selectedTransmission}
              yearOfProduction={carAYearOfProduction}
              mileageKm={carAMileageKm}
              onManufacturerChange={carA.setSelectedManufacturer}
              onModelChange={carA.setSelectedModel}
              onFuelChange={carA.setSelectedFuel}
              onTransmissionChange={carA.setSelectedTransmission}
              onYearOfProductionChange={setCarAYearOfProduction}
              onMileageKmChange={setCarAMileageKm}
              showTargetYear={false}
            />
          </div>
        )}

        {step === 2 && (
          <div>
            <PredictionForm
              manufacturers={carB.manufacturers}
              models={carB.models}
              details={carB.details}
              selectedManufacturer={carB.selectedManufacturer}
              selectedModel={carB.selectedModel}
              selectedFuel={carB.selectedFuel}
              selectedTransmission={carB.selectedTransmission}
              yearOfProduction={carBYearOfProduction}
              mileageKm={carBMileageKm}
              onManufacturerChange={carB.setSelectedManufacturer}
              onModelChange={carB.setSelectedModel}
              onFuelChange={carB.setSelectedFuel}
              onTransmissionChange={carB.setSelectedTransmission}
              onYearOfProductionChange={setCarBYearOfProduction}
              onMileageKmChange={setCarBMileageKm}
              showTargetYear={false}
            />
          </div>
        )}

        {step === 3 && !result && (
          <div className="space-y-6">
            <Field>
              <Label className="text-lg font-semibold text-gray-900">
                Algorithm
              </Label>
              <select
                id="algorithm"
                value={algorithm}
                onChange={(e) => setAlgorithm(e.target.value)}
                className="w-80 h-10 px-3 rounded-md border border-input bg-background"
              >
                <option value="linear">Linear Regression</option>
                <option value="ridge">Ridge Regression</option>
                <option value="ridge_rf">Random Forest</option>
                <option value="ridge_gb">Ridge GB</option>
              </select>
            </Field>

            <Field>
              <Label className="text-lg font-semibold text-gray-900">
                Start Year:
                {carA.details?.minYear && carB.details?.minYear && (
                  <span className="text-sm font-normal text-gray-600 ml-2">
                    (min: {Math.max(carA.details.minYear, carB.details.minYear)}
                    )
                  </span>
                )}
              </Label>
              <Input
                id="startYear"
                type="number"
                value={startYear}
                onChange={(e) => setStartYear(e.target.value)}
                placeholder="e.g., 2026"
                className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
            </Field>

            <Field>
              <Label className="text-lg font-semibold text-gray-900">
                End Year:
                {carA.details?.minYear && carB.details?.minYear && (
                  <span className="text-sm font-normal text-gray-600 ml-2">
                    (min: {Math.max(carA.details.minYear, carB.details.minYear)}
                    )
                  </span>
                )}
              </Label>
              <Input
                id="endYear"
                type="number"
                value={endYear}
                onChange={(e) => setEndYear(e.target.value)}
                placeholder="e.g., 2030"
                className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
            </Field>
          </div>
        )}

        {error && (
          <p className="text-center text-red-600 font-semibold mt-6">
            Error: {error}
          </p>
        )}

        <div className="flex justify-center gap-4 mt-8">
          <Button
            onClick={
              step === 1 ? onBack : () => setStep((s) => (s - 1) as 1 | 2 | 3)
            }
            variant="outline"
            className="border-gray-400 text-gray-900 hover:bg-gray-50"
          >
            {step === 1
              ? "⾕ Back to Home"
              : step === 2
              ? "← Back to First Car"
              : "← Back to Second Car"}
          </Button>
          {step === 1 && (
            <Button
              onClick={handleCarANext}
              disabled={
                !isCarComplete(carA, carAMileageKm, carAYearOfProduction)
              }
              className="bg-gray-900 text-white hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next: Configure Second Car →
            </Button>
          )}
          {step === 2 && (
            <Button
              onClick={handleCarBNext}
              disabled={
                !isCarComplete(carB, carBMileageKm, carBYearOfProduction)
              }
              className="bg-gray-900 text-white hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next: Select Algorithm →
            </Button>
          )}
          {step === 3 && (
            <Button
              onClick={handlePredict}
              disabled={!startYear || !endYear || !algorithm || isLoading}
              className="bg-gray-900 text-white hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? "Predicting..." : "Get Predictions"}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
