import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { RangeComparisonChart } from "../RangeComparisonChart";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
let capturedOptions: any;
jest.mock("react-chartjs-2", () => ({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  Line: ({ options }: any) => {
    capturedOptions = options;
    return <div data-testid="line-chart">Line Chart</div>;
  },
}));

describe("RangeComparisonChart", () => {
  const mockCarAData = [
    { year: 2024, predictedPrice: 15000 },
    { year: 2025, predictedPrice: 14000 },
    { year: 2026, predictedPrice: 13000 },
  ];

  const mockCarBData = [
    { year: 2024, predictedPrice: 18000 },
    { year: 2025, predictedPrice: 17000 },
    { year: 2026, predictedPrice: 16000 },
  ];

  it("renders the chart component", () => {
    render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Toyota Camry"
        carBLabel="Honda Civic"
        algorithm="linear"
      />
    );

    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("renders with correct wrapper height", () => {
    const { container } = render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="ridge"
      />
    );

    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass("h-[500px]");
  });

  it("handles empty data arrays", () => {
    render(
      <RangeComparisonChart
        carAData={[]}
        carBData={[]}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="ridge_rf"
      />
    );

    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("renders with different algorithms", () => {
    const algorithms = ["linear", "ridge", "ridge_rf", "ridge_gb"];

    algorithms.forEach((algo) => {
      const { rerender } = render(
        <RangeComparisonChart
          carAData={mockCarAData}
          carBData={mockCarBData}
          carALabel="Car A"
          carBLabel="Car B"
          algorithm={algo}
        />
      );

      expect(screen.getByTestId("line-chart")).toBeInTheDocument();
      rerender(<div />);
    });
  });

  it("handles single year data", () => {
    const singleYearA = [{ year: 2024, predictedPrice: 15000 }];
    const singleYearB = [{ year: 2024, predictedPrice: 18000 }];

    render(
      <RangeComparisonChart
        carAData={singleYearA}
        carBData={singleYearB}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="linear"
      />
    );

    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("formats tooltip labels with dataset label and price", () => {
    render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Toyota Camry"
        carBLabel="Honda Civic"
        algorithm="linear"
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
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="ridge"
      />
    );

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: {},
      parsed: { y: 18000 },
    });

    expect(result).toMatch(/\$18[\s,]000/);
  });

  it("handles null price in tooltip", () => {
    render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="ridge_rf"
      />
    );

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: { label: "Car A" },
      parsed: { y: null },
    });

    expect(result).toBe("Car A: ");
  });

  it("formats y-axis ticks as currency", () => {
    render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="ridge_gb"
      />
    );

    const yAxisCallback = capturedOptions.scales.y.ticks.callback;

    expect(yAxisCallback(25000)).toMatch(/\$25[\s,]000/);
    expect(yAxisCallback("30000")).toMatch(/\$30[\s,]000/);
    expect(yAxisCallback(1000)).toBe("$1000");
  });

  it("uses unknown algorithm name in title when not in predefined list", () => {
    render(
      <RangeComparisonChart
        carAData={mockCarAData}
        carBData={mockCarBData}
        carALabel="Car A"
        carBLabel="Car B"
        algorithm="unknown_algo"
      />
    );

    const titleText = capturedOptions.plugins.title.text;
    expect(titleText).toBe("Price Comparison Over Years (unknown_algo)");
  });
});
