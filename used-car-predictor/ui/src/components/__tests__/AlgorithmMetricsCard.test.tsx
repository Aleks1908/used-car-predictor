import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { AlgorithmMetricsCard } from "../AlgorithmMetricsCard";

describe("AlgorithmMetricsCard", () => {
  const mockMetrics = {
    linear: {
      metrics: { mse: 100.5, mae: 50.2, r2: 0.85 },
      timing: { meanTrialMs: 10.5, trials: 5, totalMs: 52.5 },
    },
    ridge: {
      metrics: { mse: 95.3, mae: 48.1, r2: 0.87 },
      timing: { meanTrialMs: 12.3, trials: 5, totalMs: 61.5 },
    },
    ridge_rf: {
      metrics: { mse: 88.7, mae: 45.6, r2: 0.89 },
      timing: { meanTrialMs: 25.8, trials: 5, totalMs: 129.0 },
    },
    ridge_gb: {
      metrics: { mse: 82.1, mae: 42.3, r2: 0.91 },
      timing: { meanTrialMs: 30.2, trials: 5, totalMs: 151.0 },
    },
  };

  it("renders the component with title", () => {
    render(<AlgorithmMetricsCard metrics={mockMetrics} />);
    expect(
      screen.getByText("Algorithm Performance Metrics")
    ).toBeInTheDocument();
  });

  it("displays all algorithm names", () => {
    render(<AlgorithmMetricsCard metrics={mockMetrics} />);
    expect(screen.getByText("Linear Regression")).toBeInTheDocument();
    expect(screen.getByText("Ridge Regression")).toBeInTheDocument();
    expect(screen.getByText("Random Forest")).toBeInTheDocument();
    expect(screen.getByText("Gradient Boosting")).toBeInTheDocument();
  });

  it("displays metrics correctly", () => {
    render(<AlgorithmMetricsCard metrics={mockMetrics} />);
    expect(screen.getByText("100.50")).toBeInTheDocument(); // Linear MSE
    expect(screen.getByText("0.850")).toBeInTheDocument(); // Linear R2 (3 decimal places)
  });
});
