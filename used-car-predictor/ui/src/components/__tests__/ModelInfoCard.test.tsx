import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { ModelInfoCard } from "../ModelInfoCard";

describe("ModelInfoCard", () => {
  const mockModelInfo = {
    trainedAt: "2024-11-16T10:00:00Z",
    anchorTargetYear: 2024,
    totalRows: 5000,
    mse: 100.5,
    mae: 50.2,
    r2: 0.85,
  };

  const mockCarDetails = {
    yearOfProduction: 2020,
    transmission: "automatic",
    fuelType: "petrol",
    mileageKm: 50000,
  };

  it("renders car name and title", () => {
    render(<ModelInfoCard carName="Toyota Camry" modelInfo={mockModelInfo} />);

    expect(screen.getByText("Toyota Camry - Model Info")).toBeInTheDocument();
  });

  it("renders model information correctly", () => {
    render(<ModelInfoCard carName="Honda Civic" modelInfo={mockModelInfo} />);

    expect(screen.getByText("2024")).toBeInTheDocument();
    expect(screen.getByText("5000 rows")).toBeInTheDocument();
  });

  it("renders car details when provided", () => {
    render(
      <ModelInfoCard
        carName="Toyota Camry"
        modelInfo={mockModelInfo}
        carDetails={mockCarDetails}
      />
    );

    expect(screen.getByText("2020")).toBeInTheDocument();
    expect(screen.getByText("Automatic")).toBeInTheDocument();
    expect(screen.getByText("Petrol")).toBeInTheDocument();
    expect(screen.getByText(/50\s?000 km/)).toBeInTheDocument();
  });

  it("does not render car details when not provided", () => {
    render(<ModelInfoCard carName="Toyota Camry" modelInfo={mockModelInfo} />);

    expect(screen.queryByText("Year of Production:")).not.toBeInTheDocument();
    expect(screen.queryByText("Transmission:")).not.toBeInTheDocument();
  });

  it("renders metrics when provided", () => {
    render(<ModelInfoCard carName="Toyota Camry" modelInfo={mockModelInfo} />);

    expect(screen.getByText("100.50")).toBeInTheDocument();
    expect(screen.getByText("50.20")).toBeInTheDocument();
    expect(screen.getByText("0.850")).toBeInTheDocument();
  });

  it("does not render metrics when not provided", () => {
    const modelInfoWithoutMetrics = {
      trainedAt: "2024-11-16T10:00:00Z",
      anchorTargetYear: 2024,
      totalRows: 5000,
    };

    render(
      <ModelInfoCard
        carName="Toyota Camry"
        modelInfo={modelInfoWithoutMetrics}
      />
    );

    expect(screen.queryByText("MSE:")).not.toBeInTheDocument();
    expect(screen.queryByText("MAE:")).not.toBeInTheDocument();
    expect(screen.queryByText("R²:")).not.toBeInTheDocument();
  });

  it("formats date correctly", () => {
    render(<ModelInfoCard carName="Toyota Camry" modelInfo={mockModelInfo} />);

    const formattedDate = new Date("2024-11-16T10:00:00Z").toLocaleDateString();
    expect(screen.getByText(formattedDate)).toBeInTheDocument();
  });

  it("capitalizes transmission and fuel type", () => {
    const carDetailsLowercase = {
      yearOfProduction: 2020,
      transmission: "manual",
      fuelType: "diesel",
      mileageKm: 30000,
    };

    render(
      <ModelInfoCard
        carName="Toyota Camry"
        modelInfo={mockModelInfo}
        carDetails={carDetailsLowercase}
      />
    );

    expect(screen.getByText("Manual")).toBeInTheDocument();
    expect(screen.getByText("Diesel")).toBeInTheDocument();
  });
});
