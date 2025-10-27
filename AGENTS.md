# AGENTS.MD - AI Coding Agent Guide
## PoFastType Project Context for AI Assistants

> **Purpose**: This document provides AI coding agents (GitHub Copilot, Cursor, Claude, etc.) with the essential context needed to generate code that aligns with this project's architecture, conventions, and constraints.

---

## 🎯 Project Quick Facts

- **Name**: PoFastType
- **Type**: Typing speed test web application
- **Framework**: .NET 9.0, Blazor WebAssembly
- **Database**: Azure Table Storage
- **Hosting**: Azure App Service (F1 Free Tier)
- **CI/CD**: GitHub Actions + Azure Developer CLI (azd)
- **Repository**: Private (punkouter26/PoFastType)

---

## 🏗️ Architecture Overview

### Pattern: Vertical Slice Architecture
- Each feature is a self-contained slice (no shared repositories)
- Apply Clean Architecture principles within complex slices
- Keep it simple for straightforward CRUD operations

### Project Structure
```
PoFastType/
├── PoFastType.Api/          # ASP.NET Core Web API (Backend)
├── PoFastType.Client/       # Blazor WebAssembly (Frontend)
├── PoFastType.Shared/       # DTOs and Models
├── PoFastType.Tests/        # xUnit Tests
├── infra/                   # Bicep infrastructure as code
└── .github/workflows/       # CI/CD pipelines
```

---

## 🛠️ Build & Run Commands

### Local Development
```powershell
# Start Azurite (local Azure Storage emulator)
.\scripts\start-azurite.ps1

# Run the API (includes Blazor client)
dotnet run --project PoFastType.Api

# Or use VS Code debugger (F5)
```

### Testing
```powershell
# Run all tests (excluding E2E)
dotnet test --filter "FullyQualifiedName!~E2E"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific category
dotnet test --filter "FullyQualifiedName~Unit"
```

### Deployment
```powershell
# Deploy to Azure (via azd)
azd up

# CI/CD deploys automatically on push to master
git push origin master
```

---

## 📜 Code Style & Conventions

### General Rules
1. **Follow `.editorconfig`**: All formatting rules defined there
2. **SOLID Principles**: Especially Single Responsibility
3. **File Size Limit**: Refactor if a file exceeds ~500 lines
4. **Naming**:
   - Classes/Methods: `PascalCase`
   - Private fields: `_camelCase`
   - Interfaces: `IPascalCase`
   - Async methods: `MethodNameAsync`
   - Constants: `PascalCase`

### .NET Specific
```csharp
// ✅ GOOD - Use var for obvious types
var gameService = new GameService();

// ✅ GOOD - Expression-bodied members for simple cases
public string Username => _user?.Name ?? "Anonymous";

// ✅ GOOD - Pattern matching
if (result is { WPM: > 100, Accuracy: > 95 })

// ❌ BAD - Unnecessary this keyword
this.gameService = gameService; // Remove "this"

// ❌ BAD - Not using async suffix
public async Task GetData() // Should be GetDataAsync()
```

### Blazor Specific
```razor
@* ✅ GOOD - Use Radzen components for UI *@
<RadzenButton Click="StartGame">Start</RadzenButton>

@* ✅ GOOD - Component parameters with validation *@
[Parameter, EditorRequired]
public required string UserId { get; set; }

@* ❌ BAD - Don't create custom components when Radzen has them *@
<CustomButton /> @* Use RadzenButton instead *@
```

---

## 🗃️ Data Access Patterns

### Azure Table Storage Rules
1. **NO generic repositories** - Create feature-specific interfaces
2. **Partition Key**: Use `userId` for user-specific data
3. **Row Key**: Use `Timestamp.Ticks` for temporal ordering
4. **Table Names**: Follow format `PoFastType{EntityName}`

```csharp
// ✅ GOOD - Feature-specific interface
public interface IGameResultRepository
{
    Task AddResultAsync(GameResult result);
    Task<IEnumerable<GameResult>> GetTopScoresAsync(int count);
    Task<IEnumerable<GameResult>> GetUserResultsAsync(string userId);
}

// ❌ BAD - Generic repository
public interface IRepository<T>
{
    Task Add(T entity); // Too generic, inefficient for Table Storage
}
```

