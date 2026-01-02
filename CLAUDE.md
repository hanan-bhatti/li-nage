# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Li'nage** is a line-level version control system (VCS) that tracks individual line changes rather than file-level changes. It provides granular blame capabilities, AI-assisted commit tracking, and supports multiple diff algorithms. The application features a Windows Forms GUI inspired by VS Code with integrated file explorer, staging, commit graph visualization, and terminal.

## Build & Development Commands

### Building the Project
```bash
# Build the solution
dotnet build li'nage.sln

# or build the project directly
dotnet build Linage.csproj

# Run the application
dotnet run --project Linage.csproj
```

**Important**: The project requires SQL Server connection. Ensure SQL Server is running and accessible before starting the application. Connection failures will show a startup error dialog.

## Architecture Overview

Li'nage follows a **Four-Layer Clean Architecture**:

### 1. Presentation Layer (`GUI/`)
Windows Forms-based UI with VS Code-inspired interface:
- `MainWindow.cs`: Main application shell with menu, activity bar, sidebar, editor tabs, terminal, and status bar
- `FileExplorerView`, `StagingView`, `GitGraphView`, `DebugView`: Sidebar views
- `EditorView`, `TerminalView`: Main content areas
- `ConflictResolutionView`, `MergeConflictView`: Merge conflict resolution UI
- `Theme/`: ModernTheme color scheme and custom controls

### 2. Controller Layer (`Controllers/`)
Orchestrates interaction between UI and business logic:
- `VersionController`: Primary controller managing commits, branches, and project lifecycle
- `AuthController`: Handles authentication for remote operations
- `RemoteController`: Manages push/pull operations
- `SyncController`: Synchronization logic
- `IndexController`, `ScanController`, `DebugController`: Supporting controllers

### 3. Business Logic Layer (`Core/`)
Pure domain logic implementing VCS functionality:

**Key Services:**
- `VersionGraphService`: Manages the commit DAG (Directed Acyclic Graph), branches, merge operations, and rebase
  - Maintains in-memory cache of commits and branches for performance
  - Implements three-way merge with blob content retrieval
  - Handles common ancestor finding and graph traversal

- `LineTracker`: Generates line-level changes using pluggable diff strategies
  - Uses `IDiffStrategy` interface (Myers, Patient, Minimal algorithms in `Core/Diff/`)
  - Produces `LineChange` objects with hash tracking for ADDED/DELETED/MODIFIED lines

- `ChangeDetector`: Scans working directory for modifications
- `MergeService`: Three-way merge logic with conflict detection
- `GitImportService`: Imports existing Git repositories (full or quick import)
- `AIActivityService`: Tracks AI-assisted code contributions
- `RecoveryManager`: Snapshot-based rollback functionality

**Domain Entities:**
- `Commit`: Contains CommitId, hash, message, author, timestamp, parent commits, and `Snapshot`
- `Branch`: Tracks branch name, HEAD commit, and history
- `Snapshot`: Represents project state at a commit (collection of `FileMetadata`)
- `LineChange`: Individual line modification with old/new hash and change type
- `Conflict`: Merge conflict representation

### 4. Infrastructure Layer (`Infrastructure/`)
External system access and persistence:
- `MetadataStore`: SQL Server persistence using Entity Framework 6, stores commits, branches, snapshots, file metadata, line changes, and remotes
- `LiNageDbContext`: EF DbContext for database operations with SQL Server
- `FileService`: File I/O operations and blob storage management
- `BlobStore`: Content-addressable storage for file versions
- `HashService`: SHA-256 hashing for content integrity
- `CredentialStore`: Windows Credential Manager integration with AES-256 encryption
- `FileWatcher`: Monitors working directory changes
- `GitignoreParser`: Respects .gitignore patterns
- Transport implementations:
  - `LinageHttpTransport`: HTTP/HTTPS with Personal Access Token auth
  - `LinageSshTransport`: SSH key-based authentication
  - `ITransport`: Interface for remote protocol abstraction

## Key Technical Details

### Diff Algorithm Strategy Pattern
The `LineTracker` uses the Strategy pattern for pluggable diff algorithms:
- `MyersDiffStrategy`: Default O(ND) general-purpose algorithm
- `PatientDiffStrategy`: Optimized for long files with unique lines
- `MinimalDiffStrategy`: Fast O(N log N) approximation

