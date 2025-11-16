import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ComparisonPrediction from "../ComparisonPrediction";
import { validateYears } from "../../utils/validation";
import { usePredictionData } from "../../hooks/usePredictionData";
import type { MockedFunction } from "jest-mock";

jest.mock("@/components/PredictionForm", () => ({
  PredictionForm: ({
    onYearOfProductionChange,
    onMileageKmChange,
    onTargetYearChange,
    yearOfProduction,
    mileageKm,
    targetYear,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => (
    <div data-testid="prediction-form">
      <input
        placeholder="Year of Production"
        value={yearOfProduction}
        onChange={(e) => onYearOfProductionChange(e.target.value)}
      />
      <input
        placeholder="Mileage"
        value={mileageKm}
        onChange={(e) => onMileageKmChange(e.target.value)}
      />
      <input
        placeholder="Target Year"
        value={targetYear}
        onChange={(e) => onTargetYearChange(e.target.value)}
      />
    </div>
  ),
}));

jest.mock("@/components/ComparisonChart", () => ({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ComparisonChart: ({ carALabel, carBLabel }: any) => (
    <div data-testid="comparison-chart">
      {carALabel} vs {carBLabel}
    </div>
  ),
}));

jest.mock("@/components/AlgorithmResultCard", () => ({
  AlgorithmResultCard: ({
    result,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => <div data-testid="algorithm-result-card">{result.algorithm}</div>,
}));

jest.mock("@/components/AlgorithmMetricsCard", () => ({
  AlgorithmMetricsCard: () => (
    <div data-testid="algorithm-metrics-card">Metrics</div>
  ),
}));

jest.mock("@/components/ModelInfoCard", () => ({
  ModelInfoCard: ({
    carName,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => <div data-testid="model-info-card">{carName}</div>,
}));

jest.mock("@/hooks/usePredictionData");
jest.mock("@/utils/validation");
jest.mock("@/utils/formatting", () => ({
  formatCarName: (manufacturer: string, model: string) =>
    `${manufacturer} ${model}`,
}));

const mockUsePredictionData = usePredictionData as MockedFunction<
  typeof usePredictionData
>;
const mockValidateYears = validateYears as MockedFunction<typeof validateYears>;

const mockSetError = jest.fn();

const defaultMockData = {
  manufacturers: [{ value: "toyota", label: "Toyota" }],
  models: [{ value: "camry", label: "Camry" }],
  details: {
    fuels: [{ value: "petrol", label: "Petrol" }],
    transmissions: [{ value: "automatic", label: "Automatic" }],
    minYear: 2010,
    maxYear: 2024,
    anchorTargetYear: 2024,
  },
  selectedManufacturer: "",
  setSelectedManufacturer: jest.fn(),
  selectedModel: "",
  setSelectedModel: jest.fn(),
  selectedFuel: "",
  setSelectedFuel: jest.fn(),
  selectedTransmission: "",
  setSelectedTransmission: jest.fn(),
  error: null,
  setError: mockSetError,
  isLoadingManufacturers: false,
  isLoadingModels: false,
  isLoadingDetails: false,
};

describe("ComparisonPrediction", () => {
  const mockOnBack = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
    mockUsePredictionData.mockReturnValue(defaultMockData);
    mockValidateYears.mockReturnValue(null);
  });

  describe("Step 1: Car A Configuration", () => {
    it("renders the initial Car A form", () => {
      render(<ComparisonPrediction onBack={mockOnBack} />);

      expect(screen.getByText("Car Comparison")).toBeInTheDocument();
      expect(screen.getByText("First Car")).toBeInTheDocument();
      expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
    });

    it("shows back to home button", () => {
      render(<ComparisonPrediction onBack={mockOnBack} />);

      const backButton = screen.getByText("⾕ Back to Home");
      expect(backButton).toBeInTheDocument();
    });

    it("calls onBack when back to home button is clicked", () => {
      render(<ComparisonPrediction onBack={mockOnBack} />);

      const backButton = screen.getByText("⾕ Back to Home");
      fireEvent.click(backButton);

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it("has Next button disabled when form incomplete", () => {
      render(<ComparisonPrediction onBack={mockOnBack} />);

      const nextButton = screen.getByText("Next: Configure Second Car →");
      expect(nextButton).toBeDisabled();
    });

    it("shows validation error when years are invalid", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "camry",
        selectedFuel: "petrol",
        selectedTransmission: "automatic",
      });
      mockValidateYears.mockReturnValue("Invalid year range");

      render(<ComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      expect(screen.getByText("Error: Invalid year range")).toBeInTheDocument();
    });

    it("moves to step 2 when Car A is complete and valid", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "camry",
        selectedFuel: "petrol",
        selectedTransmission: "automatic",
      });

      render(<ComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      expect(screen.getByText("Second Car")).toBeInTheDocument();
    });
  });

  describe("Step 2: Car B Configuration", () => {
    const navigateToStep2 = () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "camry",
        selectedFuel: "petrol",
        selectedTransmission: "automatic",
      });

      render(<ComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);
    };

    it("shows back to first car button", () => {
      navigateToStep2();

      expect(screen.getByText("← Back to First Car")).toBeInTheDocument();
    });

    it("navigates back to step 1", () => {
      navigateToStep2();

      const backButton = screen.getByText("← Back to First Car");
      fireEvent.click(backButton);

      expect(screen.getByText("First Car")).toBeInTheDocument();
    });

    it("has Compare Cars button disabled when form incomplete", () => {
      navigateToStep2();

      const compareButton = screen.getByText("Compare Cars →");
      expect(compareButton).toBeDisabled();
    });

    it("shows validation error for Car B when years are invalid", () => {
      navigateToStep2();

      mockValidateYears.mockReturnValue("Target year too high");

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2019" } });
      fireEvent.change(mileageInput, { target: { value: "60000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const compareButton = screen.getByText("Compare Cars →");
      fireEvent.click(compareButton);

      expect(
        screen.getByText("Error: Target year too high")
      ).toBeInTheDocument();
    });

    it("submits successfully and displays comparison results", async () => {
      const mockResponse = {
        carA: {
          manufacturer: "toyota",
          model: "camry",
          yearOfProduction: 2020,
          targetYear: 2025,
          results: [
            { algorithm: "linear", predictedPrice: 25000 },
            { algorithm: "ridge", predictedPrice: 26000 },
            { algorithm: "ridge_rf", predictedPrice: 25500 },
            { algorithm: "ridge_gb", predictedPrice: 26500 },
          ],
          modelInfo: {
            trainedAt: "2024-01-01",
            anchorTargetYear: 2025,
            totalRows: 1000,
          },
          metrics: {
            linear: {
              metrics: { mse: 100, mae: 50, r2: 0.95 },
              timing: { totalMs: 100, trials: 10, meanTrialMs: 10 },
            },
          },
        },
        carB: {
          manufacturer: "honda",
          model: "civic",
          yearOfProduction: 2019,
          targetYear: 2025,
          results: [
            { algorithm: "linear", predictedPrice: 22000 },
            { algorithm: "ridge", predictedPrice: 23000 },
            { algorithm: "ridge_rf", predictedPrice: 22500 },
            { algorithm: "ridge_gb", predictedPrice: 23500 },
          ],
          modelInfo: {
            trainedAt: "2024-01-01",
            anchorTargetYear: 2025,
            totalRows: 1000,
          },
          metrics: {
            linear: {
              metrics: { mse: 110, mae: 55, r2: 0.94 },
              timing: { totalMs: 105, trials: 10, meanTrialMs: 10.5 },
            },
          },
        },
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      navigateToStep2();

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2019" } });
      fireEvent.change(mileageInput, { target: { value: "60000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const compareButton = screen.getByText("Compare Cars →");
      fireEvent.click(compareButton);

      await waitFor(() => {
        expect(screen.getByText("Comparison Results")).toBeInTheDocument();
      });

      expect(screen.getByTestId("comparison-chart")).toBeInTheDocument();
      expect(screen.getAllByTestId("model-info-card")).toHaveLength(2);
      expect(screen.getAllByTestId("algorithm-metrics-card")).toHaveLength(2);
    });

    it("handles fetch error", async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
      });

      navigateToStep2();

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2019" } });
      fireEvent.change(mileageInput, { target: { value: "60000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const compareButton = screen.getByText("Compare Cars →");
      fireEvent.click(compareButton);

      await waitFor(() => {
        expect(screen.getByText(/Error: HTTP 500/i)).toBeInTheDocument();
      });
    });
  });

  describe("Results View", () => {
    const setupResults = async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "camry",
        selectedFuel: "petrol",
        selectedTransmission: "automatic",
      });

      const mockResponse = {
        carA: {
          manufacturer: "toyota",
          model: "camry",
          yearOfProduction: 2020,
          targetYear: 2025,
          results: [
            { algorithm: "linear", predictedPrice: 25000 },
            { algorithm: "ridge", predictedPrice: 26000 },
          ],
          modelInfo: {
            trainedAt: "2024-01-01",
            anchorTargetYear: 2025,
            totalRows: 1000,
          },
          metrics: {
            linear: {
              metrics: { mse: 100, mae: 50, r2: 0.95 },
              timing: { totalMs: 100, trials: 10, meanTrialMs: 10 },
            },
          },
        },
        carB: {
          manufacturer: "honda",
          model: "civic",
          yearOfProduction: 2019,
          targetYear: 2025,
          results: [
            { algorithm: "linear", predictedPrice: 22000 },
            { algorithm: "ridge", predictedPrice: 23000 },
          ],
          modelInfo: {
            trainedAt: "2024-01-01",
            anchorTargetYear: 2025,
            totalRows: 1000,
          },
          metrics: {
            linear: {
              metrics: { mse: 110, mae: 55, r2: 0.94 },
              timing: { totalMs: 105, trials: 10, meanTrialMs: 10.5 },
            },
          },
        },
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<ComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2025" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      const yearInput2 = screen.getByPlaceholderText("Year of Production");
      const mileageInput2 = screen.getByPlaceholderText("Mileage");
      const targetYearInput2 = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput2, { target: { value: "2019" } });
      fireEvent.change(mileageInput2, { target: { value: "60000" } });
      fireEvent.change(targetYearInput2, { target: { value: "2025" } });

      const compareButton = screen.getByText("Compare Cars →");
      fireEvent.click(compareButton);

      await waitFor(() => {
        expect(screen.getByText("Comparison Results")).toBeInTheDocument();
      });
    };

    it("displays back to form button", async () => {
      await setupResults();

      expect(screen.getByText("← Back to Form")).toBeInTheDocument();
    });

    it("navigates back to form from results", async () => {
      await setupResults();

      const backToFormButton = screen.getByText("← Back to Form");
      fireEvent.click(backToFormButton);

      await waitFor(() => {
        expect(screen.getByText("Second Car")).toBeInTheDocument();
      });
    });

    it("calls onBack from results view", async () => {
      await setupResults();

      const backToHomeButton = screen.getByText("⾕ Back to Home");
      fireEvent.click(backToHomeButton);

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it("displays all result cards for both cars", async () => {
      await setupResults();

      expect(screen.getAllByTestId("algorithm-result-card")).toHaveLength(4);
    });
  });
});
