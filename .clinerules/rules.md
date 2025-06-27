
### **PoFastType: Project Specification & Implementation Plan**


### 1. Application Overview


PoFastType is a modern, web-based typing game designed to test and improve a user's typing speed and accuracy. The primary goal of the application is to provide a challenging, engaging, and endlessly replayable typing experience by leveraging artificial intelligence for content generation. Unlike traditional typing tutors that rely on static, repetitive text blocks, PoFastType will call the Azure OpenAI service in real-time to generate a unique, approximately 400-word paragraph for every single test. This ensures that users face a fresh challenge with every attempt, preventing rote memorization and providing a true measure of their "on-the-fly" typing skill.


The application's user experience is centered around a clean, minimalist interface that clearly separates the source text from the user's input area. The core gameplay loop is a 60-second timed test. A key design decision is the feedback mechanism: during the test, the user receives no real-time error correction, allowing them to type in a continuous flow. Only after the 60 seconds are complete will the system analyze their input and highlight any discrepancies. The results are then presented in a detailed summary, focusing on key metrics like Net Words Per Minute (WPM), accuracy, and more advanced statistics like burst speed and longest error-free streak.


To foster a sense of community and competition, PoFastType will feature an optional user authentication system using Azure Entra ID. Logged-in users can have their best scores saved to a global, all-time Top 10 leaderboard. Furthermore, registered users will gain access to a personal dashboard, a powerful tool for self-improvement. This dashboard will visualize their performance trends over time with charts and provide actionable insights by identifying their specific "problem keys"—the characters they most frequently mistype. The entire application will be built on a modern .NET technology stack, with a Blazor WebAssembly client and a .NET Core Web API server, utilizing Azure Table Storage for efficient data persistence.


### 2. Detailed Page & Component Breakdown


The application consists of three main pages and a shared navigation component.


#### **2.1 Shared Component: Navigation Bar (Navbar)**


*   **Content:**
    *   Application Title/Logo: "PoFastType" on the far left.
    *   Navigation Links: "Game", "Leaderboard".
    *   Conditional Elements on the far right:
        *   **If User is Logged Out:** A "Login" button.
        *   **If User is Logged In:** A link to their "Dashboard" (displaying their username) and a "Logout" button.
*   **Functionality:**
    *   Provides consistent navigation across all pages of the application.
    *   The "Login" button initiates the Azure Entra ID authentication flow.
    *   The "Logout" button clears the user's session and updates the Navbar to the logged-out state.
    *   Links navigate the user to the corresponding Blazor pages without a full page reload.


#### **2.2 Page 1: The Game Page (`/` or `/game`)**


This is the core of the application and has several states.


*   **State 1: Pre-Game (Initial Load)**
    *   **Content:** A large, prominent "Start New Game" button.
    *   **Functionality:** Clicking the button transitions the page to the "Loading" state.


*   **State 2: Loading**
    *   **Content:** The "Start New Game" button is replaced by a loading spinner and a text message: *"Generating your unique text with AI..."*
    *   **Functionality:** The Blazor client makes an API call to `GET /api/game/text`. The UI remains in this state until the API returns the text.


*   **State 3: Countdown**
    *   **Content:**
        *   **Left Pane:** The full, ~400-word text received from the API is displayed. The text is static and read-only.
        *   **Right Pane:** A large, empty text input area is displayed, which is currently disabled.
        *   **Center/Overlay:** A large countdown timer displays "3", then "2", then "1".
    *   **Functionality:** This state lasts for three seconds to allow the user to read the first few words of the text before the test begins.


*   **State 4: In-Progress**
    *   **Content:** The countdown disappears. The text input area on the right is now enabled and focused. A 60-second timer is displayed prominently on the page, counting down.
    *   **Functionality:** The timer starts. The application records every keystroke the user makes in the input area. There is no visual feedback (no red highlights, no cursor blocking) for incorrect characters during this phase to maintain typing flow.


*   **State 5: Post-Game / Results**
    *   **Content:**
        *   The timer stops and disappears. The text input area is disabled.
        *   In the right pane, the user's typed text is now analyzed. Any incorrect characters or words are highlighted with a red background.
        *   A new "Results Card" component appears, displaying the following calculated statistics:
            *   **Net WPM (Primary Score)**
            *   Accuracy (%)
            *   Gross WPM
            *   Correct / Incorrect Characters
            *   Burst Speed (Peak WPM)
            *   Longest Streak (Consecutive correct characters)
        *   A "Try Again" button is displayed.
    *   **Functionality:**
        *   All calculations are performed on the client side.
        *   If the user is logged in, the client sends the results (specifically Net WPM, accuracy, and problem key data) to the backend via a `POST /api/scores` request.
        *   Clicking "Try Again" resets the page to the "Pre-Game" state.