### Entity Structure
```csharp
// ✅ GOOD - Proper Table Storage entity
public class GameResult : ITableEntity
{
    public string PartitionKey { get; set; } = default!; // UserId
    public string RowKey { get; set; } = default!;       // Timestamp
    public int WPM { get; set; }
    public double Accuracy { get; set; }
    public int DurationSeconds { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

---

## 🧪 Testing Standards

### Test Organization
- **Unit Tests**: `PoFastType.Tests/Unit/`
- **Integration Tests**: `PoFastType.Tests/Integration/`
- **API Tests**: `PoFastType.Tests/API/`
- **E2E Tests**: `PoFastType.Tests/E2E/` (requires running app)

### Test Naming Convention
```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public void CalculateWPM_WithValidInput_ReturnsCorrectValue()
{
    // Arrange
    var service = new GameService();
    
    // Act
    var wpm = service.CalculateWPM(250, 60);
    
    // Assert
    Assert.Equal(50, wpm);
}
```

### Coverage Target
- **Minimum**: 60% (enforced in CI)
- **Goal**: 80%+ for business logic
- **Run**: `dotnet test --collect:"XPlat Code Coverage"`

---

## 🚨 Important Gotchas & Tips

### 1. **Secrets Management**
⚠️ **This is a private repository** - Secrets CAN be in `appsettings.json`  
✅ **Development**: Use `UseDevelopmentStorage=true` for Azurite  
✅ **Production**: Override via Azure App Service configuration

```json
// appsettings.Development.json - OK for private repo
{
  "AzureTableStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "TableName": "PoFastTypeGameResults"
  }
}
```

### 2. **Azure Free Tier Constraints**
- **App Service Plan**: F1 (Free) - 60 CPU minutes/day, 1GB RAM
- **Must use existing plan**: `PoShared/PoSharedAppServicePlan`
- **If plan is full**: Try `PoSharedAppServicePlan2`, `3`, `4`, or `5`
- **No AlwaysOn**: Free tier doesn't support it
- **First request slow**: App sleeps when idle (~20s cold start)

### 3. **Blazor WebAssembly Hosting**
✅ **Client is hosted inside API project** - No CORS needed  
✅ **Static files served from `wwwroot`**  
❌ **Don't create separate hosting** - Keep it in PoFastType.Api

### 4. **Exception Handling**
✅ **Global middleware exists**: `GlobalExceptionMiddleware.cs`  
✅ **Returns Problem Details (RFC 7807)**  
✅ **Logs to Serilog** (console + rolling file)

```csharp
// ✅ GOOD - Let middleware handle exceptions
public async Task<IActionResult> GetScores()
{
    var scores = await _repository.GetTopScoresAsync(50);
    return Ok(scores);
}

// ❌ BAD - Don't wrap everything in try/catch
public async Task<IActionResult> GetScores()
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message); // Middleware does this
    }
}
```

### 5. **Health Checks**
- **Endpoint**: `/api/health` (used by CD pipeline)
- **Checks**: Internet, Azure, API, Table Storage, OpenAI
- **Add new checks to**: `DiagController.cs`

### 6. **Deployment**
⚠️ **ONLY deploy via GitHub Actions** - No manual `azd up`  
✅ **Trigger**: Push to `master` branch  
✅ **Pipeline**: Build → Test → Deploy → Health Check

---

## 🔒 Security Guidelines

1. **HTTPS Only**: All traffic over TLS 1.2+
2. **No Authentication**: Public app (no user accounts yet)
3. **Input Validation**: Validate all API inputs
4. **Rate Limiting**: Prevent abuse on result submissions
5. **No PII**: Store only GUID for users (no names/emails)

```csharp
// ✅ GOOD - Validate input
[HttpPost("results")]
public async Task<IActionResult> SubmitResult([FromBody] GameResult result)
{
    if (result.WPM < 0 || result.WPM > 300)
        return BadRequest("Invalid WPM value");
        
    if (result.Accuracy < 0 || result.Accuracy > 100)
        return BadRequest("Invalid accuracy value");
        
    await _repository.AddResultAsync(result);
    return Ok();
}
```

---

## 📦 Dependencies

### Approved Libraries
- **Radzen.Blazor**: UI components
- **Serilog**: Logging
- **Azure.Data.Tables**: Table Storage SDK
- **Microsoft.ApplicationInsights**: Telemetry
- **xUnit**: Testing framework
- **Microsoft.Playwright**: E2E testing

### Adding New Dependencies
⚠️ **Requires approval** - Don't add packages without review  
✅ **Check**: License, maintenance status, alternatives  
✅ **Justify**: Why is it needed? Can we use existing libs?

---

## 🔄 CI/CD Workflow

### CI Pipeline (`.github/workflows/ci.yml`)
1. Build solution
2. Run tests (exclude E2E)
3. Code quality checks (`dotnet format`)
4. Security scan (vulnerable packages)
5. Upload coverage reports

### CD Pipeline (`.github/workflows/cd.yml`)
1. Build solution
2. Run tests
3. Deploy to Azure via `azd up`
4. Verify `/api/health` endpoint
5. Generate deployment summary

### PR Validation (`.github/workflows/pr-validation.yml`)
- Runs on all PRs
- Posts results as PR comment
- Auto-labels PRs (ready-for-review, needs-work, security)

---

## 🗺️ Common Tasks

### Add a New API Endpoint
1. Create method in appropriate controller (e.g., `GameController.cs`)
2. Add business logic to service (e.g., `GameService.cs`)
3. Add data access to repository (e.g., `IGameResultRepository.cs`)
4. Write unit tests in `PoFastType.Tests/Unit/`
5. Write API tests in `PoFastType.Tests/API/`
6. Update Swagger comments with `/// <summary>`

