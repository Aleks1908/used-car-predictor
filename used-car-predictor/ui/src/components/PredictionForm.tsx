import { Combobox } from "@/components/ui/combobox";
import type { ComboboxOption } from "@/components/ui/combobox";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface PredictionFormProps {
  manufacturers: ComboboxOption[] | null;
  models: ComboboxOption[] | null;
  details: {
    fuels?: ComboboxOption[];
    transmissions?: ComboboxOption[];
    minYear?: number;
    maxYear?: number;
    anchorTargetYear?: number;
  } | null;
  selectedManufacturer: string;
  selectedModel: string;
  selectedFuel: string;
  selectedTransmission: string;
  yearOfProduction: string;
  targetYear?: string;
  mileageKm: string;
  onManufacturerChange: (value: string) => void;
  onModelChange: (value: string) => void;
  onFuelChange: (value: string) => void;
  onTransmissionChange: (value: string) => void;
  onYearOfProductionChange: (value: string) => void;
  onTargetYearChange?: (value: string) => void;
  onMileageKmChange: (value: string) => void;
  showTargetYear?: boolean;
  children?: React.ReactNode;
}

export function PredictionForm({
  manufacturers,
  models,
  details,
  selectedManufacturer,
  selectedModel,
  selectedFuel,
  selectedTransmission,
  yearOfProduction,
  targetYear,
  mileageKm,
  onManufacturerChange,
  onModelChange,
  onFuelChange,
  onTransmissionChange,
  onYearOfProductionChange,
  onTargetYearChange,
  onMileageKmChange,
  showTargetYear = true,
  children,
}: PredictionFormProps) {
  return (
    <div className="space-y-6">
      <Field>
        <Label className="text-lg font-semibold text-gray-900">
          Select Manufacturer:
        </Label>
        <Combobox
          options={manufacturers}
          placeholder="Select manufacturer..."
          searchPlaceholder="Search manufacturers..."
          emptyText="No manufacturer found."
          className="w-80"
          value={selectedManufacturer}
          onValueChange={onManufacturerChange}
        />
      </Field>

      {selectedManufacturer && (
        <Field>
          <Label className="text-lg font-semibold text-gray-900">
            Select Model:
          </Label>
          <Combobox
            options={models}
            placeholder="Select model..."
            searchPlaceholder="Search models..."
            emptyText="No model found."
            className="w-80"
            value={selectedModel}
            onValueChange={onModelChange}
          />
        </Field>
      )}

      {details && (
        <div className="flex flex-col items-center gap-6">
          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              Fuels:
            </Label>
            <Combobox
              options={details.fuels || null}
              placeholder="Select fuel..."
              searchPlaceholder="Search fuels..."
              emptyText="No fuel found."
              className="w-80"
              value={selectedFuel}
              onValueChange={onFuelChange}
            />
          </Field>

          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              Transmissions:
            </Label>
            <Combobox
              options={details.transmissions || null}
              placeholder="Select transmission..."
              searchPlaceholder="Search transmissions..."
              emptyText="No transmission found."
              className="w-80"
              value={selectedTransmission}
              onValueChange={onTransmissionChange}
            />
          </Field>

          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              Year of Production:
              {details.minYear && details.maxYear && (
                <span className="text-sm font-normal text-gray-600 ml-2">
                  ({details.minYear} - {details.maxYear})
                </span>
              )}
            </Label>
            <Input
              type="number"
              placeholder={`e.g., ${details.maxYear || 2006}`}
              value={yearOfProduction}
              onChange={(e) => onYearOfProductionChange(e.target.value)}
              min={details.minYear}
              max={details.maxYear}
              className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
            />
          </Field>

          {showTargetYear !== false && (
            <Field>
              <Label className="text-lg font-semibold text-gray-900">
                Target Year:
                {details.minYear && (
                  <span className="text-sm font-normal text-gray-600 ml-2">
                    (min: {details.minYear})
                  </span>
                )}
              </Label>
              <Input
                type="number"
                placeholder="e.g., 2025"
                value={targetYear}
                onChange={(e) => onTargetYearChange?.(e.target.value)}
                min={details.minYear}
                className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
            </Field>
          )}

          <Field>
            <Label className="text-lg font-semibold text-gray-900">
              Mileage (km):
            </Label>
            <Input
              type="number"
              placeholder="e.g., 80000"
              value={mileageKm}
              onChange={(e) => onMileageKmChange(e.target.value)}
              className="w-80 h-10 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
            />
          </Field>

          {children}

          {details.anchorTargetYear && (
            <Field>
              <div className="w-80 bg-blue-50 border-2 border-blue-200 rounded-lg p-4">
                <p className="text-sm text-blue-900 text-center">
                  <span className="font-semibold">Note:</span> This model is
                  most accurate for predictions around the year{" "}
                  <span className="font-bold">{details.anchorTargetYear}</span>.
                  Predictions for years further from this anchor may be less
                  accurate.
                </p>
              </div>
            </Field>
          )}
        </div>
      )}
    </div>
  );
}
