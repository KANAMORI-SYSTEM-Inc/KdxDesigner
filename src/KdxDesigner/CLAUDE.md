# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KdxDesigner is a Windows application for automatic ladder program generation at KANAMORI SYSTEM Inc. It converts Access database data into CSV format mnemonic programs for PLC operations.

## Build and Development Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Architecture Overview

### Technology Stack
- **Framework**: .NET 8.0, WPF
- **Pattern**: MVVM with CommunityToolkit.Mvvm
- **Database**: Microsoft Access via OleDb
- **Key Libraries**:
  - Dapper (2.1.66) - Database access
  - CommunityToolkit.Mvvm (8.4.0) - MVVM implementation
  - System.Data.OleDb (9.0.4) - Access database connectivity

### Core Architecture

1. **Data Layer** (`Services/Access/`)
   - `IAccessRepository` interface defines all database operations
   - `AccessRepository` implements queries for Access database
   - All database operations go through this repository pattern

2. **Model Layer** (`Models/`)
   - Database table structures mapped to C# classes
   - Composite keys implemented using `[Key]` and `[Column(Order = n)]` attributes
   - Key entities: Process, ProcessDetail, Operation, CY (Cylinder)
   - MnemonicId values: 1=Process, 2=ProcessDetail, 3=Operation, 4=CY

3. **Service Layer** (`Services/`)
   - Business logic separated from ViewModels
   - Key services:
     - `MnemonicDeviceService` - Device management for each mnemonic type
     - `MnemonicTimerDeviceService` - Timer device management
     - `IOAddressService` - IO address resolution
     - `ErrorService` - Error handling and aggregation

4. **ViewModel Layer** (`ViewModels/`)
   - Uses `ObservableObject` and `[ObservableProperty]` attributes
   - Commands use `[RelayCommand]` attribute
   - Main entry point: `MainViewModel`

5. **View Layer** (`Views/`)
   - WPF XAML views
   - Entry point: `MainView.xaml`
   - Modal dialogs for editors (IO, Timer, Memory, etc.)

### Key Design Patterns

1. **Composite Primary Keys**
   - Many tables use composite keys instead of single ID columns
   - Example: IO table uses (Address, PlcId), MnemonicTimerDevice uses (MnemonicId, RecordId, TimerId)

2. **Intermediate Tables for Many-to-Many Relationships**
   - CylinderIO - links CY and IO tables
   - OperationIO - links Operation and IO tables

3. **Error Handling**
   - Graceful degradation when tables don't exist
   - Try-catch blocks return empty collections for missing tables

4. **Process Flow Connections**
   - ProcessDetailConnection: Normal connections between ProcessDetail nodes
   - ProcessDetailFinish: Finish condition connections (Ctrl+Shift+Click)
   - Finish conditions can be used with:
     - Period processes (期間工程)
     - Process OFF confirmation (工程OFF確認, CategoryId=13)

## Database Schema Considerations

- Tables may not exist until features are used
- Always check table existence before operations
- Migration scripts in `docs/` folder for schema changes

## UI Development Guidelines

- Node height in process flow: 40px (not 60px)
- Use ICollectionView for filtering large data sets
- Modal dialogs receive repository and parent ViewModel in constructor

### Loading Screen Implementation

**Overview:**
Process flow detail window implements an animated loading screen during initial data load.

**Implementation Details:**

1. **ViewModel (ProcessFlowDetailViewModel.cs)**
   ```csharp
   [ObservableProperty] private bool _isLoading = false;

   public async void LoadNodesAsync()
   {
       IsLoading = true;
       try
       {
           await LoadProcessDetailsInternal();
       }
       finally
       {
           IsLoading = false;
       }
   }
   ```

2. **View (ProcessFlowDetailWindow.xaml)**
   - Semi-transparent overlay with `Panel.ZIndex="1000"`
   - Animated spinning circle using `RotateTransform` and `Storyboard`
   - Visibility bound to `IsLoading` property via `BooleanToVisibilityConverter`

**IMPORTANT: WPF UI Thread Affinity**

⚠️ **DO NOT use `Task.Run()` for methods that manipulate ObservableCollections**

**Problem:**
```csharp
// ❌ WRONG - Causes cross-thread access violations
await Task.Run(async () => {
    await LoadProcessDetailsInternal();  // Manipulates ObservableCollections
});
```

**Reason:**
- WPF ObservableCollections must be accessed from the UI thread
- `Task.Run()` executes code on a background thread pool thread
- This causes "collection was modified from a different thread" errors

**Solution:**
```csharp
// ✅ CORRECT - Execute on UI thread with async/await
public async void LoadNodesAsync()
{
    IsLoading = true;
    try
    {
        await LoadProcessDetailsInternal();  // Uses async/await, not Task.Run
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Why This Works:**
- Repository methods (`GetProcessDetailsAsync()`, etc.) already use proper async/await
- When you `await` these methods, the UI thread is released to process other events
- Database operations happen asynchronously without blocking UI
- ObservableCollection modifications happen on the UI thread where they're safe
- The loading screen displays immediately because the UI thread remains responsive

**Key Takeaway:**
In WPF MVVM applications, prefer `async/await` over `Task.Run()` for data loading operations that update UI-bound collections. The async repository pattern already provides non-blocking behavior without requiring explicit background threading.

## Testing and Validation

```bash
# Run lint and type checking (if configured)
npm run lint
npm run typecheck

# For C# projects, these are typically configured in the project
dotnet build # Will show compilation errors and warnings
```

## Common Development Tasks

### Adding a New Feature Screen
1. Create View (.xaml) and code-behind (.xaml.cs) in Views/
2. Create ViewModel in ViewModels/ using ObservableObject
3. Add navigation command in MainViewModel
4. Add menu item in MainView.xaml

### Working with Composite Keys
- Use Dapper parameters in correct order matching composite key definition
- Include all key parts in WHERE clauses for updates/deletes

### Database Operations
- All database access through IAccessRepository
- Use Dapper for parameterized queries
- Handle OleDb connection properly with using statements

## Work Documentation Process

When completing major changes:

1. **Database schema changes**
2. **Large refactoring across multiple files**
3. **New feature implementations**
4. **Complex bug fixes**
5. **Data migrations**

Create documentation:
- Chat history: `docs/chat-history-[feature]-[date].md`
- Wiki summary: `docs/wiki-[feature].md`
- Include in git commit with descriptive message

## Important Notes from Copilot Instructions

- このプロジェクトでは、Accessファイルから内容、アクチュエータの動作を取得し、それをPLCで動作するCSVファイル形式のニモニックプログラムに変換することを目的としています
- プロンプトに対して不明点があれば、聞きやすいようにしてください
- 最新リポジトリを参照するようにしてください: https://github.com/HayatoShimada/KdxDesigner
- コードにはなるべくコメントを入れるようにしてください
- 適切な変数名や関数名を使用してください
- クラス化やメソッド化を行い、コードの可読性を高めてください
- コードの重複を避け、DRY原則に従ってください