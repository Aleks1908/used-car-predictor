import { render, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { MemoryRouter } from "react-router-dom";
import App from "../App";

jest.mock("../pages/Home", () => ({
  __esModule: true,
  default: () => <div data-testid="home-page">Home Page</div>,
}));

describe("App", () => {
  const renderApp = () => {
    return render(
      <MemoryRouter initialEntries={["/"]}>
        <App />
      </MemoryRouter>
    );
  };

  it("renders without crashing", () => {
    renderApp();
    expect(screen.getByTestId("home-page")).toBeInTheDocument();
  });

  it("has proper container styling", () => {
    const { container } = renderApp();
    const mainDiv = container.querySelector(".min-h-screen");
    expect(mainDiv).toBeInTheDocument();
  });

  it("renders Routes component", () => {
    renderApp();
    expect(screen.getByTestId("home-page")).toBeInTheDocument();
  });

  it("renders Home component on root path", () => {
    renderApp();
    expect(screen.getByTestId("home-page")).toBeInTheDocument();
  });
});
