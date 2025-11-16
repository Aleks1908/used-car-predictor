import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { PriceRangeChart } from "../PriceRangeChart";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
let capturedOptions: any;
jest.mock("react-chartjs-2", () => ({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  Line: ({ options }: any) => {
    capturedOptions = options;
    return <div data-testid="line-chart">Line Chart</div>;
  },
}));

describe("PriceRangeChart", () => {
  const mockItems = [
    {
      targetYear: 2024,
      results: [
        { algorithm: "linear", predictedPrice: 15000 },
        { algorithm: "ridge", predictedPrice: 15500 },
        { algorithm: "ridge_rf", predictedPrice: 16000 },
        { algorithm: "ridge_gb", predictedPrice: 16500 },
      ],
    },
    {
      targetYear: 2025,
      results: [
        { algorithm: "linear", predictedPrice: 14000 },
        { algorithm: "ridge", predictedPrice: 14500 },
        { algorithm: "ridge_rf", predictedPrice: 15000 },
        { algorithm: "ridge_gb", predictedPrice: 15500 },
      ],
    },
  ];

  it("renders the chart component", () => {
    render(<PriceRangeChart items={mockItems} />);
    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("renders with correct wrapper height", () => {
    const { container } = render(<PriceRangeChart items={mockItems} />);
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper).toHaveClass("h-[500px]");
  });

  it("handles empty items array", () => {
    render(<PriceRangeChart items={[]} />);
    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("handles single year data", () => {
    const singleYearData = [
      {
        targetYear: 2024,
        results: [{ algorithm: "linear", predictedPrice: 15000 }],
      },
    ];

    render(<PriceRangeChart items={singleYearData} />);
    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });

  it("formats tooltip labels with dataset label and price", () => {
    render(<PriceRangeChart items={mockItems} />);

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: { label: "Linear Regression" },
      parsed: { y: 15000 },
    });

    expect(result).toMatch(/Linear Regression: \$15[\s,]000/);
  });

  it("formats tooltip labels without dataset label", () => {
    render(<PriceRangeChart items={mockItems} />);

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: {},
      parsed: { y: 18000 },
    });

    expect(result).toMatch(/\$18[\s,]000/);
  });

  it("handles null price in tooltip", () => {
    render(<PriceRangeChart items={mockItems} />);

    const tooltipCallback = capturedOptions.plugins.tooltip.callbacks.label;
    const result = tooltipCallback({
      dataset: { label: "Ridge Regression" },
      parsed: { y: null },
    });

    expect(result).toBe("Ridge Regression: ");
  });

  it("formats y-axis ticks as currency", () => {
    render(<PriceRangeChart items={mockItems} />);

    const yAxisCallback = capturedOptions.scales.y.ticks.callback;

    expect(yAxisCallback(25000)).toMatch(/\$25[\s,]000/);
    expect(yAxisCallback("30000")).toMatch(/\$30[\s,]000/);
    expect(yAxisCallback(1000)).toBe("$1000");
  });

  it("handles missing algorithm in results", () => {
    const itemsWithMissingAlgo = [
      {
        targetYear: 2024,
        results: [
          { algorithm: "linear", predictedPrice: 15000 },
          { algorithm: "ridge", predictedPrice: 15500 },
        ],
      },
      {
        targetYear: 2025,
        results: [{ algorithm: "linear", predictedPrice: 14000 }],
      },
    ];

    render(<PriceRangeChart items={itemsWithMissingAlgo} />);
    expect(screen.getByTestId("line-chart")).toBeInTheDocument();
  });
});
