import { render, screen, fireEvent } from "@testing-library/react";
import "@testing-library/jest-dom";
import { PredictionForm } from "../PredictionForm";
import type { ComboboxOption } from "../ui/combobox";

jest.mock("../ui/combobox", () => ({
  Combobox: ({
    options,
    placeholder,
    value,
    onValueChange,
  }: {
    options: ComboboxOption[] | null;
    placeholder: string;
    value: string;
    onValueChange: (value: string) => void;
  }) => (
    <select
      data-testid={`combobox-${placeholder}`}
      value={value}
      onChange={(e) => onValueChange(e.target.value)}
    >
      <option value="">{placeholder}</option>
      {options?.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  ),
}));

describe("PredictionForm", () => {
  const mockManufacturers: ComboboxOption[] = [
    { value: "toyota", label: "Toyota" },
    { value: "honda", label: "Honda" },
  ];

  const mockModels: ComboboxOption[] = [
    { value: "corolla", label: "Corolla" },
    { value: "camry", label: "Camry" },
  ];

  const mockDetails = {
    fuels: [
      { value: "petrol", label: "Petrol" },
      { value: "diesel", label: "Diesel" },
    ],
    transmissions: [
      { value: "manual", label: "Manual" },
      { value: "automatic", label: "Automatic" },
    ],
    minYear: 2000,
    maxYear: 2020,
    anchorTargetYear: 2025,
  };

  const defaultProps = {
    manufacturers: mockManufacturers,
    models: mockModels,
    details: mockDetails,
    selectedManufacturer: "",
    selectedModel: "",
    selectedFuel: "",
    selectedTransmission: "",
    yearOfProduction: "",
    targetYear: "",
    mileageKm: "",
    onManufacturerChange: jest.fn(),
    onModelChange: jest.fn(),
    onFuelChange: jest.fn(),
    onTransmissionChange: jest.fn(),
    onYearOfProductionChange: jest.fn(),
    onTargetYearChange: jest.fn(),
    onMileageKmChange: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("manufacturer selection", () => {
    it("renders manufacturer combobox", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText("Select Manufacturer:")).toBeInTheDocument();
      expect(
        screen.getByTestId("combobox-Select manufacturer...")
      ).toBeInTheDocument();
    });

    it("calls onManufacturerChange when manufacturer is selected", () => {
      render(<PredictionForm {...defaultProps} />);

      const select = screen.getByTestId("combobox-Select manufacturer...");
      fireEvent.change(select, { target: { value: "toyota" } });

      expect(defaultProps.onManufacturerChange).toHaveBeenCalledWith("toyota");
    });
  });

  describe("model selection", () => {
    it("does not render model combobox when no manufacturer is selected", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.queryByText("Select Model:")).not.toBeInTheDocument();
    });

    it("renders model combobox when manufacturer is selected", () => {
      render(
        <PredictionForm {...defaultProps} selectedManufacturer="toyota" />
      );

      expect(screen.getByText("Select Model:")).toBeInTheDocument();
      expect(
        screen.getByTestId("combobox-Select model...")
      ).toBeInTheDocument();
    });

    it("calls onModelChange when model is selected", () => {
      render(
        <PredictionForm {...defaultProps} selectedManufacturer="toyota" />
      );

      const select = screen.getByTestId("combobox-Select model...");
      fireEvent.change(select, { target: { value: "corolla" } });

      expect(defaultProps.onModelChange).toHaveBeenCalledWith("corolla");
    });
  });

  describe("details section", () => {
    it("does not render details when details is null", () => {
      render(<PredictionForm {...defaultProps} details={null} />);

      expect(screen.queryByText("Fuels:")).not.toBeInTheDocument();
    });

    it("renders fuel combobox when details are available", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText("Fuels:")).toBeInTheDocument();
      expect(screen.getByTestId("combobox-Select fuel...")).toBeInTheDocument();
    });

    it("calls onFuelChange when fuel is selected", () => {
      render(<PredictionForm {...defaultProps} />);

      const select = screen.getByTestId("combobox-Select fuel...");
      fireEvent.change(select, { target: { value: "petrol" } });

      expect(defaultProps.onFuelChange).toHaveBeenCalledWith("petrol");
    });

    it("renders transmission combobox when details are available", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText("Transmissions:")).toBeInTheDocument();
      expect(
        screen.getByTestId("combobox-Select transmission...")
      ).toBeInTheDocument();
    });

    it("calls onTransmissionChange when transmission is selected", () => {
      render(<PredictionForm {...defaultProps} />);

      const select = screen.getByTestId("combobox-Select transmission...");
      fireEvent.change(select, { target: { value: "manual" } });

      expect(defaultProps.onTransmissionChange).toHaveBeenCalledWith("manual");
    });
  });

  describe("year of production field", () => {
    it("renders year of production input with range hint", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText(/Year of Production:/)).toBeInTheDocument();
      expect(screen.getByText(/\(2000 - 2020\)/)).toBeInTheDocument();
    });

    it("calls onYearOfProductionChange when value changes", () => {
      render(<PredictionForm {...defaultProps} />);

      const input = screen.getByPlaceholderText("e.g., 2020");
      fireEvent.change(input, { target: { value: "2015" } });

      expect(defaultProps.onYearOfProductionChange).toHaveBeenCalledWith(
        "2015"
      );
    });

    it("does not render range hint when min/max year not provided", () => {
      const detailsWithoutRange = {
        ...mockDetails,
        minYear: undefined,
        maxYear: undefined,
      };
      render(
        <PredictionForm {...defaultProps} details={detailsWithoutRange} />
      );

      expect(screen.queryByText(/\(2000 - 2020\)/)).not.toBeInTheDocument();
    });
  });

  describe("target year field", () => {
    it("renders target year input by default", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText(/Target Year:/)).toBeInTheDocument();
      expect(screen.getByPlaceholderText("e.g., 2025")).toBeInTheDocument();
    });

    it("renders min year hint for target year", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText(/\(min: 2000\)/)).toBeInTheDocument();
    });

    it("calls onTargetYearChange when value changes", () => {
      render(<PredictionForm {...defaultProps} />);

      const input = screen.getByPlaceholderText("e.g., 2025");
      fireEvent.change(input, { target: { value: "2025" } });

      expect(defaultProps.onTargetYearChange).toHaveBeenCalledWith("2025");
    });

    it("does not render target year when showTargetYear is false", () => {
      render(<PredictionForm {...defaultProps} showTargetYear={false} />);

      expect(screen.queryByText(/Target Year:/)).not.toBeInTheDocument();
    });
  });

  describe("mileage field", () => {
    it("renders mileage input", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText("Mileage (km):")).toBeInTheDocument();
      expect(screen.getByPlaceholderText("e.g., 80000")).toBeInTheDocument();
    });

    it("calls onMileageKmChange when value changes", () => {
      render(<PredictionForm {...defaultProps} />);

      const input = screen.getByPlaceholderText("e.g., 80000");
      fireEvent.change(input, { target: { value: "100000" } });

      expect(defaultProps.onMileageKmChange).toHaveBeenCalledWith("100000");
    });
  });

  describe("anchor target year note", () => {
    it("renders anchor target year note when provided", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(
        screen.getByText(/most accurate for predictions around the year/)
      ).toBeInTheDocument();
      expect(screen.getByText("2025")).toBeInTheDocument();
    });

    it("does not render anchor target year note when not provided", () => {
      const detailsWithoutAnchor = {
        ...mockDetails,
        anchorTargetYear: undefined,
      };
      render(
        <PredictionForm {...defaultProps} details={detailsWithoutAnchor} />
      );

      expect(
        screen.queryByText(/most accurate for predictions around the year/)
      ).not.toBeInTheDocument();
    });
  });

  describe("children prop", () => {
    it("renders children when provided", () => {
      render(
        <PredictionForm {...defaultProps}>
          <div data-testid="custom-child">Custom Content</div>
        </PredictionForm>
      );

      expect(screen.getByTestId("custom-child")).toBeInTheDocument();
      expect(screen.getByText("Custom Content")).toBeInTheDocument();
    });

    it("does not crash when children is not provided", () => {
      render(<PredictionForm {...defaultProps} />);

      expect(screen.getByText("Select Manufacturer:")).toBeInTheDocument();
    });
  });

  describe("form values", () => {
    it("displays selected manufacturer value", () => {
      render(
        <PredictionForm {...defaultProps} selectedManufacturer="toyota" />
      );

      const select = screen.getByTestId(
        "combobox-Select manufacturer..."
      ) as HTMLSelectElement;
      expect(select.value).toBe("toyota");
    });

    it("displays year of production value", () => {
      render(<PredictionForm {...defaultProps} yearOfProduction="2015" />);

      const input = screen.getByPlaceholderText(
        "e.g., 2020"
      ) as HTMLInputElement;
      expect(input.value).toBe("2015");
    });

    it("displays target year value", () => {
      render(<PredictionForm {...defaultProps} targetYear="2025" />);

      const input = screen.getByPlaceholderText(
        "e.g., 2025"
      ) as HTMLInputElement;
      expect(input.value).toBe("2025");
    });

    it("displays mileage value", () => {
      render(<PredictionForm {...defaultProps} mileageKm="100000" />);

      const input = screen.getByPlaceholderText(
        "e.g., 80000"
      ) as HTMLInputElement;
      expect(input.value).toBe("100000");
    });
  });
});
