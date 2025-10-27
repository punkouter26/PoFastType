# PoFastType.Client

Blazor WebAssembly frontend for the PoFastType typing speed test application.

## Overview

This project is a client-side Blazor WebAssembly application that provides an interactive UI for typing tests, leaderboards, user statistics, and system diagnostics. It runs entirely in the user's browser using WebAssembly.

## Project Structure

```
PoFastType.Client/
├── Pages/              # Routable pages (@page directive)
├── Components/         # Reusable UI components
├── Layout/             # Layout components (MainLayout)
├── Services/           # Client-side services
└── wwwroot/            # Static assets (CSS, JS, images)
```

---

## Pages

### `Home.razor`
**Route**: `/`  
**Purpose**: Main typing test interface

**Key Methods:**
- `StartGame()` - Initiates a new typing test session
  - Calls `/api/game/text` to fetch random text
  - Resets timer, WPM, accuracy counters
  - Focuses input textarea

- `OnInput(ChangeEventArgs e)` - Handles real-time typing input
  - Compares user input to target text character-by-character
  - Updates UI with green (correct) and red (incorrect) highlighting
  - Calculates current WPM and accuracy in real-time

- `SubmitResult()` - Saves game result when completed
  - Calls `POST /api/game/results` with game data
  - Displays final WPM, accuracy, time taken
  - Offers to play again

**State Variables:**
- `targetText` - The text to be typed
- `userInput` - Current user's typed text
- `isPlaying` - Game in progress flag
- `startTime` - Game start timestamp
- `wpm` - Current words per minute
- `accuracy` - Current accuracy percentage

---

### `Leaderboard.razor`
**Route**: `/leaderboard`  
**Purpose**: Display global top scores

**Key Methods:**
- `LoadLeaderboard()` - Fetches top scores from API
  - Calls `GET /api/scores/leaderboard?count={limit}`
  - Populates `RadzenDataGrid` with results
  - Auto-refreshes every 60 seconds

- `RefreshLeaderboard()` - Manual refresh button handler
  - Reloads data immediately
  - Shows loading indicator

**State Variables:**
- `leaderboardEntries` - List of top scores
- `isLoading` - Loading state indicator
- `selectedLimit` - Number of results to show (10/25/50/100)

**UI Components:**
- `RadzenDataGrid` - Table display (desktop)
- `RadzenCard` - Card-based layout (mobile)
- `RadzenDropDown` - Result limit selector

---

### `UserStats.razor`
**Route**: `/user-stats`  
**Purpose**: Personal performance dashboard

**Key Methods:**
- `LoadUserStats()` - Fetches user's game history
  - Retrieves userId from local storage
  - Calls `GET /api/scores/user/{userId}`
  - Calculates aggregate statistics

- `CalculateAverage(string property)` - Computes average WPM or accuracy
  - Filters valid results
  - Returns mean value

- `RenderChart()` - Generates WPM over time line chart
  - Uses Chart.js (via JavaScript interop)
  - X-axis: Date/time
  - Y-axis: WPM values

**State Variables:**
- `userResults` - List of all user's game results
- `totalGames` - Count of games played
- `averageWPM` - Mean typing speed
- `bestWPM` - Highest WPM achieved
- `averageAccuracy` - Mean accuracy percentage

**UI Components:**
- `RadzenCard` - Statistic cards (Total Games, Average WPM, etc.)
- `RadzenDataGrid` - Recent games table
- `RadzenChart` - Performance trend chart

---

### `Diag.razor`
**Route**: `/diag`  
**Purpose**: System health diagnostics

**Key Methods:**
- `CheckHealth()` - Polls health endpoint every 30 seconds
  - Calls `GET /api/diag/health`
  - Updates health check UI with results
  - Color-codes status (green/yellow/red)

- `TestEndpoint(string endpoint)` - Manual API test
  - Allows testing any API endpoint
  - Displays JSON response with syntax highlighting
  - Shows HTTP status code

**State Variables:**
- `healthStatus` - Overall system health (Healthy/Degraded/Unhealthy)
- `healthChecks` - Array of individual check results
- `lastChecked` - Timestamp of last health check

**Health Checks Displayed:**
1. Internet Connection (✅/❌)
2. Azure Connectivity (✅/❌)
3. Self Health Check (✅/❌)
4. API Endpoint (✅/❌)
5. Azure Table Storage (✅/❌)
6. OpenAI Service (✅/❌)

---

## Components

### `Navbar_New.razor`
**Purpose**: Global navigation bar

**Features:**
- Responsive hamburger menu for mobile
- Active route highlighting
- Sticky positioning at top
- Links to: Home, Leaderboard, My Stats, Diagnostics

**Key Methods:**
- `ToggleMobileMenu()` - Shows/hides mobile menu
- `IsActive(string route)` - Highlights current page in nav

---

### `ErrorBoundary.razor`
**Purpose**: Catches and displays component errors

**Features:**
- Prevents entire app crash from component errors
- Displays friendly error message to user
- Logs error details to console
- Provides recovery mechanism

