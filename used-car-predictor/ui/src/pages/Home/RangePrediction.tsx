import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PredictionForm } from "@/components/PredictionForm";
import { PriceRangeChart } from "@/components/PriceRangeChart";
import { AlgorithmResultCard } from "@/components/AlgorithmResultCard";
import { usePredictionData } from "@/hooks/usePredictionData";
import { validateYearRange } from "@/utils/validation";
import type { RangePredictionResponse } from "@/types/prediction";

interface RangePredictionProps {
  onBack: () => void;
}

function RangePrediction({ onBack }: RangePredictionProps) {
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
  const [startYear, setStartYear] = useState("");
  const [endYear, setEndYear] = useState("");
  const [yearOfProduction, setYearOfProduction] = useState("");
  const [predictionResult, setPredictionResult] =
    useState<RangePredictionResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (
      !selectedManufacturer ||
      !selectedModel ||
      !selectedFuel ||
      !selectedTransmission ||
      !mileageKm ||
      !startYear ||
      !endYear ||
      !yearOfProduction
    ) {
      setError("Please fill in all fields");
      return;
    }

    const validationError = validateYearRange(
      yearOfProduction,
      startYear,
      endYear,
      details
    );
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);
    try {
      const res = await fetch("/api/v1/prediction/predict/range", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          manufacturer: selectedManufacturer,
          model: selectedModel,
          yearOfProduction: parseInt(yearOfProduction),
          transmission: selectedTransmission,
          fuelType: selectedFuel,
          mileageKm: parseInt(mileageKm),
          startYear: parseInt(startYear),
          endYear: parseInt(endYear),
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      console.log("Range prediction response:", data);
      setPredictionResult(data);
    } catch (err) {
      console.error("Range prediction failed:", err);
      setError(err instanceof Error ? err.message : "Range prediction failed");
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
    startYear &&
    endYear &&
    yearOfProduction;

  if (predictionResult !== null) {
    return (
      <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
        <div className="w-full max-w-6xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
          <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
            Range Prediction Results
          </h1>

          <div className="mb-12">
            <PriceRangeChart items={predictionResult.items} />
          </div>

          <div className="space-y-8">
            {predictionResult.items?.map((item) => (
              <div key={item.targetYear} className="space-y-4">
                <h2 className="text-2xl font-bold text-gray-900 text-center">
                  Year {item.targetYear}
                </h2>
                <div className="grid grid-cols-4 gap-4">
                  {item.results?.map((result) => (
                    <AlgorithmResultCard
                      key={result.algorithm}
                      result={result}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>

          <div className="text-center text-sm text-gray-600 mt-8 mb-8 space-y-1">
            <p>
              Model trained at:{" "}
              {new Date(predictionResult.modelInfo.trainedAt).toLocaleString()}
            </p>
            <p>
              Anchor target year: {predictionResult.modelInfo.anchorTargetYear}
            </p>
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
          Range Prediction
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
          onManufacturerChange={setSelectedManufacturer}
          onModelChange={setSelectedModel}
          onFuelChange={setSelectedFuel}
          onTransmissionChange={setSelectedTransmission}
          onYearOfProductionChange={setYearOfProduction}
          onMileageKmChange={setMileageKm}
          showTargetYear={false}
        >
          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              Start Year:
              {details?.minYear && (
                <span className="text-sm font-normal text-gray-600 ml-2">
                  (min: {details.minYear})
                </span>
              )}
            </Label>
            <Input
              type="number"
              placeholder="e.g., 2025"
              value={startYear}
              onChange={(e) => setStartYear(e.target.value)}
              min={details?.minYear}
              className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
            />
          </Field>

          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              End Year:
              {details?.minYear && (
                <span className="text-sm font-normal text-gray-600 ml-2">
                  (min: {details.minYear})
                </span>
              )}
            </Label>
            <Input
              type="number"
              placeholder="e.g., 2030"
              value={endYear}
              onChange={(e) => setEndYear(e.target.value)}
              min={details?.minYear}
              className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
            />
          </Field>
        </PredictionForm>

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
            {isSubmitting ? "Predicting..." : "Get Range Prediction"}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default RangePrediction;
