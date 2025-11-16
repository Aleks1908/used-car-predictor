import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import SinglePrediction from "../SinglePrediction";
import { usePredictionData } from "../../hooks/usePredictionData";

jest.mock("@/components/PredictionForm", () => ({
  PredictionForm: ({
    onYearOfProductionChange,
    onMileageKmChange,
    onTargetYearChange,
  }: {
    onYearOfProductionChange: (value: string) => void;
    onMileageKmChange: (value: string) => void;
    onTargetYearChange?: (value: string) => void;
  }) => (
    <div data-testid="prediction-form">
      <input
        placeholder="Year of Production"
        onChange={(e) => onYearOfProductionChange(e.target.value)}
      />
      <input
        placeholder="Mileage"
        onChange={(e) => onMileageKmChange(e.target.value)}
      />
      <input
        placeholder="Target Year"
        onChange={(e) => onTargetYearChange?.(e.target.value)}
      />
    </div>
  ),
}));

jest.mock("@/components/AlgorithmResultCard", () => ({
  AlgorithmResultCard: ({ result }: { result: { algorithm: string } }) => (
    <div data-testid="algorithm-result-card">{result.algorithm}</div>
  ),
}));

jest.mock("@/components/AlgorithmMetricsCard", () => ({
  AlgorithmMetricsCard: () => (
    <div data-testid="algorithm-metrics-card">Metrics</div>
  ),
}));

jest.mock("@/components/ModelInfoCard", () => ({
  ModelInfoCard: () => <div data-testid="model-info-card">Model Info</div>,
}));

jest.mock("@/hooks/usePredictionData");

const mockUsePredictionData = usePredictionData as jest.MockedFunction<
  typeof usePredictionData
>;

describe("SinglePrediction", () => {
  const mockOnBack = jest.fn();
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
      minYear: 2020,
      maxYear: 2025,
      fuels: [{ value: "petrol", label: "Petrol" }],
      transmissions: [{ value: "manual", label: "Manual" }],
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

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
    mockUsePredictionData.mockReturnValue(defaultMockData);
  });

  it("renders the single prediction form", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Single Prediction")).toBeInTheDocument();
    expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
  });

  it("shows back to home button", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("⾕ Back to Home")).toBeInTheDocument();
  });

  it("calls onBack when back to home button is clicked", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    const backButton = screen.getByText("⾕ Back to Home");
    fireEvent.click(backButton);

    expect(mockOnBack).toHaveBeenCalledTimes(1);
  });

  it("has Get Prediction button", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Get Prediction")).toBeInTheDocument();
  });

  it("renders prediction form component", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
  });

  it("displays correct title for single prediction", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Single Prediction")).toBeInTheDocument();
  });

  it("has proper container styling", () => {
    const { container } = render(<SinglePrediction onBack={mockOnBack} />);

    const mainDiv = container.firstChild as HTMLElement;
    expect(mainDiv).toHaveClass("min-h-screen");
  });

  it("Get Prediction button is initially disabled", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    const button = screen.getByText("Get Prediction");
    expect(button).toBeDisabled();
  });

  it("displays the prediction form with title", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Single Prediction")).toBeInTheDocument();
    expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
  });

  it("has both back and submit buttons", () => {
    render(<SinglePrediction onBack={mockOnBack} />);

    expect(screen.getByText("⾕ Back to Home")).toBeInTheDocument();
    expect(screen.getByText("Get Prediction")).toBeInTheDocument();
  });

  describe("form submission", () => {
    it("button is disabled when form is incomplete", () => {
      render(<SinglePrediction onBack={mockOnBack} />);

      const submitButton = screen.getByText("Get Prediction");

      expect(submitButton).toBeDisabled();
    });

    it("submits successfully with complete form data", async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      const mockResponse = {
        manufacturer: "toyota",
        model: "corolla",
        yearOfProduction: 2020,
        targetYear: 2026,
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
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          "/api/v1/prediction/predict",
          expect.objectContaining({
            method: "POST",
            headers: { "Content-Type": "application/json" },
          })
        );
      });

      await waitFor(() => {
        expect(screen.getByText("Prediction Results")).toBeInTheDocument();
      });
    });

    it("displays results after successful submission", async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      const mockResponse = {
        manufacturer: "toyota",
        model: "corolla",
        yearOfProduction: 2020,
        targetYear: 2026,
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
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText("Prediction Results")).toBeInTheDocument();
      });

      expect(screen.getByTestId("model-info-card")).toBeInTheDocument();
      expect(screen.getByTestId("algorithm-metrics-card")).toBeInTheDocument();
      expect(screen.getAllByTestId("algorithm-result-card")).toHaveLength(2);
    });

    it("handles fetch error", async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(mockSetError).toHaveBeenCalledWith("HTTP 500");
      });
    });

    it("handles validation error", () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2024" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2022" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      expect(mockSetError).toHaveBeenCalledWith(
        "Target year must be greater than or equal to year of production"
      );
    });
  });

  describe("results view", () => {
    it("displays back to form button in results view", async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      const mockResponse = {
        manufacturer: "toyota",
        model: "corolla",
        yearOfProduction: 2020,
        targetYear: 2026,
        results: [{ algorithm: "linear", predictedPrice: 25000 }],
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
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText("← Back to Form")).toBeInTheDocument();
      });

      const backToFormButton = screen.getByText("← Back to Form");
      fireEvent.click(backToFormButton);

      await waitFor(() => {
        expect(screen.getByText("Single Prediction")).toBeInTheDocument();
      });
    });

    it("calls onBack from results view", async () => {
      mockUsePredictionData.mockReturnValue({
        ...defaultMockData,
        selectedManufacturer: "toyota",
        selectedModel: "corolla",
        selectedFuel: "petrol",
        selectedTransmission: "manual",
      });

      const mockResponse = {
        manufacturer: "toyota",
        model: "corolla",
        yearOfProduction: 2020,
        targetYear: 2026,
        results: [{ algorithm: "linear", predictedPrice: 25000 }],
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
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      render(<SinglePrediction onBack={mockOnBack} />);

      const yearInput = screen.getByPlaceholderText("Year of Production");
      const mileageInput = screen.getByPlaceholderText("Mileage");
      const targetYearInput = screen.getByPlaceholderText("Target Year");

      fireEvent.change(yearInput, { target: { value: "2020" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(targetYearInput, { target: { value: "2026" } });

      const submitButton = screen.getByText("Get Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText("⾕ Back to Home")).toBeInTheDocument();
      });

      const backToHomeButton = screen.getByText("⾕ Back to Home");
      fireEvent.click(backToHomeButton);

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });
  });
});
