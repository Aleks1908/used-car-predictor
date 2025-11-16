import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { ComparisonChart } from "../ComparisonChart";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
let capturedOptions: any;
jest.mock("react-chartjs-2", () => ({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  Bar: ({ options }: any) => {
    capturedOptions = options;
    return <div data-testid="bar-chart">Bar Chart</div>;
  },
}));

describe("ComparisonChart", () => {
  const mockCarAResults = [
    { algorithm: "linear", predictedPrice: 15000 },
    { algorithm: "ridge", predictedPrice: 15500 },
    { algorithm: "ridge_rf", predictedPrice: 16000 },
    { algorithm: "ridge_gb", predictedPrice: 16500 },
  ];

  const mockCarBResults = [
    { algorithm: "linear", predictedPrice: 18000 },
    { algorithm: "ridge", predictedPrice: 18500 },
    { algorithm: "ridge_rf", predictedPrice: 19000 },
    { algorithm: "ridge_gb", predictedPrice: 19500 },
  ];

  it("renders the chart component", () => {
    render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Toyota Camry"
        carBLabel="Honda Civic"
      />
    );

    expect(screen.getByTestId("bar-chart")).toBeInTheDocument();
  });

  it("renders with correct wrapper height", () => {
    const { container } = render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Car A"
        carBLabel="Car B"
      />
    );

    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass("h-[500px]");
  });

  it("handles empty results arrays", () => {
    render(
      <ComparisonChart
        carAResults={[]}
        carBResults={[]}
        carALabel="Car A"
        carBLabel="Car B"
      />
    );

    expect(screen.getByTestId("bar-chart")).toBeInTheDocument();
  });

  it("formats tooltip labels with dataset label and price", () => {
    render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Toyota Camry"
        carBLabel="Honda Civic"
      />
    );

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: { label: "Toyota Camry" },
      parsed: { y: 15000 },
    });

    expect(result).toMatch(/Toyota Camry: \$15[\s,]000/);
  });

  it("formats tooltip labels without dataset label", () => {
    render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Car A"
        carBLabel="Car B"
      />
    );

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: {},
      parsed: { y: 19000 },
    });

    expect(result).toMatch(/\$19[\s,]000/);
  });

  it("handles null price in tooltip", () => {
    render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Honda Civic"
        carBLabel="Toyota Camry"
      />
    );

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: { label: "Honda Civic" },
      parsed: { y: null },
    });

    expect(result).toBe("Honda Civic: ");
  });

  it("formats y-axis ticks as currency", () => {
    render(
      <ComparisonChart
        carAResults={mockCarAResults}
        carBResults={mockCarBResults}
        carALabel="Car A"
        carBLabel="Car B"
      />
    );

    const yAxisCallback = capturedOptions.scales.y.ticks.callback;

    expect(yAxisCallback(25000)).toMatch(/\$25[\s,]000/);
    expect(yAxisCallback("30000")).toMatch(/\$30[\s,]000/);
    expect(yAxisCallback(1000)).toBe("$1000");
  });

  it("uses unknown algorithm name when not in predefined list", () => {
    const customResults = [{ algorithm: "custom_algo", predictedPrice: 15000 }];

    render(
      <ComparisonChart
        carAResults={customResults}
        carBResults={customResults}
        carALabel="Car A"
        carBLabel="Car B"
      />
    );

    expect(screen.getByTestId("bar-chart")).toBeInTheDocument();
  });
});
