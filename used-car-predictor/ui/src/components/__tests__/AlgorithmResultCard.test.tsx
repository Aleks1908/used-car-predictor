import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { AlgorithmResultCard } from "../AlgorithmResultCard";

describe("AlgorithmResultCard", () => {
  const mockResult = {
    algorithm: "linear",
    predictedPrice: 25000,
  };

  it("renders the component with algorithm name", () => {
    render(<AlgorithmResultCard result={mockResult} />);
    expect(screen.getByText("Linear Regression")).toBeInTheDocument();
  });

  it("displays the predicted price formatted with locale string", () => {
    render(<AlgorithmResultCard result={mockResult} />);
    expect(screen.getByText(/\$.*25.*000/)).toBeInTheDocument();
  });

  it("shows the predicted price label", () => {
    render(<AlgorithmResultCard result={mockResult} />);
    expect(screen.getByText("Predicted Price")).toBeInTheDocument();
  });

  it("renders Ridge Regression algorithm name correctly", () => {
    const ridgeResult = {
      algorithm: "ridge",
      predictedPrice: 30000,
    };
    render(<AlgorithmResultCard result={ridgeResult} />);
    expect(screen.getByText("Ridge Regression")).toBeInTheDocument();
  });

  it("renders Random Forest algorithm name correctly", () => {
    const rfResult = {
      algorithm: "ridge_rf",
      predictedPrice: 28000,
    };
    render(<AlgorithmResultCard result={rfResult} />);
    expect(screen.getByText("Random Forest")).toBeInTheDocument();
  });

  it("renders Gradient Boosting algorithm name correctly", () => {
    const gbResult = {
      algorithm: "ridge_gb",
      predictedPrice: 27500,
    };
    render(<AlgorithmResultCard result={gbResult} />);
    expect(screen.getByText("Gradient Boosting")).toBeInTheDocument();
  });

  it("displays unknown algorithm name as fallback", () => {
    const unknownResult = {
      algorithm: "unknown_algo",
      predictedPrice: 20000,
    };
    render(<AlgorithmResultCard result={unknownResult} />);
    expect(screen.getByText("unknown_algo")).toBeInTheDocument();
  });

  it("formats large prices correctly", () => {
    const expensiveResult = {
      algorithm: "linear",
      predictedPrice: 1234567,
    };
    render(<AlgorithmResultCard result={expensiveResult} />);
    expect(screen.getByText(/\$.*1.*234.*567/)).toBeInTheDocument();
  });

  it("formats small prices correctly", () => {
    const cheapResult = {
      algorithm: "linear",
      predictedPrice: 999,
    };
    render(<AlgorithmResultCard result={cheapResult} />);
    expect(screen.getByText("$999")).toBeInTheDocument();
  });
});
