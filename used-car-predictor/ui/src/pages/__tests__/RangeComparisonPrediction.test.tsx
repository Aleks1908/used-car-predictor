import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import { RangeComparisonPrediction } from "../RangeComparisonPrediction";
import { usePredictionData } from "../../hooks/usePredictionData";
import type { MockedFunction } from "jest-mock";

jest.mock("@/components/PredictionForm", () => ({
  PredictionForm: ({
    onYearOfProductionChange,
    onMileageKmChange,
    yearOfProduction,
    mileageKm,
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
    </div>
  ),
}));

jest.mock("@/components/RangeComparisonChart", () => ({
  RangeComparisonChart: ({
    carALabel,
    carBLabel,
    algorithm,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => (
    <div data-testid="range-comparison-chart">
      Chart: {carALabel} vs {carBLabel} - {algorithm}
    </div>
  ),
}));

jest.mock("@/components/AlgorithmResultCard", () => ({
  AlgorithmResultCard: ({
    result,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => (
    <div data-testid="algorithm-result-card">
      {result.algorithm}: ${result.predictedPrice}
    </div>
  ),
}));

jest.mock("@/components/ModelInfoCard", () => ({
  ModelInfoCard: ({
    carName,
  }: // eslint-disable-next-line @typescript-eslint/no-explicit-any
  any) => <div data-testid="model-info-card">{carName}</div>,
}));

// Mock the usePredictionData hook
jest.mock("@/hooks/usePredictionData");

const mockUsePredictionData = usePredictionData as MockedFunction<
  typeof usePredictionData
>;

const mockSetError = jest.fn();

const defaultMockData = {
  manufacturers: [
    { value: "toyota", label: "Toyota" },
    { value: "honda", label: "Honda" },
  ],
  models: [
    { value: "corolla", label: "Corolla" },
    { value: "civic", label: "Civic" },
  ],
  details: {
    minYear: 2025,
    maxYear: 2030,
    fuels: [
      { value: "petrol", label: "Petrol" },
      { value: "diesel", label: "Diesel" },
    ],
    transmissions: [
      { value: "manual", label: "Manual" },
      { value: "automatic", label: "Automatic" },
    ],
  },
  selectedManufacturer: "",
  selectedModel: "",
  selectedFuel: "",
  selectedTransmission: "",
  setSelectedManufacturer: jest.fn(),
  setSelectedModel: jest.fn(),
  setSelectedFuel: jest.fn(),
  setSelectedTransmission: jest.fn(),
  isLoadingManufacturers: false,
  isLoadingModels: false,
  isLoadingDetails: false,
  error: null,
  setError: mockSetError,
};

describe("RangeComparisonPrediction", () => {
  const mockOnBack = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
    mockUsePredictionData.mockReturnValue(defaultMockData);
  });

  describe("Step 1: Car A Configuration", () => {
    it("renders the initial Car A form", () => {
      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      expect(screen.getByText("Range Car Comparison")).toBeInTheDocument();
      expect(screen.getByText("First Car")).toBeInTheDocument();
      expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
    });

    it("shows back to home button", () => {
      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      expect(screen.getByText("⾕ Back to Home")).toBeInTheDocument();
    });

    it("calls onBack when back to home button is clicked", () => {
      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const backButton = screen.getByText("⾕ Back to Home");
      fireEvent.click(backButton);

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it("has Next button disabled when form incomplete", () => {
      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const nextButton = screen.getByText("Next: Configure Second Car →");
      expect(nextButton).toBeDisabled();
    });

    it("moves to step 2 when Car A is complete", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      expect(screen.getByText("Second Car")).toBeInTheDocument();
    });
  });

  describe("Step 2: Car B Configuration", () => {
    it("shows back to first car button", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      expect(screen.getByText("← Back to First Car")).toBeInTheDocument();
    });

    it("navigates back to step 1", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });

      const nextButton = screen.getByText("Next: Configure Second Car →");
      fireEvent.click(nextButton);

      const backButton = screen.getByText("← Back to First Car");
      fireEvent.click(backButton);

      expect(screen.getByText("First Car")).toBeInTheDocument();
    });

    it("moves to step 3 when Car B is complete", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const yearInputs = screen.getAllByPlaceholderText("Year of Production");
      const mileageInputs = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputs[0], { target: { value: "2020" } });
      fireEvent.change(mileageInputs[0], { target: { value: "50000" } });

      fireEvent.click(screen.getByText("Next: Configure Second Car →"));

      const yearInputsStep2 =
        screen.getAllByPlaceholderText("Year of Production");
      const mileageInputsStep2 = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputsStep2[0], { target: { value: "2019" } });
      fireEvent.change(mileageInputsStep2[0], { target: { value: "60000" } });

      fireEvent.click(screen.getByText("Next: Select Algorithm →"));

      expect(screen.getByText("Algorithm & Range")).toBeInTheDocument();
    });
  });

  describe("Step 3: Algorithm & Range Selection", () => {
    beforeEach(() => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });
    });

    const navigateToStep3 = () => {
      const { rerender } = render(
        <RangeComparisonPrediction onBack={mockOnBack} />
      );

      const yearInputs = screen.getAllByPlaceholderText("Year of Production");
      const mileageInputs = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputs[0], { target: { value: "2020" } });
      fireEvent.change(mileageInputs[0], { target: { value: "50000" } });
      fireEvent.click(screen.getByText("Next: Configure Second Car →"));

      const yearInputsStep2 =
        screen.getAllByPlaceholderText("Year of Production");
      const mileageInputsStep2 = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputsStep2[0], { target: { value: "2019" } });
      fireEvent.change(mileageInputsStep2[0], { target: { value: "60000" } });
      fireEvent.click(screen.getByText("Next: Select Algorithm →"));

      return rerender;
    };

    it("displays algorithm selection and year inputs", () => {
      navigateToStep3();

      expect(screen.getByText("Algorithm")).toBeInTheDocument();
      expect(screen.getByText("Start Year:")).toBeInTheDocument();
      expect(screen.getByText("End Year:")).toBeInTheDocument();
    });

    it("shows back to second car button", () => {
      navigateToStep3();

      expect(screen.getByText("← Back to Second Car")).toBeInTheDocument();
    });

    it("has Get Predictions button disabled when fields empty", () => {
      navigateToStep3();

      const predictButton = screen.getByText("Get Predictions");
      expect(predictButton).toBeDisabled();
    });

    it("validates start year minimum", async () => {
      navigateToStep3();

      const startYearInput = screen.getByPlaceholderText("e.g., 2026");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");

      fireEvent.change(startYearInput, { target: { value: "2020" } });
      fireEvent.change(endYearInput, { target: { value: "2030" } });

      const predictButton = screen.getByText("Get Predictions");
      fireEvent.click(predictButton);

      await waitFor(() => {
        expect(
          screen.getByText(/Start year must be at least 2025/i)
        ).toBeInTheDocument();
      });
    });

    it("validates end year >= start year", async () => {
      navigateToStep3();

      const startYearInput = screen.getByPlaceholderText("e.g., 2026");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");

      fireEvent.change(startYearInput, { target: { value: "2028" } });
      fireEvent.change(endYearInput, { target: { value: "2026" } });

      const predictButton = screen.getByText("Get Predictions");
      fireEvent.click(predictButton);

      await waitFor(() => {
        expect(
          screen.getByText(
            /End year must be greater than or equal to start year/i
          )
        ).toBeInTheDocument();
      });
    });

    it("submits successfully and displays results", async () => {
      const mockResponse = {
        algorithm: "ridge_gb",
        carA: [
          { year: 2026, predictedPrice: 25000 },
          { year: 2027, predictedPrice: 23000 },
        ],
        carB: [
          { year: 2026, predictedPrice: 24000 },
          { year: 2027, predictedPrice: 22000 },
        ],
        modelInfoA: {
          trainedAt: "2024-01-01",
          anchorTargetYear: 2025,
          totalRows: 1000,
        },
        modelInfoB: {
          trainedAt: "2024-01-01",
          anchorTargetYear: 2025,
          totalRows: 1000,
        },
        metricsA: {
          ridge_gb: {
            metrics: { mse: 100, mae: 50, r2: 0.95 },
            timing: { totalMs: 100, trials: 10, meanTrialMs: 10 },
          },
        },
        metricsB: {
          ridge_gb: {
            metrics: { mse: 110, mae: 55, r2: 0.94 },
            timing: { totalMs: 105, trials: 10, meanTrialMs: 10.5 },
          },
        },
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      navigateToStep3();

      const startYearInput = screen.getByPlaceholderText("e.g., 2026");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2027" } });

      const predictButton = screen.getByText("Get Predictions");
      fireEvent.click(predictButton);

      await waitFor(() => {
        expect(
          screen.getByText("Range Comparison Results")
        ).toBeInTheDocument();
      });

      expect(screen.getByTestId("range-comparison-chart")).toBeInTheDocument();
      expect(screen.getAllByTestId("model-info-card")).toHaveLength(2);
    });

    it("handles fetch error", async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
      });

      navigateToStep3();

      const startYearInput = screen.getByPlaceholderText("e.g., 2026");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2027" } });

      const predictButton = screen.getByText("Get Predictions");
      fireEvent.click(predictButton);

      await waitFor(() => {
        expect(
          screen.getByText(/HTTP error! status: 500/i)
        ).toBeInTheDocument();
      });
    });
  });

  describe("Results View", () => {
    const setupResults = async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      const mockResponse = {
        algorithm: "ridge_gb",
        carA: [
          { year: 2026, predictedPrice: 25000 },
          { year: 2027, predictedPrice: 23000 },
        ],
        carB: [
          { year: 2026, predictedPrice: 24000 },
          { year: 2027, predictedPrice: 22000 },
        ],
        modelInfoA: {
          trainedAt: "2024-01-01",
          anchorTargetYear: 2025,
          totalRows: 1000,
        },
        modelInfoB: {
          trainedAt: "2024-01-01",
          anchorTargetYear: 2025,
          totalRows: 1000,
        },
        metricsA: {
          ridge_gb: {
            metrics: { mse: 100, mae: 50, r2: 0.95 },
            timing: { totalMs: 100, trials: 10, meanTrialMs: 10 },
          },
        },
        metricsB: {
          ridge_gb: {
            metrics: { mse: 110, mae: 55, r2: 0.94 },
            timing: { totalMs: 105, trials: 10, meanTrialMs: 10.5 },
          },
        },
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<RangeComparisonPrediction onBack={mockOnBack} />);

      const yearInputs = screen.getAllByPlaceholderText("Year of Production");
      const mileageInputs = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputs[0], { target: { value: "2020" } });
      fireEvent.change(mileageInputs[0], { target: { value: "50000" } });
      fireEvent.click(screen.getByText("Next: Configure Second Car →"));

      const yearInputsStep2 =
        screen.getAllByPlaceholderText("Year of Production");
      const mileageInputsStep2 = screen.getAllByPlaceholderText("Mileage");
      fireEvent.change(yearInputsStep2[0], { target: { value: "2019" } });
      fireEvent.change(mileageInputsStep2[0], { target: { value: "60000" } });
      fireEvent.click(screen.getByText("Next: Select Algorithm →"));

      const startYearInput = screen.getByPlaceholderText("e.g., 2026");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2027" } });

      const predictButton = screen.getByText("Get Predictions");
      fireEvent.click(predictButton);

      await waitFor(() => {
        expect(
          screen.getByText("Range Comparison Results")
        ).toBeInTheDocument();
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
        expect(screen.getByText("Algorithm & Range")).toBeInTheDocument();
      });
    });

    it("calls onBack from results view", async () => {
      await setupResults();

      const backToHomeButton = screen.getByText("⾕ Back to Home");
      fireEvent.click(backToHomeButton);

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it("displays algorithm metrics for both cars", async () => {
      await setupResults();

      expect(
        screen.getByText("Algorithm Performance Metrics")
      ).toBeInTheDocument();
      expect(screen.getAllByText("Gradient Boosting")).toHaveLength(2);
    });
  });
});
