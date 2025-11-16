import { formatCarName } from "../formatting";

describe("Formatting Utils", () => {
  describe("formatCarName", () => {
    it("formats car name with manufacturer and model", () => {
      expect(formatCarName("toyota", "camry")).toBe("Toyota Camry");
    });

    it("capitalizes first letter of each part", () => {
      expect(formatCarName("honda", "civic")).toBe("Honda Civic");
    });

    it("returns empty string when manufacturer is missing", () => {
      expect(formatCarName(undefined, "civic")).toBe("");
    });

    it("returns empty string when model is missing", () => {
      expect(formatCarName("honda", undefined)).toBe("");
    });

    it("returns empty string when both are missing", () => {
      expect(formatCarName()).toBe("");
    });
  });
});
