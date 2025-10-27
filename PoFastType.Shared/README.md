# PoFastType.Shared

Shared data transfer objects (DTOs) and domain models used across both API and Client projects.

## Overview

This project contains all shared types that are serialized between the Blazor WebAssembly client and the ASP.NET Core API. By keeping these in a separate project, we ensure consistency and avoid code duplication.

---

## Models

### `GameResult.cs`
**Purpose**: Represents a completed typing test result

**Properties:**
- `PartitionKey` (string) - User ID (for Azure Table Storage partitioning)
- `RowKey` (string) - Timestamp in ticks (for unique row identification)
- `UserId` (string) - GUID identifying the user
- `WPM` (int) - Words per minute achieved
- `Accuracy` (double) - Percentage of correct characters (0-100)
- `DurationSeconds` (int) - Time taken to complete the test
- `TextLength` (int) - Number of characters in the test text
- `DatePlayed` (DateTime) - When the game was completed
- `Timestamp` (DateTimeOffset?) - Azure Table Storage timestamp
- `ETag` (ETag) - Azure Table Storage concurrency token

**Implements**: `ITableEntity` (Azure.Data.Tables)

**Usage:**
- Stored in Azure Table Storage (`PoFastTypeGameResults` table)
- Sent from client when submitting results
- Returned to client for leaderboard and user stats

**Example:**
```csharp
var result = new GameResult
{
    PartitionKey = userId,
    RowKey = DateTime.UtcNow.Ticks.ToString(),
    UserId = userId,
    WPM = 75,
    Accuracy = 98.5,
    DurationSeconds = 45,
    TextLength = 250,
    DatePlayed = DateTime.UtcNow
};
```

---

### `LeaderboardEntry.cs`
**Purpose**: Represents a single entry in the global leaderboard

**Properties:**
- `Rank` (int) - Position on the leaderboard (1, 2, 3...)
- `UserId` (string) - User's unique identifier
- `Username` (string) - Display name of the user
- `WPM` (int) - Typing speed in words per minute
- `Accuracy` (double) - Accuracy percentage
- `DateAchieved` (DateTime) - When this score was set

**Usage:**
- Returned by `/api/scores/leaderboard` endpoint
- Displayed in Leaderboard page table/cards
- Sorted by WPM descending, then accuracy descending

**Example:**
```csharp
var entry = new LeaderboardEntry
{
    Rank = 1,
    UserId = "guid",
    Username = "TypeMaster",
    WPM = 120,
    Accuracy = 99.8,
    DateAchieved = DateTime.UtcNow
};
```

---

### `UserGameResult.cs`
**Purpose**: Represents a user's individual game history

**Properties:**
- `GameId` (string) - Unique identifier for the game
- `WPM` (int) - Words per minute achieved
- `Accuracy` (double) - Accuracy percentage
- `DurationSeconds` (int) - Time taken
- `TextLength` (int) - Number of characters typed
- `DatePlayed` (DateTime) - When the game was completed
- `PersonalBest` (bool) - Flag indicating if this is user's best WPM

**Usage:**
- Returned by `/api/scores/user/{userId}` endpoint
- Displayed in User Stats page table
- Used for calculating average WPM, accuracy, and trends

**Differences from `GameResult`:**
- No Azure Table Storage properties (PartitionKey, RowKey, etc.)
- Includes `PersonalBest` flag for highlighting
- Optimized for client-side consumption

**Example:**
```csharp
var userResult = new UserGameResult
{
    GameId = "guid",
    WPM = 85,
    Accuracy = 96.5,
    DurationSeconds = 40,
    TextLength = 200,
    DatePlayed = DateTime.UtcNow,
    PersonalBest = true
};
```

---

### `UserIdentity.cs`
**Purpose**: Represents user profile information

**Properties:**
- `UserId` (string) - Unique identifier (GUID)
- `Username` (string) - Display name chosen by user
- `DateCreated` (DateTime) - When the user first played
- `LastPlayed` (DateTime?) - Last game session timestamp
- `TotalGamesPlayed` (int) - Count of completed games
- `BestWPM` (int) - Highest WPM achieved
- `AverageWPM` (double) - Mean WPM across all games

**Usage:**
- Stored in Azure Table Storage (future: `PoFastTypeUsers` table)
- Sent to `/api/user/identity` for creation/update
- Retrieved from `/api/user/identity/{userId}`
- Displayed in User Stats dashboard

**Example:**
```csharp
var identity = new UserIdentity
{
    UserId = Guid.NewGuid().ToString(),
    Username = "SpeedTyper",
    DateCreated = DateTime.UtcNow,
    TotalGamesPlayed = 15,
    BestWPM = 95,
    AverageWPM = 78.3
};
```

---

## Design Principles

### 1. Immutability (Where Possible)
Properties use `{ get; set; }` for serialization compatibility, but should be treated as immutable after construction.

### 2. No Business Logic
Models contain only data, no methods or logic. Keep models simple and focused on data structure.

### 3. Serialization-Friendly
- All properties are public
- Parameterless constructors
- Compatible with `System.Text.Json`
- No circular references

