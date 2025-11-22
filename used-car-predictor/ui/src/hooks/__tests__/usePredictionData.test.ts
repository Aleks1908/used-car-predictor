import { renderHook, waitFor, act } from "@testing-library/react";
import "@testing-library/jest-dom";
import { usePredictionData } from "../usePredictionData";

global.fetch = jest.fn();

describe("usePredictionData", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.resetAllMocks();
  });

  it("initializes with correct default values", () => {
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => [],
    });

    const { result } = renderHook(() => usePredictionData());

    expect(result.current.manufacturers).toBeNull();
    expect(result.current.models).toBeNull();
    expect(result.current.selectedManufacturer).toBe("");
    expect(result.current.selectedModel).toBe("");
    expect(result.current.details).toBeNull();
    expect(result.current.selectedFuel).toBe("");
    expect(result.current.selectedTransmission).toBe("");
    expect(result.current.error).toBeNull();
  });

  it("loads manufacturers on mount", async () => {
    const mockManufacturers = ["toyota", "honda", "ford"];
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockManufacturers,
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    expect(result.current.manufacturers).toEqual([
      { value: "toyota", label: "toyota" },
      { value: "honda", label: "honda" },
      { value: "ford", label: "ford" },
    ]);
  });

  it("handles manufacturers fetch error", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();
    (global.fetch as jest.Mock).mockRejectedValueOnce(
      new Error("Network error")
    );

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.error).toBe("Network error");
    });

    consoleErrorSpy.mockRestore();
  });

  it("loads models when manufacturer is selected", async () => {
    const mockManufacturers = ["toyota"];
    const mockModels = ["camry", "corolla"];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockManufacturers,
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockModels,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    expect(result.current.models).toEqual([
      { value: "camry", label: "camry" },
      { value: "corolla", label: "corolla" },
    ]);
  });

  it("resets model and details when manufacturer changes", async () => {
    (global.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => [],
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
      result.current.setSelectedModel("camry");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("honda");
    });

    expect(result.current.selectedModel).toBe("");
    expect(result.current.details).toBeNull();
    expect(result.current.selectedFuel).toBe("");
    expect(result.current.selectedTransmission).toBe("");
  });

  it("loads details when model is selected", async () => {
    const mockDetails = {
      fuels: [{ value: "petrol", label: "Petrol" }],
      transmissions: [{ value: "automatic", label: "Automatic" }],
      minYear: 2010,
      maxYear: 2024,
      anchorTargetYear: 2024,
    };

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockDetails,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedModel("camry");
    });

    await waitFor(() => {
      expect(result.current.details).not.toBeNull();
    });

    expect(result.current.details).toEqual(mockDetails);
  });

  it("handles model details fetch error", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry"],
      })
      .mockRejectedValueOnce(new Error("Details fetch failed"));

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    result.current.setSelectedManufacturer("toyota");

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    result.current.setSelectedModel("camry");

    await waitFor(() => {
      expect(result.current.error).toBe("Details fetch failed");
    });

    consoleErrorSpy.mockRestore();
  });

  it("updates selected fuel and transmission", async () => {
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => [],
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    act(() => {
      result.current.setSelectedFuel("petrol");
      result.current.setSelectedTransmission("automatic");
    });

    expect(result.current.selectedFuel).toBe("petrol");
    expect(result.current.selectedTransmission).toBe("automatic");
  });

  it("handles HTTP error responses", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.error).toBe("HTTP 404");
    });

    consoleErrorSpy.mockRestore();
  });

  it("clears models and details when manufacturer is cleared", async () => {
    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry"],
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("");
    });

    expect(result.current.models).toBeNull();
    expect(result.current.selectedModel).toBe("");
    expect(result.current.details).toBeNull();
  });

  it("handles manufacturers with object format containing value and label", async () => {
    const mockManufacturers = [
      { value: "toyota", label: "Toyota" },
      { value: "honda", label: "Honda" },
    ];
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockManufacturers,
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    expect(result.current.manufacturers).toEqual([
      { value: "toyota", label: "Toyota" },
      { value: "honda", label: "Honda" },
    ]);
  });

  it("handles manufacturers with object format using name property", async () => {
    const mockManufacturers = [
      { name: "toyota" },
      { manufacturer: "honda" },
      { id: "ford" },
    ];
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockManufacturers,
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    expect(result.current.manufacturers).toEqual([
      { value: "toyota", label: "toyota" },
      { value: "honda", label: "honda" },
      { value: "ford", label: "ford" },
    ]);
  });

  it("handles manufacturers with null or undefined properties", async () => {
    const mockManufacturers = [{ name: null }, { manufacturer: undefined }, {}];
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => mockManufacturers,
    });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    expect(result.current.manufacturers).toEqual([
      { value: "", label: "" },
      { value: "", label: "" },
      { value: "", label: "" },
    ]);
  });

  it("handles models with object format containing value and label", async () => {
    const mockModels = [
      { value: "camry", label: "Camry" },
      { value: "corolla", label: "Corolla" },
    ];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockModels,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    expect(result.current.models).toEqual([
      { value: "camry", label: "Camry" },
      { value: "corolla", label: "Corolla" },
    ]);
  });

  it("handles models with object format using name property", async () => {
    const mockModels = [
      { name: "camry" },
      { model: "corolla" },
      { id: "prius" },
    ];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockModels,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    expect(result.current.models).toEqual([
      { value: "camry", label: "camry" },
      { value: "corolla", label: "corolla" },
      { value: "prius", label: "prius" },
    ]);
  });

  it("handles models with null or undefined properties", async () => {
    const mockModels = [{ name: null }, { model: undefined }, {}];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockModels,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    expect(result.current.models).toEqual([
      { value: "", label: "" },
      { value: "", label: "" },
      { value: "", label: "" },
    ]);
  });

  it("handles HTTP error when fetching models", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.error).toBe("HTTP 500");
    });

    consoleErrorSpy.mockRestore();
  });

  it("handles network error when fetching models", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockRejectedValueOnce(new Error("Network error"));

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.error).toBe("Network error");
    });

    consoleErrorSpy.mockRestore();
  });

  it("handles HTTP error when fetching details", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry"],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedModel("camry");
    });

    await waitFor(() => {
      expect(result.current.error).toBe("HTTP 403");
    });

    consoleErrorSpy.mockRestore();
  });

  it("handles non-Error exceptions gracefully", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock).mockRejectedValueOnce("String error");

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.error).toBe("An error occurred");
    });

    consoleErrorSpy.mockRestore();
  });

  it("clears details and selections when model is cleared", async () => {
    const mockDetails = {
      fuels: [{ value: "petrol", label: "Petrol" }],
      transmissions: [{ value: "automatic", label: "Automatic" }],
      minYear: 2010,
      maxYear: 2024,
    };

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockDetails,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedModel("camry");
    });

    await waitFor(() => {
      expect(result.current.details).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedFuel("petrol");
      result.current.setSelectedTransmission("automatic");
    });

    expect(result.current.selectedFuel).toBe("petrol");
    expect(result.current.selectedTransmission).toBe("automatic");

    await act(async () => {
      result.current.setSelectedModel("");
    });

    expect(result.current.details).toBeNull();
    expect(result.current.selectedFuel).toBe("");
    expect(result.current.selectedTransmission).toBe("");
  });

  it("clears error when models fetch succeeds after previous error", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry", "corolla"],
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.error).toBe("HTTP 500");
    });

    await act(async () => {
      result.current.setSelectedManufacturer("honda");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    expect(result.current.error).toBeNull();
    expect(result.current.models).toEqual([
      { value: "camry", label: "camry" },
      { value: "corolla", label: "corolla" },
    ]);

    consoleErrorSpy.mockRestore();
  });

  it("clears error when details fetch succeeds after previous error", async () => {
    const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation();

    const mockDetails = {
      fuels: [{ value: "petrol", label: "Petrol" }],
      transmissions: [{ value: "automatic", label: "Automatic" }],
      minYear: 2010,
      maxYear: 2024,
    };

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["toyota"],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ["camry", "corolla"],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockDetails,
      });

    const { result } = renderHook(() => usePredictionData());

    await waitFor(() => {
      expect(result.current.manufacturers).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedManufacturer("toyota");
    });

    await waitFor(() => {
      expect(result.current.models).not.toBeNull();
    });

    await act(async () => {
      result.current.setSelectedModel("camry");
    });

    await waitFor(() => {
      expect(result.current.error).toBe("HTTP 500");
    });

    await act(async () => {
      result.current.setSelectedModel("corolla");
    });

    await waitFor(() => {
      expect(result.current.details).not.toBeNull();
    });

    expect(result.current.error).toBeNull();
    expect(result.current.details).toEqual(mockDetails);

    consoleErrorSpy.mockRestore();
  });
});