Change strategy via: `lineTracker.SetDiffStrategy(new PatientDiffStrategy())`

### Commit Graph & DAG Operations
- Commits form a DAG with support for multiple parents (merge commits)
- `VersionGraphService.FindCommonAncestor()`: BFS-based merge base calculation
- `VersionGraphService.Merge()`: Three-way merge using base/local/remote snapshots with blob content retrieval
- `VersionGraphService.Rebase()`: Linear history rewriting

### Metadata Storage
- Entity Framework 6 with Code First approach
- **SQL Server** database stores all version metadata (commits, branches, snapshots, file metadata, line changes, remotes)
- Blob storage (content-addressable) stores actual file contents by hash
- Commit hashes calculated via `Commit.CalculateHash()` for integrity

### Remote Operations
- Multi-protocol support: HTTP/HTTPS (PAT), SSH (key-based), OAuth (planned)
- `RemoteService` manages remote configurations
- Push/Pull operations via transport abstraction (`ITransport`)
- Authentication handled by `AuthenticationService` with secure credential storage

### Project Initialization
When loading a repository:
1. `VersionController.LoadProject(rootPath)` initializes blob store
2. `ChangeDetector` starts file monitoring
3. `GraphService` hydrates commit/branch cache from `MetadataStore`
4. Transport instances created with authentication context

### Important File Locations
- Blob store: Initialized at repository root via `FileService.InitializeBlobStore(rootPath)`
- Database: SQL Server connection managed by `LiNageDbContext` (connection string in context configuration)
- Credentials: Windows Credential Manager (`CredentialStore`)

## Development Considerations

### Clean Architecture Boundaries
- Controllers should not contain business logic - delegate to Core services
- Core layer must remain framework-agnostic (no UI, no EF dependencies)
- Infrastructure provides implementations of Core interfaces
- UI only interacts through Controllers

### Dependency Injection
- `VersionController` constructor sets up the entire dependency tree
- Services are injected into controllers
- `MainWindow` creates `VersionController` and accesses services via its properties

### Error Handling
- Controllers catch exceptions and display MessageBox errors to users
- Core services throw descriptive exceptions for invalid operations
- `DebugView.Log()` available for diagnostic logging
- SQL Server connection failures are caught at startup with user-friendly error dialog

### Async Operations
- Remote operations (Push/Pull/Clone) are async
- UI event handlers for remote operations use `async void` pattern
- Status updates via `UpdateStatus()` during long-running operations

## Current State & Migration Notes

- **Target Framework**: .NET Framework 4.8 (C# 7.3)
- **Migration in Progress**: Moving to .NET 7.0+
- **Database**: SQL Server (Entity Framework 6)
- **Dependencies**: EntityFramework 6.4.4, LibGit2Sharp 0.30.0, Newtonsoft.Json 13.0.3
- **RecoveryManager**: Two versions exist (`RecoveryManager.cs` and `RecoveryManager_New.cs`) - refactoring in progress

## Common Workflows

### Creating a Commit
1. User stages files in `StagingView`
2. `OnCommitRequested` event triggers `VersionController.CreateCommit(message, selectedFiles)`
3. `CreateCommit` creates `Snapshot` with `FileMetadata` from `FileService`
4. New `Commit` object created with hash, linked to parent, added to `GraphService`
5. Branch HEAD updated, persisted via `MetadataStore` to SQL Server

### Merging Branches
1. `VersionGraphService.Merge(sourceBranch)` called
2. Common ancestor found via `FindCommonAncestor()`
3. Three snapshots retrieved: base, local (current), remote (source)
4. For each file, blob content fetched from `FileService.GetContentByHash()`
5. `MergeService.MergeFile()` performs three-way merge
6. Conflicts returned for UI resolution in `ConflictResolutionView`

### Importing Git Repositories
1. User selects Git repo via File > Import Git Repository
2. `GitImportService` created via `VersionController.CreateGitImporter()`
3. Full import: `ImportRepository()` reads entire Git history via LibGit2Sharp
4. Quick import: `QuickImport()` imports only current HEAD state
5. Results displayed, repository loaded into Li'nage

## License
GNU General Public License v3.0
