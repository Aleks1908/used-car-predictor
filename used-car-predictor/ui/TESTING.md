# Testing Setup

This project uses Jest and React Testing Library for unit and integration testing.

## Running Tests

```bash
# Run all tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage report
npm run test:coverage
```

## Test Structure

Tests are located in `__tests__` directories next to the code they test:

```
src/
  components/
    __tests__/
      AlgorithmMetricsCard.test.tsx
  utils/
    __tests__/
      formatting.test.ts
```

## Writing Tests

### Component Tests

```typescript
import { render, screen } from "@testing-library/react";
import { MyComponent } from "../MyComponent";

describe("MyComponent", () => {
  it("renders correctly", () => {
    render(<MyComponent />);
    expect(screen.getByText("Hello")).toBeInTheDocument();
  });
});
```

### Utility Function Tests

```typescript
import { myFunction } from "../myFunction";

describe("myFunction", () => {
  it("returns expected value", () => {
    expect(myFunction("input")).toBe("output");
  });
});
```

## Configuration Files

- **jest.config.ts** - Main Jest configuration
- **jest.setup.ts** - Test environment setup (imports @testing-library/jest-dom)
- **tsconfig.test.json** - TypeScript configuration for tests

## Dependencies

- **jest** - Test framework
- **@testing-library/react** - React component testing utilities
- **@testing-library/jest-dom** - Custom Jest matchers for DOM
- **@testing-library/user-event** - User interaction simulation
- **ts-jest** - TypeScript support for Jest
- **jest-environment-jsdom** - Browser-like environment for tests
