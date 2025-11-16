import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import RangePrediction from "../RangePrediction";
import { usePredictionData } from "../../hooks/usePredictionData";

jest.mock("@/components/PredictionForm", () => ({
  PredictionForm: ({
    children,
    onYearOfProductionChange,
    onMileageKmChange,
  }: {
    children: React.ReactNode;
    onYearOfProductionChange: (value: string) => void;
    onMileageKmChange: (value: string) => void;
  }) => (
    <div data-testid="prediction-form">
      <input
        placeholder="e.g., 2020"
        onChange={(e) => onYearOfProductionChange(e.target.value)}
      />
      <input
        placeholder="e.g., 80000"
        onChange={(e) => onMileageKmChange(e.target.value)}
      />
      {children}
    </div>
  ),
}));

jest.mock("@/components/PriceRangeChart", () => ({
  PriceRangeChart: () => <div data-testid="price-range-chart">Chart</div>,
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

describe("RangePrediction", () => {
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

  it("renders the range prediction form", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Range Prediction")).toBeInTheDocument();
    expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
  });

  it("shows back to home button", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText("⾕ Back to Home")).toBeInTheDocument();
  });

  it("calls onBack when back to home button is clicked", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    const backButton = screen.getByText("⾕ Back to Home");
    fireEvent.click(backButton);

    expect(mockOnBack).toHaveBeenCalledTimes(1);
  });

  it("has Get Range Prediction button", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Get Range Prediction")).toBeInTheDocument();
  });

  it("displays start year input label", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText(/Start Year:/)).toBeInTheDocument();
  });

  it("displays end year input label", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText(/End Year:/)).toBeInTheDocument();
  });

  it("shows minimum year hints for start and end year", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    const minYearHints = screen.getAllByText(/\(min: 2020\)/);
    expect(minYearHints.length).toBeGreaterThan(0);
  });

  it("has proper container styling", () => {
    const { container } = render(<RangePrediction onBack={mockOnBack} />);

    const mainDiv = container.firstChild as HTMLElement;
    expect(mainDiv).toHaveClass("min-h-screen");
  });

  it("displays correct title for range prediction", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByText("Range Prediction")).toBeInTheDocument();
  });

  it("renders prediction form component", () => {
    render(<RangePrediction onBack={mockOnBack} />);

    expect(screen.getByTestId("prediction-form")).toBeInTheDocument();
  });

  describe("form submission", () => {
    it("button is disabled when form is incomplete", () => {
      render(<RangePrediction onBack={mockOnBack} />);

      const submitButton = screen.getByText("Get Range Prediction");

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
        items: [
          {
            targetYear: 2026,
            manufacturer: "toyota",
            model: "corolla",
            yearOfProduction: 2020,
            results: [
              { algorithm: "linear", predictedPrice: 25000 },
              { algorithm: "ridge", predictedPrice: 26000 },
            ],
          },
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2028" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          "/api/v1/prediction/predict/range",
          expect.objectContaining({
            method: "POST",
            headers: { "Content-Type": "application/json" },
          })
        );
      });

      await waitFor(() => {
        expect(
          screen.getByText("Range Prediction Results")
        ).toBeInTheDocument();
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
        items: [
          {
            targetYear: 2026,
            manufacturer: "toyota",
            model: "corolla",
            yearOfProduction: 2020,
            results: [
              { algorithm: "linear", predictedPrice: 25000 },
              { algorithm: "ridge", predictedPrice: 26000 },
            ],
          },
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2028" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Range Prediction Results")
        ).toBeInTheDocument();
      });

      expect(screen.getByTestId("model-info-card")).toBeInTheDocument();
      expect(screen.getByTestId("price-range-chart")).toBeInTheDocument();
      expect(screen.getByTestId("algorithm-metrics-card")).toBeInTheDocument();
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2028" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2028" } });
      fireEvent.change(endYearInput, { target: { value: "2026" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
      fireEvent.click(submitButton);

      expect(mockSetError).toHaveBeenCalledWith(
        "End year must be greater than or equal to start year"
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
        items: [
          {
            targetYear: 2026,
            manufacturer: "toyota",
            model: "corolla",
            yearOfProduction: 2020,
            results: [{ algorithm: "linear", predictedPrice: 25000 }],
          },
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2028" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText("← Back to Form")).toBeInTheDocument();
      });

      const backToFormButton = screen.getByText("← Back to Form");
      fireEvent.click(backToFormButton);

      await waitFor(() => {
        expect(screen.getByText("Range Prediction")).toBeInTheDocument();
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
        items: [
          {
            targetYear: 2026,
            manufacturer: "toyota",
            model: "corolla",
            yearOfProduction: 2020,
            results: [{ algorithm: "linear", predictedPrice: 25000 }],
          },
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

      render(<RangePrediction onBack={mockOnBack} />);

      const startYearInput = screen.getByPlaceholderText("e.g., 2025");
      const endYearInput = screen.getByPlaceholderText("e.g., 2030");
      const mileageInput = screen.getByPlaceholderText("e.g., 80000");
      const yearOfProductionInput = screen.getByPlaceholderText(/e.g., 2020/);

      fireEvent.change(startYearInput, { target: { value: "2026" } });
      fireEvent.change(endYearInput, { target: { value: "2028" } });
      fireEvent.change(mileageInput, { target: { value: "50000" } });
      fireEvent.change(yearOfProductionInput, { target: { value: "2020" } });

      const submitButton = screen.getByText("Get Range Prediction");
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