### 4. Validation Attributes (Future)
Consider adding `[Required]`, `[Range]`, `[StringLength]` attributes for automatic validation:

```csharp
public class GameResult
{
    [Required]
    [Range(0, 300, ErrorMessage = "WPM must be between 0 and 300")]
    public int WPM { get; set; }
    
    [Range(0, 100, ErrorMessage = "Accuracy must be between 0 and 100")]
    public double Accuracy { get; set; }
}
```

---

## JSON Serialization

### Default Serialization Settings
Configured in both API (`Program.cs`) and Client:

```csharp
JsonSerializerOptions options = new()
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

### Example JSON

**GameResult:**
```json
{
  "userId": "abc123-def456",
  "wpm": 75,
  "accuracy": 98.5,
  "durationSeconds": 45,
  "textLength": 250,
  "datePlayed": "2025-10-27T10:30:00Z"
}
```

**LeaderboardEntry:**
```json
{
  "rank": 1,
  "userId": "abc123-def456",
  "username": "TypeMaster",
  "wpm": 120,
  "accuracy": 99.8,
  "dateAchieved": "2025-10-27T10:30:00Z"
}
```

---

## Azure Table Storage Mapping

### GameResult Entity
- **Table Name**: `PoFastTypeGameResults`
- **Partition Key**: `UserId` (groups all games by user)
- **Row Key**: `Timestamp.Ticks` (ensures chronological ordering)

**Benefits:**
- Efficient user-specific queries (query single partition)
- Automatic chronological sorting (row key is timestamp)
- Scalable partitioning across storage nodes

**Example Query:**
```csharp
// Get all results for a user (efficient partition query)
var userResults = tableClient.Query<GameResult>(
    filter: $"PartitionKey eq '{userId}'"
);

// Get top scores globally (expensive table scan - cache this!)
var topScores = tableClient.Query<GameResult>()
    .OrderByDescending(r => r.WPM)
    .Take(50);
```

---

## Usage Examples

### Client → API (POST)
```csharp
// Submit game result
var result = new GameResult
{
    UserId = userId,
    WPM = 85,
    Accuracy = 96.5,
    DurationSeconds = 40,
    TextLength = 200,
    DatePlayed = DateTime.UtcNow
};

await Http.PostAsJsonAsync("/api/game/results", result);
```

### API → Client (GET)
```csharp
// Fetch leaderboard
var leaderboard = await Http.GetFromJsonAsync<List<LeaderboardEntry>>(
    "/api/scores/leaderboard?count=50"
);

// Fetch user stats
var userResults = await Http.GetFromJsonAsync<List<UserGameResult>>(
    $"/api/scores/user/{userId}"
);
```

---

## Testing

### Unit Tests
Test model serialization/deserialization:

```csharp
[Fact]
public void GameResult_Serializes_Correctly()
{
    var result = new GameResult { WPM = 75, Accuracy = 98.5 };
    var json = JsonSerializer.Serialize(result);
    var deserialized = JsonSerializer.Deserialize<GameResult>(json);
    
    Assert.Equal(75, deserialized.WPM);
    Assert.Equal(98.5, deserialized.Accuracy);
}
```

---

## Best Practices

### 1. Keep Models Simple
❌ **Bad:**
```csharp
public class GameResult
{
    public int WPM { get; set; }
    
    public string GetPerformanceLevel()  // Business logic in model
    {
        return WPM > 80 ? "Expert" : "Beginner";
    }
}
```

✅ **Good:**
```csharp
public class GameResult
{
    public int WPM { get; set; }
}

// In a service:
public string GetPerformanceLevel(GameResult result)
{
    return result.WPM > 80 ? "Expert" : "Beginner";
}
```

### 2. Use DTOs for API Responses
When API response differs from storage model, create a DTO:

```csharp
// Storage model (includes Azure properties)
public class GameResult : ITableEntity { }

// API response model (clean, no infrastructure concerns)
public class GameResultDto
{
    public int WPM { get; set; }
    public double Accuracy { get; set; }
}
```

### 3. Avoid Circular References
❌ **Bad:**
```csharp
public class User
{
    public List<GameResult> Games { get; set; }
}

public class GameResult
{
    public User User { get; set; }  // Circular!
}
```

✅ **Good:**
```csharp
public class User
{
    public string UserId { get; set; }
}

public class GameResult
{
    public string UserId { get; set; }  // Reference by ID
}
```

---

## Future Enhancements

- [ ] Add validation attributes for automatic model validation
- [ ] Create DTOs to separate API contracts from storage models
- [ ] Add base classes for common properties (Created, Modified timestamps)
- [ ] Implement `IEquatable<T>` for value comparison
- [ ] Add XML documentation comments for IntelliSense

---

## Dependencies

- `Azure.Data.Tables` - For `ITableEntity` interface
- `System.Text.Json` - For serialization (no explicit reference, part of .NET)

---

## Notes

- This project has no dependencies on API or Client projects
- Changes here affect both API and Client (breaking changes!)
- Keep backward compatibility when modifying existing models
- Version models if making breaking changes (e.g., `GameResultV2`)