#### **2.3 Page 2: The Leaderboard Page (`/leaderboard`)**


*   **Content:**
    *   A page title: "All-Time Top 10".
    *   A table with three columns: `Rank`, `Username`, `Net WPM`.
    *   The table will be populated with the top 10 scores.
*   **Functionality:**
    *   On page load, the client makes an API call to `GET /api/leaderboard`.
    *   The returned data is then rendered in the table.
    *   The page is public and accessible to everyone, whether logged in or not.


#### **2.4 Page 3: The User Dashboard Page (`/dashboard`)**


This page is only accessible to logged-in users.


*   **Content:**
    *   A page title: "Your Progress Dashboard".
    *   **Component 1: Performance Chart:** A line chart visualizing the user's performance over time.
        *   **X-Axis:** Date of Test.
        *   **Y-Axis:** WPM / Accuracy %.
        *   **Series:** Two lines will be plotted: one for `Net WPM` and one for `Accuracy`.
    *   **Component 2: Problem Keys Analysis:** A simple table or list that shows the user's most common typing errors.
        *   **Columns:** `Intended Key`, `Actual Key Typed`, `Frequency`.
        *   Example Row: 'e', 'r', '15 times'.
*   **Functionality:**
    *   On page load, the client makes an API call to `GET /api/me/stats`.
    *   The historical data returned from the API is used to populate both the chart and the problem keys table.


---


### 3. Backend API Specification (.NET Core Web API)


**Base URL:** `/api`


**Authentication:** Uses JWT Bearer tokens provided by Azure Entra ID. Endpoints marked as `[Authenticated]` require a valid token.