### Add a New Blazor Page
1. Create `.razor` file in `PoFastType.Client/Pages/`
2. Add route with `@page "/route"`
3. Inject services with `@inject IServiceName ServiceName`
4. Add navigation link to `Navbar_New.razor`
5. Use Radzen components for UI
6. Add bUnit tests in `PoFastType.Tests/Unit/Components/`

### Add a New Health Check
1. Add check to `DiagController.HealthCheck()` method
2. Return status object with check results
3. Update `Diag.razor` to display new check
4. Test via `/diag` page

---

## 🌐 API Endpoint Reference

### Game Endpoints
- `GET /api/game/text` - Get random typing text
- `POST /api/game/results` - Submit game result

### Scores Endpoints
- `GET /api/scores/leaderboard?count=50` - Get top scores
- `GET /api/scores/user/{userId}` - Get user's results

### Diagnostics Endpoints
- `GET /api/health` - Simple health check (HTTP 200/500)
- `GET /api/diag/health` - Detailed health with all checks

### User Endpoints
- `POST /api/user/identity` - Create/update user identity
- `GET /api/user/identity/{userId}` - Get user identity

---

## 🎨 UI Component Library

### Radzen Components Used
```razor
@* Buttons *@
<RadzenButton Click="@HandleClick">Click Me</RadzenButton>

@* Text Input *@
<RadzenTextBox @bind-Value="username" />

@* Data Grid *@
<RadzenDataGrid Data="@scores" TItem="LeaderboardEntry">
    <Columns>
        <RadzenDataGridColumn TItem="LeaderboardEntry" Property="Rank" Title="Rank" />
    </Columns>
</RadzenDataGrid>

@* Card *@
<RadzenCard>
    <h3>Title</h3>
    <p>Content</p>
</RadzenCard>
```

---

## 🐛 Debugging Tips

### Local Development
1. **Azurite must be running**: Check Task Manager for "node.exe" (Azurite)
2. **Check ports**: API runs on https://localhost:5001
3. **Browser cache**: Clear if Blazor changes not appearing
4. **Hot reload**: Enabled by default with `dotnet watch`

### Common Errors
```
Error: "UseDevelopmentStorage is not supported"
Fix: Start Azurite with .\scripts\start-azurite.ps1

Error: "Table does not exist"
Fix: Tables auto-create on first write, or manually create in Azure Storage Explorer

Error: "E2E tests failing with connection refused"
Fix: E2E tests require app to be running - excluded from CI

Error: "App Service deployment fails"
Fix: Check if PoSharedAppServicePlan is full, try AppServicePlan2/3/4/5
```

---

## 📊 Performance Targets

- **API Response**: < 500ms (95th percentile)
- **Page Load**: < 2 seconds
- **Typing Latency**: < 50ms (real-time feedback)
- **Availability**: 99.9% uptime
- **Error Rate**: < 0.1%

---

## 🔮 Future Enhancements (Don't Implement Yet)

- User authentication (Azure AD B2C)
- Multiplayer races
- Custom text uploads
- Mobile apps (iOS/Android)
- Themes and customization
- Offline mode
- Multi-language support

❌ **Don't add these features** unless explicitly requested

---

## ✅ AI Agent Checklist

Before generating code, ensure you understand:
- [ ] Which project the code belongs to (Api/Client/Shared/Tests)
- [ ] The architectural pattern (Vertical Slice, Clean Architecture)
- [ ] Naming conventions (PascalCase, _camelCase, async suffix)
- [ ] Testing requirements (Unit, Integration, or API test needed?)
- [ ] Azure Table Storage entity structure (PartitionKey, RowKey)
- [ ] Whether to use Radzen components (if Blazor UI)
- [ ] Error handling approach (let middleware handle exceptions)
- [ ] Secret management (OK in appsettings for private repo)

---

## 📞 Questions for Humans

If unsure, ask:
1. "Should this be a new controller or added to existing?"
2. "Do you want unit tests, integration tests, or both?"
3. "Should I use Radzen components or standard HTML?"
4. "Is this feature a new vertical slice or part of existing?"
5. "Should I update the API documentation/Swagger comments?"

---

**Document Version**: 1.0  
**Last Updated**: October 27, 2025  
**Maintained by**: Development Team  
**Target Audience**: AI Coding Agents (GitHub Copilot, Cursor, Claude, etc.)
