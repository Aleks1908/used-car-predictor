interface Details {
  minYear?: number;
  maxYear?: number;
}

export function validateYears(
  yearOfProduction: string,
  targetYear: string,
  details: Details | null
): string | null {
  const prodYear = parseInt(yearOfProduction);
  const tgtYear = parseInt(targetYear);

  if (details?.minYear && details?.maxYear) {
    if (prodYear < details.minYear || prodYear > details.maxYear) {
      return `Year of production must be between ${details.minYear} and ${details.maxYear}`;
    }
    if (tgtYear < details.minYear) {
      return `Target year must be at least ${details.minYear}`;
    }
  }

  if (tgtYear < prodYear) {
    return "Target year must be greater than or equal to year of production";
  }

  return null;
}

export function validateYearRange(
  yearOfProduction: string,
  startYear: string,
  endYear: string,
  details: Details | null
): string | null {
  const prodYear = parseInt(yearOfProduction);
  const start = parseInt(startYear);
  const end = parseInt(endYear);

  if (details?.minYear && details?.maxYear) {
    if (prodYear < details.minYear || prodYear > details.maxYear) {
      return `Year of production must be between ${details.minYear} and ${details.maxYear}`;
    }
    if (start < details.minYear) {
      return `Start year must be at least ${details.minYear}`;
    }
    if (end < details.minYear) {
      return `End year must be at least ${details.minYear}`;
    }
  }

  if (start < prodYear) {
    return "Start year must be greater than or equal to year of production";
  }

  if (end < start) {
    return "End year must be greater than or equal to start year";
  }

  return null;
}
