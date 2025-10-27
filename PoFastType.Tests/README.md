# PoFastType Test Project

This test project contains comprehensive test coverage organized into 4 test layers, with a total of **96 automated tests** achieving **31.34% line coverage** (442/1410 lines).

---

## 📊 Coverage Summary

**Latest Coverage Report (Phase 3 Completion)**

| Metric | Coverage | Lines |
|--------|----------|-------|
| **Line Coverage** | **31.34%** | 442/1410 |
| **Branch Coverage** | **24.84%** | 80/322 |
| **Total Tests** | **96** | All passing ✅ |

### Coverage by Component

| Component | Line Coverage | Status |
|-----------|---------------|--------|
| **Program.cs** | 75.89% | ✅ Good |
| **DiagController** | 65.21% | ✅ Improved from 0% |
| **GameService** | 100% | ✅ Excellent |
| **TextGenerationService** | 100% | ✅ Excellent |
| **HardcodedTextStrategy** | 100% | ✅ Excellent |
| **AzureTableGameResultRepository** | 100% | ✅ Improved from 40.74% |
| **ScoresController** | 80.76% | ✅ Good |
| **GameController** | 73.91% | ✅ Good |
| **GlobalExceptionMiddleware** | 35.71% | ⚠️ Error paths untested |
| **UserIdentityService** | 23.52% | ⚠️ Not in scope |
| **AzureTableStorageHealthCheck** | 0% | ⚠️ Not in scope |

**Key Achievements:**
- ✅ **DiagController**: Increased from 0% to 65.21% with 18 comprehensive tests
- ✅ **AzureTableGameResultRepository**: Increased from 40.74% to 100% with 20 unit tests + 10 integration tests
- ✅ **Business Logic**: GameService, TextGenerationService, and HardcodedTextStrategy all at 100% coverage

---

## 🗂️ Test Organization

### 1. **Unit Tests** (`/Unit/`) - 60+ tests
Tests for individual components in isolation using mocks and stubs.

