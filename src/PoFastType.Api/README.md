# PoFastType.Api

ASP.NET Core 9.0 Web API providing backend services for the PoFastType typing speed test application.

## Overview

This project serves as both the backend API and hosts the Blazor WebAssembly client. It provides RESTful endpoints for game management, scoring, user tracking, and system diagnostics.

## Key Components

### Controllers

#### `GameController.cs`
Manages typing game sessions and text generation.

**Key Methods:**
- `GetGameText()` - Returns random text snippet for typing practice
  - **Route**: `GET /api/game/text`
  - **Returns**: `{ text: "random text content", length: 250 }`
  - **Purpose**: Provides the challenge text for each game session

- `SubmitGameResult(GameResult result)` - Stores completed game results
  - **Route**: `POST /api/game/results`
  - **Body**: `GameResult` object with WPM, accuracy, duration
  - **Returns**: HTTP 201 Created with result ID
  - **Purpose**: Persists game performance data to Azure Table Storage

#### `ScoresController.cs`
Handles leaderboard and user statistics.

**Key Methods:**
- `GetLeaderboard(int count = 50)` - Retrieves top scores globally
  - **Route**: `GET /api/scores/leaderboard?count={count}`
  - **Parameters**: `count` (optional, default 50) - number of top scores
  - **Returns**: Array of `LeaderboardEntry` sorted by WPM descending
  - **Purpose**: Powers the global leaderboard page

- `GetUserResults(string userId)` - Fetches all results for a specific user
  - **Route**: `GET /api/scores/user/{userId}`
  - **Parameters**: `userId` (GUID) - unique user identifier
  - **Returns**: Array of `UserGameResult` with personal stats
  - **Purpose**: Powers the User Stats page with historical data

#### `DiagController.cs`
System health monitoring and diagnostics.

**Key Methods:**
- `Health()` - Simple health check for load balancers
  - **Route**: `GET /api/health`
  - **Returns**: HTTP 200 OK if healthy, 500 if unhealthy
  - **Purpose**: Used by Azure health probes and CD pipeline

- `HealthCheck()` - Detailed health status with all checks
  - **Route**: `GET /api/diag/health`
  - **Returns**: JSON object with 6 health check results
  - **Checks**:
    1. Internet connectivity (google.com ping)
    2. Azure service availability
    3. Self health endpoint
    4. API endpoint validation
    5. Azure Table Storage connectivity
    6. OpenAI service availability (if configured)
  - **Purpose**: Comprehensive diagnostics for troubleshooting

#### `UserController.cs`
User identity and profile management.

**Key Methods:**
- `CreateOrUpdateIdentity(UserIdentity identity)` - Stores user profile
  - **Route**: `POST /api/user/identity`
  - **Body**: `UserIdentity` with userId and username
  - **Returns**: HTTP 200 OK
  - **Purpose**: Persists user display names and preferences

- `GetIdentity(string userId)` - Retrieves user profile
  - **Route**: `GET /api/user/identity/{userId}`
  - **Returns**: `UserIdentity` object or 404 if not found
  - **Purpose**: Fetches user display name for UI

---

### Services

#### `GameService.cs`
Business logic for game management.

**Key Methods:**
- `CalculateWPM(int charactersTyped, double secondsElapsed)` - Calculates typing speed
  - **Formula**: `(charactersTyped / 5) / (secondsElapsed / 60)`
  - **Returns**: Words per minute as integer
  - **Purpose**: Standard WPM calculation (5 chars = 1 word)

- `CalculateAccuracy(int correctChars, int totalChars)` - Calculates accuracy percentage
  - **Formula**: `(correctChars / totalChars) * 100`
  - **Returns**: Accuracy as double (0-100)
  - **Purpose**: Measures typing precision

#### `TextGenerationService.cs`
Generates random text for typing challenges.

**Key Methods:**
- `GenerateTextAsync(int length = 250)` - Creates random text snippet
  - **Parameters**: `length` - target character count
  - **Returns**: String of random text from configured strategy
  - **Purpose**: Provides varied typing challenges
  - **Strategy**: Currently uses `HardcodedTextStrategy`, can be extended for AI-generated text

#### `UserIdentityService.cs`
Manages user identity and authentication (future).

**Key Methods:**
- `GetOrCreateUserIdAsync()` - Retrieves or generates user GUID
  - **Returns**: User ID as string (GUID format)
  - **Purpose**: Ensures unique user identification without authentication

- `ValidateUserId(string userId)` - Validates GUID format
  - **Returns**: Boolean indicating validity
  - **Purpose**: Input validation for user-provided IDs

---

### Repositories

#### `AzureTableGameResultRepository.cs`
Data access layer for Azure Table Storage.

**Key Methods:**
- `AddResultAsync(GameResult result)` - Inserts new game result
  - **Parameters**: `GameResult` entity
  - **Storage**: Partition by userId, row key by timestamp
  - **Purpose**: Persists game results to cloud storage

