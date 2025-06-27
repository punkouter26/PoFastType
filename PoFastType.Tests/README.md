# PoFastType Test Project

This test project is organized into 4 distinct test types, each serving a specific purpose in ensuring the application's quality and reliability.

## Test Organization

### 1. **Unit Tests** (`/Unit/`)
Tests for individual components in isolation using mocks and stubs.

- **Services/**: Tests for business logic services
  - `TextGenerationServiceTests.cs` - Tests TextGenerationService using Strategy pattern
  - `HardcodedTextStrategyTests.cs` - Tests concrete strategy implementation
  
- **Repositories/**: Tests for data access components
  - `AzureTableGameResultRepositoryTests.cs` - Tests repository pattern implementation

### 2. **Integration Tests** (`/Integration/`)
Tests for component interactions and data flow between layers.

- **Services/**: Tests for service layer integration
  - `GameServiceTests.cs` - Tests business logic with repository integration

### 3. **API Tests** (`/API/`)
Tests for HTTP API endpoints and web layer functionality.

- **Controllers/**: Tests for REST API endpoints
  - `GameControllerTests.cs` - Tests game-related API endpoints
  - `ScoresControllerTests.cs` - Tests score and leaderboard API endpoints

### 4. **System Tests** (`/System/`)
End-to-end tests that verify complete application workflows.

- `TypingGameSystemTests.cs` - Tests complete user workflows from text generation to leaderboard

## Test Helpers (`/TestHelpers/`)
Shared utilities and test infrastructure.

- `CustomWebApplicationFactory.cs` - Test factory using Factory Pattern (GoF) for creating test instances

## Design Patterns Used

- **Factory Pattern**: `CustomWebApplicationFactory` for test instance creation
- **Repository Pattern**: Testing data access abstraction
- **Strategy Pattern**: Testing text generation strategies
- **Dependency Injection**: Using mocks for dependencies

## SOLID Principles

- **Single Responsibility**: Each test class tests one specific component
- **Open/Closed**: Test structure allows easy addition of new test types
- **Dependency Inversion**: Tests depend on abstractions (interfaces) not concretions

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration" 
dotnet test --filter "Category=API"
dotnet test --filter "Category=System"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage Goals

- **Unit Tests**: 90%+ coverage for business logic
- **Integration Tests**: Key component interactions
- **API Tests**: All HTTP endpoints and status codes
- **System Tests**: Critical user workflows