---

## Layout

### `MainLayout.razor`
**Purpose**: Defines overall app structure

**Structure:**
```
<div class="page">
    <Navbar_New />
    <main>
        @Body  <!-- Page content injected here -->
    </main>
    <footer>
        © 2025 PoFastType
    </footer>
</div>
```

---

## Services

### `GameStateService.cs`
**Purpose**: Manages game session state

**Key Methods:**
- `StartNewGame()` - Initializes game state
- `UpdateProgress(string input)` - Tracks typing progress
- `EndGame()` - Finalizes results

**State:**
- Current game session
- Timer management
- Real-time calculations

### `UserService.cs` / `IUserService.cs`
**Purpose**: User identity management (client-side)

**Key Methods:**
- `GetOrCreateUserId()` - Retrieves or generates user ID
  - Checks browser local storage
  - Generates GUID if not found
  - Persists to `localStorage`

- `GetUsername()` - Retrieves display name
  - Fetches from local storage
  - Returns "Anonymous" if not set

- `SetUsername(string name)` - Stores display name
  - Validates input
  - Saves to local storage

**Local Storage Keys:**
- `pofasttype_userId` - User GUID
- `pofasttype_username` - Display name

---

## Static Assets (`wwwroot/`)

### `index.html`
Main HTML file that hosts Blazor WebAssembly

**Key Sections:**
- Blazor script reference
- Loading indicator
- Base href configuration
- CDN references (Radzen CSS/JS)

### `app.js`
JavaScript interop functions

**Functions:**
- `focusElement(id)` - Sets focus to input
- `highlightSyntax(code)` - Syntax highlighting for JSON
- `copyToClipboard(text)` - Copy text to clipboard

### `css/app.css`
Custom application styles

**Styles:**
- Typing test UI (text highlighting)
- Responsive breakpoints
- Dark mode support (future)
- Animation transitions

---

## Dependencies

### Radzen.Blazor Components
- `RadzenButton` - Styled buttons
- `RadzenCard` - Card containers
- `RadzenDataGrid` - Data tables
- `RadzenDropDown` - Dropdown selectors
- `RadzenChart` - Charts and graphs
- `RadzenTextBox` - Input fields

### Other Libraries
- `Microsoft.AspNetCore.Components.WebAssembly` - Core Blazor WASM
- `System.Net.Http.Json` - HTTP client with JSON support

---

## Routing

Routes defined with `@page` directive:

| Route | Component | Purpose |
|-------|-----------|---------|
| `/` | Home.razor | Typing test |
| `/leaderboard` | Leaderboard.razor | Global rankings |
| `/user-stats` | UserStats.razor | Personal stats |
| `/diag` | Diag.razor | System diagnostics |

---

## Local Storage

Data persisted in browser:

```javascript
localStorage.setItem('pofasttype_userId', 'guid');
localStorage.setItem('pofasttype_username', 'username');
```

**Purpose:**
- User identity across sessions
- No server-side authentication required
- Data persists until browser cache cleared

---

## HTTP Communication

All API calls use `HttpClient` injected via DI:

```csharp
@inject HttpClient Http

// Example GET
var response = await Http.GetAsync("/api/game/text");

// Example POST
await Http.PostAsJsonAsync("/api/game/results", result);
```

**Base URL**: Configured in `Program.cs` to API base URL

---

## State Management

### Component State
- Local `@code` block variables
- Scoped services for shared state
- Browser local storage for persistence

### No Global State
- No Redux/Flux pattern (not needed for simple app)
- Services injected per-component
- Parent-child parameter passing where needed

---

## Responsive Design

### Breakpoints
- **Mobile**: < 768px (hamburger menu, card layout)
- **Tablet**: 768px - 1024px (mixed layout)
- **Desktop**: > 1024px (full table, expanded nav)

### Mobile Optimizations
- Touch-friendly buttons (min 44px)
- Simplified navigation (hamburger)
- Card-based leaderboard (instead of table)
- Larger font sizes for readability

---

## Performance

### Lazy Loading
Components loaded on-demand, not upfront

### AOT Compilation
Ahead-of-time compilation for faster runtime

### Asset Optimization
- Minified CSS/JS
- Compressed images
- CDN-hosted libraries

---

## Testing

### Unit Tests (bUnit)
Located in `PoFastType.Tests/Unit/Components/`

**Example:**
```csharp
[Fact]
public void HomePage_Renders_StartButton()
{
    var ctx = new TestContext();
    var component = ctx.RenderComponent<Home>();
    
    var button = component.Find("button");
    Assert.Equal("Start Game", button.TextContent);
}
```

---

## Accessibility

- Semantic HTML elements
- ARIA labels on interactive elements
- Keyboard navigation support
- Screen reader friendly
- High contrast mode support

---

## Future Enhancements

- [ ] Progressive Web App (PWA) support
- [ ] Offline mode with service worker
- [ ] Push notifications for leaderboard updates
- [ ] Theming (light/dark mode toggle)
- [ ] Localization (multi-language support)