- `GetTopScoresAsync(int count)` - Queries highest WPM scores
  - **Query**: Table scan sorted by WPM descending
  - **Returns**: Top N `GameResult` entities
  - **Purpose**: Leaderboard data retrieval

- `GetUserResultsAsync(string userId)` - Fetches all results for user
  - **Query**: Partition query by userId
  - **Returns**: All `GameResult` entities for that user
  - **Purpose**: User statistics and history

#### `IGameResultRepository.cs`
Repository interface for dependency injection.

**Purpose**: Abstraction for data access, enables testing with mocks

---

### Middleware

#### `GlobalExceptionMiddleware.cs`
Centralized exception handling.

**Functionality:**
- Catches all unhandled exceptions in the request pipeline
- Logs errors to Serilog (console + rolling file)
- Returns RFC 7807 Problem Details responses
- Provides consistent error format for clients
- Prevents sensitive error details from leaking in production

**Error Response Format:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request",
  "status": 500,
  "detail": "Exception message (dev only)",
  "instance": "/api/game/results"
}
```

---

## Configuration

### appsettings.Development.json
- **Azurite**: Uses `UseDevelopmentStorage=true` for local testing
- **Table Name**: `PoFastTypeGameResults`
- **Logging**: Information level for debugging
- **CORS**: Allows all origins for development

### appsettings.Production.json
- **Azure Storage**: Connection string injected via App Service configuration
- **Application Insights**: Telemetry connection string
- **Logging**: Warning level to reduce noise
- **CORS**: Restricted to deployed origins

---

## Dependency Injection

Services registered in `Program.cs`:

```csharp
// Repositories
builder.Services.AddScoped<IGameResultRepository, AzureTableGameResultRepository>();

// Services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ITextGenerationService, TextGenerationService>();
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();

// Text Generation Strategy
builder.Services.AddScoped<ITextGenerationStrategy, HardcodedTextStrategy>();
```

---

## Health Checks

Endpoint: `/api/diag/health`

**Checks Performed:**
1. **Internet Connection**: Ping google.com
2. **Azure Connectivity**: Test Azure endpoint
3. **Self Health**: Call own `/api/health` endpoint
4. **API Validation**: Test game text endpoint
5. **Table Storage**: Query Azure Table Storage
6. **OpenAI Service**: Check AI service availability

**Response Statuses:**
- `Healthy` - All checks passed
- `Degraded` - Some checks failed
- `Unhealthy` - Critical checks failed

---

## Running Locally

### Prerequisites
- .NET 9.0 SDK
- Azurite (Azure Storage Emulator)
- Visual Studio 2022 or VS Code

### Steps

1. **Start Azurite**:
   ```powershell
   .\scripts\start-azurite.ps1
   ```

2. **Run the API**:
   ```powershell
   dotnet run --project PoFastType.Api
   ```

3. **Access Endpoints**:
   - **App**: https://localhost:5001
   - **Swagger**: https://localhost:5001/swagger
   - **Health**: https://localhost:5001/api/health

---

## Testing

### Unit Tests
Located in `PoFastType.Tests/Unit/`

**Run:**
```powershell
dotnet test --filter "FullyQualifiedName~Unit"
```

### API Tests
Located in `PoFastType.Tests/API/`

**Run:**
```powershell
dotnet test --filter "FullyQualifiedName~API"
```

---

## Deployment

**Automated via GitHub Actions:**
- Push to `master` branch triggers CD pipeline
- Pipeline runs: Build → Test → Deploy (azd up)
- Health check verifies deployment success
- App Service: `PoFastType.azurewebsites.net`

**Manual Deployment:**
```powershell
azd up
```

---

## Architecture Notes

- **Vertical Slice**: Each feature is self-contained
- **Clean Architecture**: Separation of concerns (Controllers → Services → Repositories)
- **Dependency Injection**: All dependencies injected, no static classes
- **Stateless**: No server-side sessions, fully stateless API
- **RESTful**: Standard HTTP verbs and status codes

---

## Performance

- **Target Response Time**: < 500ms (95th percentile)
- **Concurrent Users**: Up to 100 (F1 plan limit)
- **Throttling**: Rate limiting on result submissions
- **Caching**: Consider adding for leaderboard queries

---

## Security

- **HTTPS Only**: TLS 1.2+ enforced
- **Input Validation**: All inputs validated in controllers
- **No Authentication**: Public API (future: Azure AD B2C)
- **No PII**: User IDs are GUIDs, no personal data collected

---

## Monitoring

- **Application Insights**: Exception tracking, request telemetry
- **Serilog**: Console and rolling file logs
- **Health Checks**: Automated monitoring via `/api/health`

---

## Future Enhancements

- [ ] Rate limiting middleware
- [ ] Redis caching for leaderboard
- [ ] Authentication with Azure AD B2C
- [ ] WebSocket support for multiplayer
- [ ] Background jobs for cleanup
