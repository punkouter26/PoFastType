# PoFastType Architecture Diagrams

This folder contains Mermaid diagram files and their generated SVG outputs that document the architecture and design of the PoFastType typing test application.

## 📋 Diagram Files

### 1. **Project Dependency Diagram** (`project-dependency-diagram.mmd`)
**Purpose:** Visualizes how .NET projects (.csproj files), APIs, and databases are interconnected.

- Shows the relationship between Api, Client, Shared, and Tests projects
- Illustrates external dependencies (Azure Table Storage, Azurite)
- Demonstrates the hosting relationship between API and Client
- Supports modularization and refactoring decisions

### 2. **Class Diagram for Domain Entities** (`class-diagram-domain-entities.mmd`)
**Purpose:** Models core business objects, their properties, methods, and relationships.

- Documents the domain model including GameResult, UserProfile, UserIdentity
- Shows relationships between entities
- Illustrates Azure Table Storage integration (ITableEntity)
- Supports domain-driven design and maintainability

### 3. **Sequence Diagram for API Calls** (`sequence-diagram-api-calls.mmd`)
**Purpose:** Traces request flow across frontend, API, and backend services for typing game features.

- Shows the complete flow from game start to result submission
- Illustrates error handling scenarios
- Documents service interactions and dependencies
- Valuable for debugging and performance optimization

### 4. **Flowchart for Use Case** (`flowchart-use-case.mmd`)
**Purpose:** Outlines the logical flow and decision points of the typing game user story.

- Maps the complete user journey from game start to completion
- Shows decision points and alternative paths
- Includes error handling and retry scenarios
- Useful for planning, QA, and user experience optimization

### 5. **Component Hierarchy Diagram** (`component-hierarchy-diagram.mmd`)
**Purpose:** Provides a tree-like view of how Blazor components are nested within pages and layouts.

- Shows the structure of the Blazor WebAssembly application
- Documents component relationships and hierarchies
- Breaks down complex pages into sub-components
- Handy for layout planning and component organization

## 🛠️ Generating SVG Files

To convert the Mermaid diagrams to SVG format, use the provided PowerShell script:

```powershell
# Navigate to the Diagrams folder
cd Diagrams

# Run the conversion script
.\generate-svg.ps1

# For help and options
.\generate-svg.ps1 -Help
```

### Prerequisites
1. **Node.js** - Download from [nodejs.org](https://nodejs.org/)
2. **Mermaid CLI** - Install globally:
   ```bash
   npm install -g @mermaid-js/mermaid-cli
   ```

## 📁 File Structure
```
Diagrams/
├── project-dependency-diagram.mmd       # Source Mermaid file
├── project-dependency-diagram.svg       # Generated SVG
├── class-diagram-domain-entities.mmd    # Source Mermaid file
├── class-diagram-domain-entities.svg    # Generated SVG
├── sequence-diagram-api-calls.mmd       # Source Mermaid file
├── sequence-diagram-api-calls.svg       # Generated SVG
├── flowchart-use-case.mmd              # Source Mermaid file
├── flowchart-use-case.svg              # Generated SVG
├── component-hierarchy-diagram.mmd      # Source Mermaid file
├── component-hierarchy-diagram.svg      # Generated SVG
├── generate-svg.ps1                    # PowerShell conversion script
└── README.md                           # This file
```

## 🎯 Usage in Documentation

These diagrams can be embedded in:
- **README.md** files for quick architecture overview
- **Technical documentation** for detailed system design
- **Pull requests** to illustrate proposed changes
- **Onboarding materials** for new team members
- **Architecture decision records (ADRs)**

### Embedding in Markdown
```markdown
![Project Dependencies](Diagrams/project-dependency-diagram.svg)
```

### Embedding in HTML
```html
<img src="Diagrams/project-dependency-diagram.svg" alt="Project Dependencies" />
```

## 🔄 Updating Diagrams

1. Edit the `.mmd` source files using any text editor
2. Test your changes using online Mermaid editors like [mermaid.live](https://mermaid.live/)
3. Run `.\generate-svg.ps1` to regenerate the SVG files
4. Commit both the `.mmd` and `.svg` files to version control

## 📚 Mermaid Syntax Reference

- [Mermaid Documentation](https://mermaid-js.github.io/mermaid/)
- [Flowchart Syntax](https://mermaid-js.github.io/mermaid/#/flowchart)
- [Sequence Diagram Syntax](https://mermaid-js.github.io/mermaid/#/sequenceDiagram)
- [Class Diagram Syntax](https://mermaid-js.github.io/mermaid/#/classDiagram)

---

*Generated as part of PoFastType Phase 3 Documentation*