- **Services/**: Business logic services
  - `TextGenerationServiceTests.cs` - 15 tests for TextGenerationService
  - `HardcodedTextStrategyTests.cs` - 8 tests for concrete strategy implementation
  
- **Repositories/**: Data access components
  - `AzureTableGameResultRepositoryTests.cs` - **20 tests** (13 original + 7 new)
    - Constructor validation and null checks
    - AddAsync default timestamp and RowKey generation
    - GetUserResultsAsync with/without results
    - GetTopResultsAsync count validation
    - ExistsAsync true/false scenarios
    - Property preservation validation

### 2. **Integration Tests** (`/Integration/`) - 10 tests
Tests for component interactions with actual infrastructure (Azurite).

- `AzureTableStorageIntegrationTests.cs` - **10 comprehensive tests**
  - AddAsync successful entity creation
  - Unique RowKey generation for multiple results
  - GetUserResultsAsync with data and empty results
  - GetTopResultsAsync ordering by CompositeScore
  - GetTopResultsAsync count limits (3 of 5)
  - ExistsAsync true/false validation
  - JSON data preservation (ProblemKeysJson)
  - Property mapping validation (all 9 properties)

### 3. **API Tests** (`/API/`) - 18+ tests
Tests for HTTP API endpoints and controllers.

- **Controllers/**:
  - `GameControllerTests.cs` - Game-related API endpoints
  - `ScoresControllerTests.cs` - Score and leaderboard endpoints
  - `DiagControllerTests.cs` - **18 diagnostic health check tests** (NEW)
    - Health endpoint returns OK status
    - JSON response with correct content type
    - Internet connectivity check (SSL validation)
    - Azure connectivity check (management.azure.com)
    - Health check endpoint validation
    - Backend API check
    - Azure Table Storage check (Azurite)
    - Azure OpenAI check (posharedopenai.openai.azure.com)
    - Performance validation (15s timeout)
    - Idempotent multiple calls
    - Exception handling resilience
    - Response structure schema validation

### 4. **System Tests** (`/System/`) - 8+ tests
End-to-end tests for complete application workflows.

- `TypingGameSystemTests.cs` - Complete user workflows

### 5. **E2E Tests** (`/E2E/`) - 36 tests (Playwright)
Automated browser tests for UI validation across desktop (1920x1080) and mobile (390x844) viewports.

- `HomePageE2ETests.cs` - **10 tests**
  - Desktop/mobile page load success
  - Typing text area display
  - Start button presence
  - Navigation functionality
  - Responsive layout (no horizontal scroll)
  - Console error monitoring
  - Footer display
  - Blazor hydration validation

- `LeaderboardPageE2ETests.cs` - **10 tests**
  - Desktop/mobile page load
  - Table/list display detection
  - Score columns (WPM, Score, Accuracy)
  - Table headers visibility
  - Responsive mobile layout
  - Navigation to/from home
  - Font size readability (≥14px)
  - Console error monitoring

- `UserStatsPageE2ETests.cs` - **6 tests**
  - Desktop/mobile page load
  - Responsive layout adaptation
  - Statistics display (WPM, Accuracy, Games)
  - Navigation functionality
  - Console error monitoring

- `ResponsiveDesignE2ETests.cs` - **10 tests** (Theory tests)
  - All pages load on desktop (/, /leaderboard, /user-stats, /diag)
  - All pages load on mobile
  - No horizontal scroll on mobile pages
  - Navigation works across viewports
  - Layout adapts between desktop/mobile
  - Blazor WebAssembly loads on all pages
  - Pages load without console errors
  - Font sizes readable on mobile (≥14px)

---

## 🛠️ Test Helpers (`/TestHelpers/`)
Shared utilities and test infrastructure.

- `CustomWebApplicationFactory.cs` - Factory Pattern for test instances
- Playwright extension methods for E2E tests

---

## 🏃 Running Tests

### Run All Tests (96 total)
```bash
dotnet test
```

### Run Tests by Category
```bash
# Unit tests only (fastest)
dotnet test --filter "FullyQualifiedName~Unit"

# Integration tests (requires Azurite)
dotnet test --filter "FullyQualifiedName~Integration"

# API tests
dotnet test --filter "FullyQualifiedName~API"

# System tests
dotnet test --filter "FullyQualifiedName~System"

# E2E tests (requires running app + Playwright)
dotnet test --filter "FullyQualifiedName~E2E"

# Exclude E2E tests (for CI/CD without browser)
dotnet test --filter "FullyQualifiedName!~E2E"
```

### Run Tests with Code Coverage
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults

# Coverage report location
# TestResults/{guid}/coverage.cobertura.xml
```

### View Coverage in VS Code
1. Install **Coverage Gutters** extension
2. Open Command Palette (Ctrl+Shift+P)
3. Run "Coverage Gutters: Display Coverage"
4. Coverage highlights appear in editor gutter

---

## 🎭 Playwright Setup (for E2E Tests)

### Prerequisites
1. **Install Playwright package** (already installed):
   ```bash
   dotnet add package Microsoft.Playwright --version 1.49.0
   ```

2. **Install Chromium browser**:
   ```bash
   # After building the test project
   pwsh bin/Debug/net9.0/playwright.ps1 install chromium
   ```

3. **Browser location**: `C:\Users\{user}\AppData\Local\ms-playwright\chromium-1148`

### E2E Test Requirements
- **App must be running** on `http://localhost:5208`
- **Chromium browser** installed (131.0.6778.33)
- **Viewport sizes**:
  - Desktop: 1920x1080
  - Mobile: 390x844

### Running E2E Tests
```bash
# Start the API in a separate terminal
dotnet run --project PoFastType.Api

# Run E2E tests in another terminal
dotnet test --filter "FullyQualifiedName~E2E"
```

---

## 🗄️ Azurite Setup (for Integration Tests)

### Install Azurite
```bash
npm install -g azurite
```

### Start Azurite
```bash
azurite --silent --location ./AzuriteData
```

### Connection String
Integration tests use: `UseDevelopmentStorage=true`

### Table Naming Convention
Tables follow the format: `PoFastType{TableName}` (e.g., `PoFastTypeGameResults`)

---

## 🧪 REST Client Testing

### Manual API Testing with VS Code

1. **Install REST Client extension** for VS Code
2. **Open** `PoFastType.http` file
3. **Click "Send Request"** above any request

### Test Coverage in .http File (30+ scenarios)
- Health checks (`/api/diag/health`)
- Game text generation (`/api/game/text`)
- Score submission (valid/invalid)
- Leaderboard queries (various counts)
- Edge cases (null values, invalid data)
- Concurrent request testing
- Performance validation

---

## 📐 Design Patterns Used

- **Factory Pattern**: `CustomWebApplicationFactory` for test instance creation
- **Repository Pattern**: Data access abstraction testing
- **Strategy Pattern**: Text generation strategy testing
- **Dependency Injection**: Mocks for dependencies
- **Page Object Pattern**: Playwright E2E tests

---

## ✅ SOLID Principles

- **Single Responsibility**: Each test class tests one specific component
- **Open/Closed**: Test structure allows easy addition of new test types
- **Liskov Substitution**: Mocks and stubs can replace real implementations
- **Interface Segregation**: Focused interfaces for dependencies
- **Dependency Inversion**: Tests depend on abstractions (interfaces) not concretions

---

## 🎯 Test Coverage Goals

| Test Type | Current Coverage | Goal | Status |
|-----------|------------------|------|--------|
| **Unit Tests** | High (100% for services) | 90%+ for business logic | ✅ Achieved |
| **Integration Tests** | 10 tests | Key component interactions | ✅ Achieved |
| **API Tests** | 18+ tests | All HTTP endpoints | ✅ Good |
| **System Tests** | 8+ tests | Critical user workflows | ✅ Good |
| **E2E Tests** | 36 tests | Responsive UI validation | ✅ Excellent |
| **Overall Line Coverage** | **31.34%** | >60% | ⚠️ In Progress |

---

## 📈 Coverage Improvement History

| Phase | Line Coverage | Tests | Notes |
|-------|---------------|-------|-------|
| **Baseline (Phase 1-2)** | 20.07% | 60 | Initial state |
| **Phase 3** | **31.34%** | **96** | +11.27% improvement |

**Phase 3 Additions:**
- ✅ 18 DiagController tests (0% → 65.21%)
- ✅ 7 enhanced Repository tests (40.74% → 100%)
- ✅ 10 Integration tests (new)
- ✅ 36 E2E tests (new)

---

## 🚀 Next Steps for >60% Coverage

1. **GlobalExceptionMiddleware** (currently 35.71%)
   - Add error path tests
   - Test exception handling scenarios
   
2. **UserIdentityService** (currently 23.52%)
   - Add user identity tests
   - Test anonymous identity creation

3. **AzureTableStorageHealthCheck** (currently 0%)
   - Add health check tests
   - Test Azure Table Storage connectivity

4. **Program.cs startup code** (lines 131-147 at 0%)
   - Test health check registration
   - Test middleware configuration

---

## 📝 Notes

- **E2E tests require app to be running** - skip with `--filter "FullyQualifiedName!~E2E"` for automated CI/CD
- **Integration tests require Azurite** - ensure it's running before executing
- **Coverage reports** are generated in `TestResults/{guid}/coverage.cobertura.xml`
- **Playwright browsers** are installed to `C:\Users\{user}\AppData\Local\ms-playwright\`
- **Test execution time**: ~12-15 seconds for all 96 tests (excluding E2E)
