# PoFastType: AI-Powered Typing Game

A modern, web-based typing game designed to test and improve your typing speed and accuracy using AI-generated content. Unlike traditional typing tutors that rely on static, repetitive text blocks, PoFastType leverages Azure OpenAI to generate unique, approximately 400-word paragraphs for every single test, ensuring fresh challenges and preventing rote memorization.

## Features

- **AI-Generated Content**: Real-time content generation using Azure OpenAI for unique typing tests
- **5-Second Timed Tests**: Fast-paced typing challenges with no real-time error correction to maintain flow
- **Comprehensive Analytics**: 
  - Net WPM (primary score)
  - Accuracy percentage
  - Gross WPM
  - Correct/Incorrect character counts
  - Burst speed (peak WPM)
  - Longest error-free streak
  - Problem key analysis
- **User Authentication**: Azure Entra ID integration for personalized experience
- **Global Leaderboard**: Top 10 all-time Net WPM scores for competitive play
- **Personal Dashboard**: 
  - Performance tracking with charts over time
  - Problem key identification showing most frequent typing errors
- **Responsive Design**: Modern, retro-themed interface optimized for all devices

## Technology Stack

- **Frontend**: Blazor WebAssembly with Chart.js integration
- **Backend**: .NET Core Web API
- **Authentication**: Azure Entra ID with development mock authentication
- **Data Storage**: Azure Table Storage
- **AI Content**: Azure OpenAI service integration
- **Local Development**: Azurite storage emulator

## Prerequisites

- .NET 9.0 SDK
- Node.js (for Mermaid CLI diagram generation)
- Azure subscription with:
  - Azure Table Storage
  - Azure Entra ID (Azure Active Directory)
  - Azure OpenAI service (optional - uses hardcoded text in development)
- Azurite (for local development storage emulation)

## Quick Start

### 1. Clone and Build
```bash
git clone <repository-url>
cd PoFastType_Cursor
dotnet build
```

### 2. Start Local Storage Emulator
```bash
# Start Azurite in the background
azurite --silent --location AzuriteConfig --debug AzuriteConfig/debug.log
```

### 3. Run the Application
```bash
# Navigate to API project and run
cd PoFastType.Api
dotnet run

# The application will be available at:
# - Blazor Client: https://localhost:5001
# - API: https://localhost:5001/swagger (Swagger UI)
# - API Health Check: https://localhost:5001/api/diag/health
```

## Development Setup

### Local Development Configuration

The application is pre-configured for local development with:
- **Mock Authentication**: Automatically signed in as "Test User" for development
- **Azurite Storage**: Local Azure Table Storage emulation
- **Hardcoded Text**: Uses curated text content instead of Azure OpenAI
- **HTTPS**: Configured for HTTPS on port 5001

### Azure Services Configuration (Production)

#### Azure Table Storage Setup
1. Create a storage account in your resource group
2. Update `appsettings.json` with your connection string:
   ```json
   {
     "AzureTableStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
       "TableName": "PoFastTypeGameResults"
     }
   }
   ```

