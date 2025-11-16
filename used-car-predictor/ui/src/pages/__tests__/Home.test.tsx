import { render, screen, fireEvent } from "@testing-library/react";
import "@testing-library/jest-dom";
import Home from "../Home";

jest.mock("../SinglePrediction", () => ({
  __esModule: true,
  default: ({ onBack }: { onBack: () => void }) => (
    <div data-testid="single-prediction">
      <button onClick={onBack}>Back</button>
    </div>
  ),
}));

jest.mock("../RangePrediction", () => ({
  __esModule: true,
  default: ({ onBack }: { onBack: () => void }) => (
    <div data-testid="range-prediction">
      <button onClick={onBack}>Back</button>
    </div>
  ),
}));

jest.mock("../ComparisonPrediction", () => ({
  __esModule: true,
  default: ({ onBack }: { onBack: () => void }) => (
    <div data-testid="comparison-prediction">
      <button onClick={onBack}>Back</button>
    </div>
  ),
}));

jest.mock("../RangeComparisonPrediction", () => ({
  RangeComparisonPrediction: ({ onBack }: { onBack: () => void }) => (
    <div data-testid="range-comparison-prediction">
      <button onClick={onBack}>Back</button>
    </div>
  ),
}));

describe("Home", () => {
  it("renders the welcome message", () => {
    render(<Home />);
    expect(
      screen.getByText("Welcome to Used Car Price Prediction System")
    ).toBeInTheDocument();
  });

  it("renders all four option buttons", () => {
    render(<Home />);

    expect(screen.getByText("Single Prediction")).toBeInTheDocument();
    expect(screen.getByText("Range Prediction")).toBeInTheDocument();
    expect(screen.getByText("Car Comparison")).toBeInTheDocument();
    expect(screen.getByText("Range Comparison")).toBeInTheDocument();
  });

  it("navigates to Single Prediction when button is clicked", () => {
    render(<Home />);

    const singlePredictionButton = screen.getByText("Single Prediction");
    fireEvent.click(singlePredictionButton);

    expect(screen.getByTestId("single-prediction")).toBeInTheDocument();
    expect(
      screen.queryByText("Welcome to Used Car Price Prediction System")
    ).not.toBeInTheDocument();
  });

  it("navigates to Range Prediction when button is clicked", () => {
    render(<Home />);

    const rangePredictionButton = screen.getByText("Range Prediction");
    fireEvent.click(rangePredictionButton);

    expect(screen.getByTestId("range-prediction")).toBeInTheDocument();
    expect(
      screen.queryByText("Welcome to Used Car Price Prediction System")
    ).not.toBeInTheDocument();
  });

  it("navigates to Car Comparison when button is clicked", () => {
    render(<Home />);

    const comparisonButton = screen.getByText("Car Comparison");
    fireEvent.click(comparisonButton);

    expect(screen.getByTestId("comparison-prediction")).toBeInTheDocument();
    expect(
      screen.queryByText("Welcome to Used Car Price Prediction System")
    ).not.toBeInTheDocument();
  });

  it("navigates to Range Comparison when button is clicked", () => {
    render(<Home />);

    const rangeComparisonButton = screen.getByText("Range Comparison");
    fireEvent.click(rangeComparisonButton);

    expect(
      screen.getByTestId("range-comparison-prediction")
    ).toBeInTheDocument();
    expect(
      screen.queryByText("Welcome to Used Car Price Prediction System")
    ).not.toBeInTheDocument();
  });

  it("returns to home view when back button is clicked from Single Prediction", () => {
    render(<Home />);

    fireEvent.click(screen.getByText("Single Prediction"));
    expect(screen.getByTestId("single-prediction")).toBeInTheDocument();

    fireEvent.click(screen.getByText("Back"));

    expect(
      screen.getByText("Welcome to Used Car Price Prediction System")
    ).toBeInTheDocument();
    expect(screen.queryByTestId("single-prediction")).not.toBeInTheDocument();
  });

  it("returns to home view when back button is clicked from Range Prediction", () => {
    render(<Home />);

    fireEvent.click(screen.getByText("Range Prediction"));
    expect(screen.getByTestId("range-prediction")).toBeInTheDocument();

    fireEvent.click(screen.getByText("Back"));

    expect(
      screen.getByText("Welcome to Used Car Price Prediction System")
    ).toBeInTheDocument();
  });

  it("returns to home view when back button is clicked from Car Comparison", () => {
    render(<Home />);

    fireEvent.click(screen.getByText("Car Comparison"));
    expect(screen.getByTestId("comparison-prediction")).toBeInTheDocument();

    fireEvent.click(screen.getByText("Back"));

    expect(
      screen.getByText("Welcome to Used Car Price Prediction System")
    ).toBeInTheDocument();
  });

  it("returns to home view when back button is clicked from Range Comparison", () => {
    render(<Home />);

    fireEvent.click(screen.getByText("Range Comparison"));
    expect(
      screen.getByTestId("range-comparison-prediction")
    ).toBeInTheDocument();

    fireEvent.click(screen.getByText("Back"));

    expect(
      screen.getByText("Welcome to Used Car Price Prediction System")
    ).toBeInTheDocument();
  });

  it("applies correct styling classes to main container", () => {
    const { container } = render(<Home />);

    const mainDiv = container.firstChild as HTMLElement;
    expect(mainDiv).toHaveClass("min-h-screen");
    expect(mainDiv).toHaveClass("flex");
    expect(mainDiv).toHaveClass("items-center");
    expect(mainDiv).toHaveClass("justify-center");
  });

  it("renders buttons in a grid layout", () => {
    render(<Home />);

    const buttons = screen.getAllByRole("button");
    expect(buttons).toHaveLength(4);
  });
});