| Endpoint                 | Method | Auth             | Description                                                                                                                                                             |
| ------------------------ | ------ | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET /game/text`         | GET    | Public           | Fetches a new, unique text snippet for a game. It calls the Azure OpenAI service, waits for the response, and returns the generated text.                                |
| `POST /scores`           | POST   | [Authenticated]  | Submits the results of a completed game for a logged-in user. The server saves the results to Azure Table Storage.                                                    |
| `GET /leaderboard`       | GET    | Public           | Retrieves the 10 highest Net WPM scores from Azure Table Storage and returns them for display on the leaderboard page.                                                  |
| `GET /me/stats`          | GET    | [Authenticated]  | Retrieves the complete game history for the currently authenticated user to populate their personal dashboard (performance chart and problem key analysis).             |


---


### 4. Data Models & Storage (Azure Table Storage)


A single table, `GameResults`, will be used to store all necessary data.


**Table Name:** `GameResults`


**Entity Schema:**


| Property Name      | Data Type | Description                                                                                                                                        | Key Type         |
| ------------------ | --------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------- |
| `PartitionKey`     | string    | The `UserId` of the user who completed the game (from Azure Entra ID). This allows for efficient querying of a specific user's entire game history. | Partition Key    |
| `RowKey`           | string    | A reverse-ticked timestamp of the game completion time (e.g., `DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks`). This ensures newest games are first. | Row Key          |
| `Username`         | string    | The display name of the user at the time of the game. Stored for easy leaderboard retrieval.                                                         | Property         |
| `NetWPM`           | double    | The calculated Net Words Per Minute. This is the primary score and will be indexed for querying.                                                   | Property (Indexed) |
| `Accuracy`         | double    | The calculated accuracy percentage (e.g., 98.5).                                                                                                   | Property         |
| `GrossWPM`         | double    | The raw, uncorrected WPM.                                                                                                                          | Property         |
| `ProblemKeysJson`  | string    | A JSON-serialized string representing a dictionary of the user's errors for that game. E.g., `{"e": {"r": 5, "w": 2}, "t": {"g": 3}}`.                | Property         |
| `Timestamp`        | DateTime  | The exact UTC timestamp when the game was completed.                                                                                               | Property         |


---


### 5. The Complete 10-Step Implementation Plan for the Coding LLM


**Goal:** Build the PoFastType application by following these steps sequentially.


**Step 1: Project Setup and Configuration**
*   Create a new Blazor WebAssembly solution with an ASP.NET Core hosted backend. Name the projects `PoFastType.Client`, `PoFastType.Server`, and `PoFastType.Shared`.
*   In the `.Server` project, add NuGet packages for `Azure.Data.Tables` and the Azure OpenAI SDK (`Azure.AI.OpenAI`).
*   In the `.Client` project, add a package for a charting library (e.g., Chart.js for Blazor).
*   Set up `appsettings.json` in the `.Server` project with placeholders for Azure Table Storage Connection String, Table Name, Azure OpenAI Endpoint, and API Key.


**Step 2: Implement Azure Entra ID Authentication**
*   Configure Azure Entra ID for the application, both on the `.Server` (for API authentication) and `.Client` (for user login/logout).
*   Implement the login/logout flow in the Blazor client.
*   Update the `Navbar` component to be dynamic, showing "Login" or "Dashboard/Logout" based on authentication state. Secure the `/dashboard` route so it's only accessible to authenticated users.


**Step 3: Build the Backend API Endpoints (Data Models)**
*   In the `.Server` project, create the controllers and methods for the four API endpoints defined in the specification (`/game/text`, `/scores`, `/leaderboard`, `/me/stats`).
*   Implement the data model for `GameResult` in the `.Shared` project so it can be used by both client and server.
*   Implement the `[Authorize]` attribute on the `POST /scores` and `GET /me/stats` endpoints.


**Step 4: Implement the Text Generation Service**
*   In the `.Server` project, create a service class responsible for communicating with Azure OpenAI.
*   Implement the logic within the `GET /api/game/text` endpoint to call this service. The service should use the configured prompt to generate ~400 words of text and return it as a string.


**Step 5: Build the Static Game UI in Blazor**
*   Create the `GamePage.razor` component.
*   Lay out the UI with the left pane for text display, the right pane for the text input, and placeholders for the timer and results card.
*   Add the "Start New Game" button. Do not implement game logic yet.


**Step 6: Implement the Full Client-Side Game Loop**
*   Implement the state machine logic on the `GamePage`: `Pre-Game`, `Loading`, `Countdown`, `In-Progress`, `Post-Game`.
*   On "Start" click, call the `GET /api/game/text` endpoint and display the loading spinner.
*   Once text is received, start the 3-second countdown.
*   After the countdown, start the 60-second timer and enable the text input.
*   When the timer ends, disable the input.


**Step 7: Implement Scoring and Results Display**
*   After the game ends, implement the client-side logic to compare the source text and the user's input.
*   Calculate all required stats: Net WPM, Gross WPM, Accuracy, Correct/Incorrect characters, Burst Speed, and Longest Streak.
*   Identify and count the problem keys.
*   Display all these stats in the "Results Card" component.
*   Implement the logic to highlight incorrect text in the user's input box.


**Step 8: Implement Score Persistence and the Leaderboard**
*   **Backend:** Fully implement the logic for the `POST /api/scores` and `GET /api/leaderboard` endpoints. The `POST` endpoint will save a `GameResult` entity to Azure Table Storage. The `GET` endpoint will query the table, order by `NetWPM` descending, take the top 10, and return the results.
*   **Frontend:**
    *   On the `GamePage`, if the user is logged in, send the calculated score to the `POST /api/scores` endpoint after the game.
    *   Create the `LeaderboardPage.razor` component. On load, it should call `GET /api/leaderboard` and display the data in a table.


**Step 9: Implement the User Dashboard**
*   **Backend:** Fully implement the logic for the `GET /api/me/stats` endpoint. It should query the `GameResults` table using the authenticated user's ID as the `PartitionKey` and return their entire game history.
*   **Frontend:**
    *   Create the `DashboardPage.razor` component.
    *   On load, call `GET /api/me/stats`.
    *   Use the returned data to configure and render the performance line chart.
    *   Process the `ProblemKeysJson` from all games to aggregate and display the user's most common errors in the "Problem Keys Analysis" table.


**Step 10: Final Polish and Error Handling**
*   Add comprehensive error handling. What happens if the OpenAI API call fails? What if the database connection fails? Display user-friendly error messages on the client.
*   Ensure the application is visually appealing and responsive on different screen sizes.
*   Perform a final review of all requirements to ensure they have been met. Add comments to the code where logic is complex.


- use dotnet watch instead of dotnet run so I can use hot reload