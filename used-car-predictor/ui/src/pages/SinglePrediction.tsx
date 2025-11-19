import { useState } from "react";
import { Button } from "@/components/ui/button";
import { PredictionForm } from "@/components/PredictionForm";
import { AlgorithmResultCard } from "@/components/AlgorithmResultCard";
import { AlgorithmMetricsCard } from "@/components/AlgorithmMetricsCard";
import { ModelInfoCard } from "@/components/ModelInfoCard";
import { usePredictionData } from "@/hooks/usePredictionData";
import { validateYears } from "@/utils/validation";
import { formatCarName } from "@/utils/formatting";
import type { PredictionResponse } from "@/types/prediction";

interface SinglePredictionProps {
  onBack: () => void;
}

function SinglePrediction({ onBack }: SinglePredictionProps) {
  const {
    manufacturers,
    models,
    selectedManufacturer,
    setSelectedManufacturer,
    selectedModel,
    setSelectedModel,
    details,
    selectedFuel,
    setSelectedFuel,
    selectedTransmission,
    setSelectedTransmission,
    error,
    setError,
  } = usePredictionData();

  const [mileageKm, setMileageKm] = useState("");
  const [targetYear, setTargetYear] = useState("");
  const [yearOfProduction, setYearOfProduction] = useState("");
  const [predictionResult, setPredictionResult] =
    useState<PredictionResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (
      !selectedManufacturer ||
      !selectedModel ||
      !selectedFuel ||
      !selectedTransmission ||
      !mileageKm ||
      !targetYear ||
      !yearOfProduction
    ) {
      setError("Please fill in all fields");
      return;
    }

    const validationError = validateYears(
      yearOfProduction,
      targetYear,
      details
    );
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/v1/prediction/predict", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          manufacturer: selectedManufacturer,
          model: selectedModel,
          yearOfProduction: parseInt(yearOfProduction),
          targetYear: parseInt(targetYear),
          transmission: selectedTransmission,
          fuelType: selectedFuel,
          mileageKm: parseInt(mileageKm),
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setPredictionResult(data);
    } catch (err) {
      console.error("Prediction failed:", err);
      setError(err instanceof Error ? err.message : "Prediction failed");
    } finally {
      setIsSubmitting(false);
    }
  };

  const isFormComplete =
    selectedManufacturer &&
    selectedModel &&
    selectedFuel &&
    selectedTransmission &&
    mileageKm &&
    targetYear &&
    yearOfProduction;

  if (predictionResult !== null) {
    const carName = formatCarName(predictionResult.model);

    return (
      <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
        <div className="w-full max-w-6xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
          <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
            Prediction Results
          </h1>

          <div className="mb-8">
            <ModelInfoCard
              carName={carName}
              modelInfo={predictionResult.modelInfo}
              carDetails={{
                yearOfProduction: predictionResult.yearOfProduction,
                transmission: selectedTransmission,
                fuelType: selectedFuel,
                mileageKm: parseInt(mileageKm),
              }}
            />
          </div>

          <div className="grid grid-cols-4 gap-4 mb-8">
            {predictionResult.results.map((result) => (
              <AlgorithmResultCard key={result.algorithm} result={result} />
            ))}
          </div>

          <div className="mb-8">
            <AlgorithmMetricsCard metrics={predictionResult.metrics} />
          </div>

          <div className="flex justify-center gap-4">
            <Button
              onClick={() => setPredictionResult(null)}
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
        <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
          Single Prediction
        </h1>

        <PredictionForm
          manufacturers={manufacturers}
          models={models}
          details={details}
          selectedManufacturer={selectedManufacturer}
          selectedModel={selectedModel}
          selectedFuel={selectedFuel}
          selectedTransmission={selectedTransmission}
          yearOfProduction={yearOfProduction}
          mileageKm={mileageKm}
          targetYear={targetYear}
          onManufacturerChange={setSelectedManufacturer}
          onModelChange={setSelectedModel}
          onFuelChange={setSelectedFuel}
          onTransmissionChange={setSelectedTransmission}
          onYearOfProductionChange={setYearOfProduction}
          onMileageKmChange={setMileageKm}
          onTargetYearChange={setTargetYear}
          showTargetYear={true}
        />

        {error && (
          <p className="text-center text-red-600 font-semibold mt-6">
            Error: {error}
          </p>
        )}

        <div className="flex justify-center gap-4 mt-8">
          <Button
            onClick={onBack}
            variant="outline"
            className="border-gray-400 text-gray-900 hover:bg-gray-50"
          >
            ⾕ Back to Home
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!isFormComplete || isSubmitting}
            className="bg-gray-900 text-white hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? "Predicting..." : "Get Prediction"}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default SinglePrediction;
