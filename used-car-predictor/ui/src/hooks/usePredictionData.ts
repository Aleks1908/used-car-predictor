import { useState, useEffect } from "react";
import type { ComboboxOption } from "@/components/ui/combobox";

interface Details {
  fuels?: ComboboxOption[];
  transmissions?: ComboboxOption[];
  minYear?: number;
  maxYear?: number;
  anchorTargetYear?: number;
}

export function usePredictionData() {
  const [manufacturers, setManufacturers] = useState<ComboboxOption[] | null>(
    null
  );
  const [models, setModels] = useState<ComboboxOption[] | null>(null);
  const [selectedManufacturer, setSelectedManufacturer] = useState("");
  const [selectedModel, setSelectedModel] = useState("");
  const [details, setDetails] = useState<Details | null>(null);
  const [selectedFuel, setSelectedFuel] = useState("");
  const [selectedTransmission, setSelectedTransmission] = useState("");
  const [error, setError] = useState<string | null>(null);

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
      setSelectedModel("");
      setDetails(null);
      setSelectedFuel("");
      setSelectedTransmission("");

      async function loadModels() {
        try {
          setModels(null);
          setError(null);
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
    } else {
      setModels(null);
      setSelectedModel("");
      setDetails(null);
      setSelectedFuel("");
      setSelectedTransmission("");
    }
  }, [selectedManufacturer]);

  useEffect(() => {
    if (selectedManufacturer && selectedModel) {
      async function loadDetails() {
        try {
          setDetails(null);
          setSelectedFuel("");
          setSelectedTransmission("");
          setError(null);
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
            minYear: data.minYear,
            maxYear: data.maxYear,
            anchorTargetYear: data.anchorTargetYear,
          });
        } catch (err) {
          console.error("Model details fetch failed:", err);
          setError(err instanceof Error ? err.message : "An error occurred");
        }
      }
      loadDetails();
    } else {
      setDetails(null);
      setSelectedFuel("");
      setSelectedTransmission("");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedModel]);

  return {
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
  };
}
