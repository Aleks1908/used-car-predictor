import { useState, useEffect } from "react";
import { Combobox } from "@/components/ui/combobox";
import type { ComboboxOption } from "@/components/ui/combobox";
import { Button } from "@/components/ui/button";

interface SinglePredictionProps {
  onBack: () => void;
}

function SinglePrediction({ onBack }: SinglePredictionProps) {
  const [manufacturers, setManufacturers] = useState<ComboboxOption[] | null>(
    null
  );
  const [models, setModels] = useState<ComboboxOption[] | null>(null);
  const [selectedManufacturer, setSelectedManufacturer] = useState("");
  const [selectedModel, setSelectedModel] = useState("");
  const [details, setDetails] = useState<{
    fuels?: ComboboxOption[];
    transmissions?: ComboboxOption[];
  } | null>(null);
  const [selectedFuel, setSelectedFuel] = useState("");
  const [selectedTransmission, setSelectedTransmission] = useState("");
  const [mileageKm, setMileageKm] = useState("");
  const [targetYear, setTargetYear] = useState("");
  const [yearOfProduction, setYearOfProduction] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [predictionResult, setPredictionResult] = useState<unknown>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    async function loadManufacturers() {
      try {
        const res = await fetch("/api/v1/manufacturers");
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        const opts = Array.isArray(data)
          ? data.map((d: unknown) => {
              if (typeof d === "string") return { value: d, label: d };
              const obj = d as Record<string, unknown>;
              if (
                obj &&
                typeof obj.value === "string" &&
                typeof obj.label === "string"
              )
                return { value: obj.value, label: obj.label };
              const name = (obj.name ?? obj.manufacturer ?? obj.id) as unknown;
              const s = name == null ? "" : String(name);
              return { value: s, label: s };
            })
          : [];
        setManufacturers(opts);
      } catch (err) {
        console.error("Manufacturers fetch failed:", err);
        setError(err instanceof Error ? err.message : "An error occurred");
      }
    }
    loadManufacturers();
  }, []);

  useEffect(() => {
    if (selectedManufacturer) {
      async function loadModels() {
        try {
          setModels(null);
          setSelectedModel("");
          setDetails(null);
          const res = await fetch("/api/v1/models/list", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ manufacturer: selectedManufacturer }),
          });
          if (!res.ok) throw new Error(`HTTP ${res.status}`);
          const data = await res.json();
          const opts = Array.isArray(data)
            ? data.map((d: unknown) => {
                if (typeof d === "string") return { value: d, label: d };
                const obj = d as Record<string, unknown>;
                if (
                  obj &&
                  typeof obj.value === "string" &&
                  typeof obj.label === "string"
                )
                  return { value: obj.value, label: obj.label };
                const name = (obj.name ?? obj.model ?? obj.id) as unknown;
                const s = name == null ? "" : String(name);
                return { value: s, label: s };
              })
            : [];
          setModels(opts);
        } catch (err) {
          console.error("Models fetch failed:", err);
          setError(err instanceof Error ? err.message : "An error occurred");
        }
      }
      loadModels();
    }
  }, [selectedManufacturer]);

  useEffect(() => {
    if (selectedManufacturer && selectedModel) {
      async function loadDetails() {
        try {
          setDetails(null);
          const res = await fetch("/api/v1/models/details", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              manufacturer: selectedManufacturer,
              model: selectedModel,
            }),
          });
          if (!res.ok) throw new Error(`HTTP ${res.status}`);
          const data = await res.json();
          setDetails({
            fuels: data.fuels || [],
            transmissions: data.transmissions || [],
          });
        } catch (err) {
          console.error("Model details fetch failed:", err);
          setError(err instanceof Error ? err.message : "An error occurred");
        }
      }
      loadDetails();
    }
  }, [selectedManufacturer, selectedModel]);

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

  return (
    <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
      <div className="w-full max-w-5xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
        <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
          Single Prediction
        </h1>

        <div className="space-y-6">
          {error && (
            <p className="text-center text-red-600 font-semibold">
              Error: {error}
            </p>
          )}

          <div className="flex flex-col items-center gap-4">
            <label className="text-lg font-semibold text-gray-900">
              Select Manufacturer:
            </label>
            <Combobox
              options={manufacturers}
              placeholder="Select manufacturer..."
              searchPlaceholder="Search manufacturers..."
              emptyText="No manufacturer found."
              className="w-80"
              value={selectedManufacturer}
              onValueChange={setSelectedManufacturer}
            />
          </div>

          {selectedManufacturer && (
            <div className="flex flex-col items-center gap-4">
              <label className="text-lg font-semibold text-gray-900">
                Select Model:
              </label>
              <Combobox
                options={models}
                placeholder="Select model..."
                searchPlaceholder="Search models..."
                emptyText="No model found."
                className="w-80"
                value={selectedModel}
                onValueChange={setSelectedModel}
              />
            </div>
          )}

          {details && (
            <div className="flex flex-col items-center gap-4">
              <label className="text-lg font-semibold text-gray-900">
                Fuels:
              </label>
              <Combobox
                options={details.fuels || null}
                placeholder="Select fuel..."
                searchPlaceholder="Search fuels..."
                emptyText="No fuel found."
                className="w-80"
                value={selectedFuel}
                onValueChange={setSelectedFuel}
              />

              <label className="text-lg font-semibold text-gray-900 mt-4">
                Transmissions:
              </label>
              <Combobox
                options={details.transmissions || null}
                placeholder="Select transmission..."
                searchPlaceholder="Search transmissions..."
                emptyText="No transmission found."
                className="w-80"
                value={selectedTransmission}
                onValueChange={setSelectedTransmission}
              />

              <label className="text-lg font-semibold text-gray-900 mt-4">
                Year of Production:
              </label>
              <input
                type="number"
                placeholder="e.g., 2006"
                value={yearOfProduction}
                onChange={(e) => setYearOfProduction(e.target.value)}
                className="w-80 px-4 py-2 border-2 border-gray-400 rounded-md focus:outline-none focus:border-gray-900"
              />

              <label className="text-lg font-semibold text-gray-900 mt-4">
                Target Year:
              </label>
              <input
                type="number"
                placeholder="e.g., 2025"
                value={targetYear}
                onChange={(e) => setTargetYear(e.target.value)}
                className="w-80 px-4 py-2 border-2 border-gray-400 rounded-md focus:outline-none focus:border-gray-900"
              />

              <label className="text-lg font-semibold text-gray-900 mt-4">
                Mileage (km):
              </label>
              <input
                type="number"
                placeholder="e.g., 80000"
                value={mileageKm}
                onChange={(e) => setMileageKm(e.target.value)}
                className="w-80 px-4 py-2 border-2 border-gray-400 rounded-md focus:outline-none focus:border-gray-900"
              />
            </div>
          )}

          {predictionResult !== null && (
            <div className="mt-6 p-6 bg-gray-50 rounded-lg border-2 border-gray-300">
              <h2 className="text-2xl font-bold text-gray-900 mb-4 text-center">
                Prediction Result
              </h2>
              <pre className="text-sm text-gray-700 whitespace-pre-wrap">
                {JSON.stringify(predictionResult, null, 2)}
              </pre>
            </div>
          )}

          <div className="flex justify-center gap-4 mt-8">
            {isFormComplete && (
              <Button
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="bg-gray-900 text-white hover:bg-gray-700"
              >
                {isSubmitting ? "Predicting..." : "Get Prediction"}
              </Button>
            )}
            <Button
              onClick={onBack}
              variant="outline"
              className="border-gray-400 text-gray-900 hover:bg-gray-50"
            >
              Back to Home
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default SinglePrediction;
