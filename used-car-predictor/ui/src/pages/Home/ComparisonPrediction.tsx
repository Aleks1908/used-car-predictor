import { useState } from "react";
import { Button } from "@/components/ui/button";
import { PredictionForm } from "@/components/PredictionForm";
import { ComparisonChart } from "@/components/ComparisonChart";
import { AlgorithmResultCard } from "@/components/AlgorithmResultCard";
import { usePredictionData } from "@/hooks/usePredictionData";
import { validateYears } from "@/utils/validation";
import { formatCarName } from "@/utils/formatting";
import type { PredictionResponse } from "@/types/prediction";

interface ComparisonPredictionProps {
  onBack: () => void;
}

interface ComparisonResponse {
  carA: PredictionResponse;
  carB: PredictionResponse;
}

function ComparisonPrediction({ onBack }: ComparisonPredictionProps) {
  const [step, setStep] = useState<"carA" | "carB">("carA");

  const carA = usePredictionData();
  const [carAMileageKm, setCarAMileageKm] = useState("");
  const [carATargetYear, setCarATargetYear] = useState("");
  const [carAYearOfProduction, setCarAYearOfProduction] = useState("");

  const carB = usePredictionData();
  const [carBMileageKm, setCarBMileageKm] = useState("");
  const [carBTargetYear, setCarBTargetYear] = useState("");
  const [carBYearOfProduction, setCarBYearOfProduction] = useState("");

  const [predictionResult, setPredictionResult] =
    useState<ComparisonResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isCarAComplete =
    carA.selectedManufacturer &&
    carA.selectedModel &&
    carA.selectedFuel &&
    carA.selectedTransmission &&
    carAMileageKm &&
    carATargetYear &&
    carAYearOfProduction;

  const isCarBComplete =
    carB.selectedManufacturer &&
    carB.selectedModel &&
    carB.selectedFuel &&
    carB.selectedTransmission &&
    carBMileageKm &&
    carBTargetYear &&
    carBYearOfProduction;

  const handleNextToCarB = () => {
    if (!isCarAComplete) {
      setError("Please fill in all fields for Car A");
      return;
    }

    const validationError = validateYears(
      carAYearOfProduction,
      carATargetYear,
      carA.details
    );
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setStep("carB");
  };

  const handleSubmit = async () => {
    if (!isCarBComplete) {
      setError("Please fill in all fields for Car B");
      return;
    }

    const validationError = validateYears(
      carBYearOfProduction,
      carBTargetYear,
      carB.details
    );
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/v1/prediction/predict-two", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          carA: {
            manufacturer: carA.selectedManufacturer,
            model: carA.selectedModel,
            yearOfProduction: parseInt(carAYearOfProduction),
            targetYear: parseInt(carATargetYear),
            transmission: carA.selectedTransmission,
            fuelType: carA.selectedFuel,
            mileageKm: parseInt(carAMileageKm),
          },
          carB: {
            manufacturer: carB.selectedManufacturer,
            model: carB.selectedModel,
            yearOfProduction: parseInt(carBYearOfProduction),
            targetYear: parseInt(carBTargetYear),
            transmission: carB.selectedTransmission,
            fuelType: carB.selectedFuel,
            mileageKm: parseInt(carBMileageKm),
          },
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      console.log("Comparison prediction response:", data);
      setPredictionResult(data);
    } catch (err) {
      console.error("Comparison prediction failed:", err);
      setError(
        err instanceof Error ? err.message : "Comparison prediction failed"
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  if (predictionResult !== null) {
    const carALabel = `${formatCarName(
      predictionResult.carA.manufacturer,
      predictionResult.carA.model
    )} (${predictionResult.carA.yearOfProduction})`;
    const carBLabel = `${formatCarName(
      predictionResult.carB.manufacturer,
      predictionResult.carB.model
    )} (${predictionResult.carB.yearOfProduction})`;

    return (
      <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
        <div className="w-full max-w-6xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
          <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
            Comparison Results
          </h1>

          <div className="mb-12">
            <ComparisonChart
              carAResults={predictionResult.carA.results}
              carBResults={predictionResult.carB.results}
              carALabel={carALabel}
              carBLabel={carBLabel}
            />
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-4 text-center">
              {carALabel} - Target Year {predictionResult.carA.targetYear}
            </h2>
            <div className="grid grid-cols-4 gap-4">
              {predictionResult.carA.results.map((result) => (
                <AlgorithmResultCard key={result.algorithm} result={result} />
              ))}
            </div>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-4 text-center">
              {carBLabel} - Target Year {predictionResult.carB.targetYear}
            </h2>
            <div className="grid grid-cols-4 gap-4">
              {predictionResult.carB.results.map((result) => (
                <AlgorithmResultCard key={result.algorithm} result={result} />
              ))}
            </div>
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
              ← Back to Home
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
          Car Comparison
        </h1>
        <h2 className="text-3xl font-semibold text-center text-gray-700 mb-12">
          {step === "carA" ? "First Car" : "Second Car"}
        </h2>

        {step === "carA" && (
          <>
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
              targetYear={carATargetYear}
              onManufacturerChange={carA.setSelectedManufacturer}
              onModelChange={carA.setSelectedModel}
              onFuelChange={carA.setSelectedFuel}
              onTransmissionChange={carA.setSelectedTransmission}
              onYearOfProductionChange={setCarAYearOfProduction}
              onMileageKmChange={setCarAMileageKm}
              onTargetYearChange={setCarATargetYear}
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
                ← Back to Home
              </Button>
              {isCarAComplete && (
                <Button
                  onClick={handleNextToCarB}
                  className="bg-gray-900 text-white hover:bg-gray-700"
                >
                  Next: Car B →
                </Button>
              )}
            </div>
          </>
        )}

        {step === "carB" && (
          <>
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
              targetYear={carBTargetYear}
              onManufacturerChange={carB.setSelectedManufacturer}
              onModelChange={carB.setSelectedModel}
              onFuelChange={carB.setSelectedFuel}
              onTransmissionChange={carB.setSelectedTransmission}
              onYearOfProductionChange={setCarBYearOfProduction}
              onMileageKmChange={setCarBMileageKm}
              onTargetYearChange={setCarBTargetYear}
              showTargetYear={true}
            />

            {error && (
              <p className="text-center text-red-600 font-semibold mt-6">
                Error: {error}
              </p>
            )}

            <div className="flex justify-center gap-4 mt-8">
              <Button
                onClick={() => setStep("carA")}
                variant="outline"
                className="border-gray-400 text-gray-900 hover:bg-gray-50"
              >
                ← Back to Car A
              </Button>
              {isCarBComplete && (
                <Button
                  onClick={handleSubmit}
                  disabled={isSubmitting}
                  className="bg-gray-900 text-white hover:bg-gray-700"
                >
                  {isSubmitting ? "Comparing..." : "Compare Cars"}
                </Button>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export default ComparisonPrediction;
