import { validateYears, validateYearRange } from "../validation";

describe("validateYears", () => {
  const mockDetails = {
    minYear: 2020,
    maxYear: 2025,
  };

  describe("year of production validation", () => {
    it("returns error when year of production is below minimum", () => {
      const result = validateYears("2019", "2024", mockDetails);
      expect(result).toBe("Year of production must be between 2020 and 2025");
    });

    it("returns error when year of production is above maximum", () => {
      const result = validateYears("2026", "2027", mockDetails);
      expect(result).toBe("Year of production must be between 2020 and 2025");
    });

    it("accepts valid year of production within range", () => {
      const result = validateYears("2022", "2024", mockDetails);
      expect(result).toBeNull();
    });
  });

  describe("target year validation", () => {
    it("returns error when target year is below minimum", () => {
      const result = validateYears("2022", "2019", mockDetails);
      expect(result).toBe("Target year must be at least 2020");
    });

    it("returns error when target year is less than year of production", () => {
      const result = validateYears("2024", "2023", mockDetails);
      expect(result).toBe(
        "Target year must be greater than or equal to year of production"
      );
    });

    it("accepts target year equal to year of production", () => {
      const result = validateYears("2022", "2022", mockDetails);
      expect(result).toBeNull();
    });

    it("accepts target year greater than year of production", () => {
      const result = validateYears("2022", "2024", mockDetails);
      expect(result).toBeNull();
    });
  });

  describe("null details handling", () => {
    it("only validates year relationship when details is null", () => {
      const result = validateYears("2022", "2024", null);
      expect(result).toBeNull();
    });

    it("returns error for invalid year relationship even with null details", () => {
      const result = validateYears("2024", "2022", null);
      expect(result).toBe(
        "Target year must be greater than or equal to year of production"
      );
    });
  });

  describe("missing minYear or maxYear", () => {
    it("skips range validation when minYear is missing", () => {
      const result = validateYears("2019", "2024", { maxYear: 2025 });
      expect(result).toBeNull();
    });

    it("skips range validation when maxYear is missing", () => {
      const result = validateYears("2026", "2027", { minYear: 2020 });
      expect(result).toBeNull();
    });
  });
});

describe("validateYearRange", () => {
  const mockDetails = {
    minYear: 2020,
    maxYear: 2025,
  };

  describe("year of production validation", () => {
    it("returns error when year of production is below minimum", () => {
      const result = validateYearRange("2019", "2022", "2024", mockDetails);
      expect(result).toBe("Year of production must be between 2020 and 2025");
    });

    it("returns error when year of production is above maximum", () => {
      const result = validateYearRange("2026", "2027", "2028", mockDetails);
      expect(result).toBe("Year of production must be between 2020 and 2025");
    });

    it("accepts valid year of production within range", () => {
      const result = validateYearRange("2022", "2023", "2024", mockDetails);
      expect(result).toBeNull();
    });
  });

  describe("start year validation", () => {
    it("returns error when start year is below minimum", () => {
      const result = validateYearRange("2022", "2019", "2024", mockDetails);
      expect(result).toBe("Start year must be at least 2020");
    });

    it("returns error when start year is less than year of production", () => {
      const result = validateYearRange("2024", "2023", "2025", mockDetails);
      expect(result).toBe(
        "Start year must be greater than or equal to year of production"
      );
    });

    it("accepts start year equal to year of production", () => {
      const result = validateYearRange("2022", "2022", "2024", mockDetails);
      expect(result).toBeNull();
    });

    it("accepts start year greater than year of production", () => {
      const result = validateYearRange("2022", "2023", "2024", mockDetails);
      expect(result).toBeNull();
    });
  });

  describe("end year validation", () => {
    it("returns error when end year is below minimum", () => {
      const result = validateYearRange("2022", "2023", "2019", mockDetails);
      expect(result).toBe("End year must be at least 2020");
    });

    it("returns error when end year is less than start year", () => {
      const result = validateYearRange("2022", "2024", "2023", mockDetails);
      expect(result).toBe(
        "End year must be greater than or equal to start year"
      );
    });

    it("accepts end year equal to start year", () => {
      const result = validateYearRange("2022", "2023", "2023", mockDetails);
      expect(result).toBeNull();
    });

    it("accepts end year greater than start year", () => {
      const result = validateYearRange("2022", "2023", "2025", mockDetails);
      expect(result).toBeNull();
    });
  });

  describe("null details handling", () => {
    it("only validates year relationships when details is null", () => {
      const result = validateYearRange("2022", "2023", "2025", null);
      expect(result).toBeNull();
    });

    it("returns error for invalid start year relationship with null details", () => {
      const result = validateYearRange("2024", "2022", "2025", null);
      expect(result).toBe(
        "Start year must be greater than or equal to year of production"
      );
    });

    it("returns error for invalid end year relationship with null details", () => {
      const result = validateYearRange("2022", "2025", "2023", null);
      expect(result).toBe(
        "End year must be greater than or equal to start year"
      );
    });
  });

  describe("missing minYear or maxYear", () => {
    it("skips range validation when minYear is missing", () => {
      const result = validateYearRange("2019", "2020", "2024", {
        maxYear: 2025,
      });
      expect(result).toBeNull();
    });

    it("skips range validation when maxYear is missing", () => {
      const result = validateYearRange("2026", "2027", "2028", {
        minYear: 2020,
      });
      expect(result).toBeNull();
    });
  });

  describe("complex scenarios", () => {
    it("validates all conditions in correct order", () => {
      // First check: year of production out of range
      const result1 = validateYearRange("2019", "2020", "2024", mockDetails);
      expect(result1).toBe("Year of production must be between 2020 and 2025");

      // Second check: start year below minimum (when prod year is valid)
      const result2 = validateYearRange("2022", "2019", "2024", mockDetails);
      expect(result2).toBe("Start year must be at least 2020");

      // Third check: end year below minimum (when prod year and start year are valid)
      const result3 = validateYearRange("2022", "2023", "2019", mockDetails);
      expect(result3).toBe("End year must be at least 2020");

      // Fourth check: start year less than prod year (when all years are in valid range)
      const result4 = validateYearRange("2024", "2023", "2025", mockDetails);
      expect(result4).toBe(
        "Start year must be greater than or equal to year of production"
      );

      // Fifth check: end year less than start year (when all previous checks pass)
      const result5 = validateYearRange("2022", "2024", "2023", mockDetails);
      expect(result5).toBe(
        "End year must be greater than or equal to start year"
      );

      // All valid
      const result6 = validateYearRange("2022", "2023", "2024", mockDetails);
      expect(result6).toBeNull();
    });
  });
});