#### Azure Entra ID Setup
1. Register a new application in Azure Entra ID
2. Configure redirect URIs for your application
3. Update `appsettings.json`:
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "Domain": "your-domain.onmicrosoft.com",
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id"
     }
   }
   ```

#### Azure OpenAI Setup (Optional)
1. Create an Azure OpenAI resource
2. Deploy a text generation model (e.g., GPT-3.5-turbo)
3. Update configuration with endpoint and API key

## Project Structure

```
PoFastType_Cursor/
├── PoFastType.Api/           # .NET Core Web API backend
│   ├── Controllers/          # API controllers (Game, Scores, User, Diagnostics)
│   ├── Services/             # Business logic services
│   ├── Repositories/         # Data access layer
│   └── Properties/           # Launch settings and configuration
├── PoFastType.Client/        # Blazor WebAssembly frontend
│   ├── Components/           # Reusable UI components
│   ├── Layout/               # Page layouts
│   ├── Pages/                # Razor pages (Home, Game, Leaderboard, Dashboard)
│   └── Services/             # Client-side services
├── PoFastType.Shared/        # Shared models and DTOs
│   └── Models/               # Data models (GameResult, UserProfile, etc.)
├── PoFastType.Tests/         # Unit and integration tests
├── Diagram/                  # Mermaid diagrams and generated SVGs
│   ├── *.mmd                 # Mermaid diagram source files
│   │   ├── flowchart.mmd     # Application flow and user interactions
│   │   ├── sequence.mmd      # API call sequences and component interactions
│   │   ├── class.mmd         # Detailed class relationships and domain model
│   │   ├── class-simple.mmd  # Simplified component relationships
│   │   ├── er.mmd            # Database schema and relationships
│   │   ├── state.mmd         # Game state machine and transitions
│   │   └── dependencies.mmd  # Project dependencies and NuGet packages
│   └── *.svg                 # Generated SVG diagrams
├── infra/                    # Infrastructure as Code (Bicep files)
└── AzuriteConfig/            # Local Azurite storage emulator data
```

## API Endpoints

- `GET /api/game/text` - Get text content for typing test (uses hardcoded text in development)
- `POST /api/scores` - Submit game results (authenticated users only)
- `GET /api/scores/leaderboard` - Get top 10 Net WPM scores globally
- `GET /api/scores/me/stats` - Get authenticated user's complete game history
- `GET /api/user/identity` - Get current user identity information
- `GET /api/user/profile` - Get user profile details (authenticated)
- `GET /api/diag/health` - Health check endpoint

## Game Flow

1. **Pre-Game**: User clicks "Start New Game" button
2. **Loading**: System generates/fetches unique text content
3. **Countdown**: 3-second countdown while user reads first few words
4. **In-Progress**: 5-second timed typing test with no real-time error feedback
5. **Post-Game**: Results analysis with highlighting of errors and comprehensive statistics

## Diagrams

The project includes comprehensive Mermaid diagrams in the `Diagram/` folder:

- **Flowchart** (`flowchart.mmd/svg`): Application flow and user interactions
- **Sequence** (`sequence.mmd/svg`): API call sequences and component interactions  
- **Class** (`class.mmd/svg`): Detailed class relationships and domain model
- **Class Simple** (`class-simple.mmd/svg`): Simplified component relationships
- **Entity-Relationship** (`er.mmd/svg`): Database schema and relationships
- **State** (`state.mmd/svg`): Game state machine and transitions
- **Dependencies** (`dependencies.mmd/svg`): Project dependencies and NuGet packages

To regenerate diagrams:
```bash
# Install Mermaid CLI (if not already installed)
npm install -g @mermaid-js/mermaid-cli

# Convert specific diagram
npx @mermaid-js/mermaid-cli -i Diagram/flowchart.mmd -o Diagram/flowchart.svg

# Convert all diagrams
cd Diagram
npx @mermaid-js/mermaid-cli -i *.mmd
```

## Development Notes

- **Authentication**: Uses mock authentication in development (automatically signed in as "Test User")
- **Storage**: Uses Azurite for local Azure Table Storage emulation
- **Content**: Uses hardcoded text strategies in development instead of Azure OpenAI
- **CORS**: Configured to allow both HTTP (5000) and HTTPS (5001) localhost origins
- **Logging**: Configured for console and debug output in development

## Troubleshooting

### Common Issues

1. **Port Already in Use**: Kill any existing dotnet processes: `taskkill /f /im dotnet.exe`
2. **Azurite Connection**: Ensure Azurite is running: `azurite --silent --location AzuriteConfig`
3. **HTTPS Certificate**: Trust the development certificate: `dotnet dev-certs https --trust`
4. **Client App Not Loading**: Verify the API is running on https://localhost:5001

### Verification Steps

1. Check API health: `curl -k https://localhost:5001/api/diag/health`
2. Check user identity: `curl -k https://localhost:5001/api/user/identity`
3. Access Swagger UI: `https://localhost:5001/swagger`
4. Access Blazor app: `https://localhost:5001`

## Contributing

1. Follow the implementation steps outlined in the project specification
2. Ensure all tests pass before submitting changes
3. Follow SOLID principles and established patterns in the codebase
4. Update diagrams when making architectural changes
5. Maintain backward compatibility with existing API endpoints

## License

This project is licensed under the MIT License. 