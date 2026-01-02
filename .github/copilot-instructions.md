# Li'nage - AI Agent Instructions

## Project Overview
**Li'nage** is a line-level version control system built with C# .NET 8.0 that tracks individual line changes rather than file-level changes like Git. It features a Windows Forms IDE with visual commit graph, AI-assisted code tracking, pluggable diff algorithms, and multi-protocol authentication.

## Core Architecture (Four-Layer Clean Architecture)

### 1. Presentation Layer (`GUI/`)
Windows Forms with VS Code-inspired interface:
- **MainWindow.cs**: Primary shell with 3-pane layout (staging sidebar, tabbed editor area, terminal)
- **Views**: `FileExplorerView`, `StagingView`, `GitGraphView`, `EditorView`, `TerminalView`, `ConflictResolutionView`
- **Theme**: `ModernTheme` provides VS Code-like colors and custom controls

### 2. Controller Layer (`Controllers/`)
Orchestration between UI and business logic:
- **VersionController**: Primary controller managing commits, branches, project lifecycle
  - Dependency Injection root - initializes all services
  - `LoadProject()` initializes BlobStore, FileService, starts FileWatcher
  - `CreateCommit()` stages files, creates snapshots, updates DAG
- **RemoteController**: Push/pull operations with protocol abstraction
- **AuthController**: Credential management for remote operations
- Supporting: `SyncController`, `IndexController`, `ScanController`, `DebugController`, `AIActivityController`

### 3. Business Logic Layer (`Core/`)
Pure domain logic implementing VCS functionality:

**Key Services:**
- **VersionGraphService**: Manages commit DAG (Directed Acyclic Graph)
  - In-memory cache of commits/branches for performance (`_commitCache`, `_branchCache`)
  - `AddCommit()`, `CreateBranch()`, `SwitchBranch()`, `FindCommonAncestor()`
  - `Merge()`: Three-way merge with blob content retrieval
  - `Rebase()`: Linear history rewriting
  
- **LineTracker**: Generates line-level changes using Strategy pattern
  - Uses `IDiffStrategy` interface (Myers/Patient/Minimal in `Core/Diff/`)
  - `GenerateLineChanges()`: Returns `LineChange` objects with ADDED/DELETED/MODIFIED types
  - Each line tracked with SHA-256 hash (`OldHash`, `NewHash`)
  
- **ChangeDetector**: Scans working directory for modifications
- **MergeService**: Three-way merge logic with conflict detection
- **GitImportService**: Imports existing Git repos (full or quick import via LibGit2Sharp)
- **AIActivityService**: Tracks AI-assisted code contributions
- **RecoveryManager**: Snapshot-based rollback

**Domain Entities:**
- **Commit**: CommitId (Guid), CommitHash (SHA-256), Message, Author, Timestamp, Parents (List<Commit>), Snapshot
- **Branch**: BranchName, HeadCommit, IsActive
- **Snapshot**: Collection of FileMetadata representing project state at a commit
- **LineChange**: Individual line modification with OldHash/NewHash, ChangeType, LineNumber, Timestamp
- **Conflict**: Merge conflict representation

### 4. Infrastructure Layer (`Infrastructure/`)
External system access and persistence:
- **LiNageDbContext**: Entity Framework 6 DbContext using **SQL Server** (not SQLite)
  - Connection string: "name=LinageDbContext" in App.config
  - Auto-migration enabled: `MigrateDatabaseToLatestVersion<LiNageDbContext, Migrations.Configuration>()`
  - Startup validates SQL Server connectivity
  
- **MetadataStore**: Persistence layer for commits, branches, snapshots, file metadata, line changes, remotes
  - Async methods (`SaveCommitAsync`, `GetCommitAsync`) - prefer these to prevent UI blocking
  - Sync methods marked `[Obsolete]` for backward compatibility
  
- **FileService**: File I/O operations
  - `InitializeBlobStore(rootPath)`: Content-addressable storage in `.linage/objects/`
  - `SaveBlob()`, `LoadBlob()`: Store/retrieve file content by hash
  
- **HashService**: SHA-256 hashing for content integrity
- **CredentialStore**: Windows Credential Manager integration with AES-256 encryption
- **FileWatcher**: Monitors working directory changes
- **GitignoreParser**: Respects .gitignore patterns plus default patterns (`.git/`, `.linage/`, `bin/`, `obj/`)
- **Transport**: Protocol abstraction via `ITransport`
  - `LinageHttpTransport`: HTTP/HTTPS with Personal Access Token auth
  - `LinageSshTransport`: SSH key-based authentication

## Critical Patterns & Conventions

### Diff Algorithm Strategy Pattern
Pluggable diff algorithms in `Core/Diff/`:
```csharp
// Change strategy at runtime
lineTracker.SetDiffStrategy(new PatientDiffStrategy());
```
- **MyersDiffStrategy**: Default O(ND) general-purpose (balanced)
- **PatientDiffStrategy**: Optimized for long files with unique lines
- **MinimalDiffStrategy**: Fast O(N log N) approximation

### Database Operations
**ALWAYS use async methods** to prevent UI freezes:
```csharp
// CORRECT
await _metadataStore.SaveCommitAsync(commit).ConfigureAwait(false);

// INCORRECT - causes UI blocking
_metadataStore.SaveCommit(commit); // Marked [Obsolete]
```

### Dependency Injection & Initialization
**VersionController** is the DI root - initializes all services in correct order:
1. Create DbContext → MetadataStore → HashService → FileService
2. Create VersionGraphService, RemoteService, AuthService
3. `LoadProject(path)`:
   - Calls `FileService.InitializeBlobStore(path)` to setup `.linage/objects/`
   - Injects FileService into VersionGraphService via `SetFileService()`
   - Creates transports with correct path: `new LinageHttpTransport(AuthService, rootPath)`
   - Initializes ChangeDetector and starts FileWatcher

**MainWindow** creates VersionController then extracts services:
```csharp
_versionController = new VersionController();
_authController = new AuthController(_versionController.AuthService);
_remoteController = new RemoteController(httpTransport, sshTransport, _authController);
```

### Commit DAG Operations
Commits form a DAG with multiple parent support (merge commits):
```csharp
// Find merge base
var ancestor = graphService.FindCommonAncestor(localCommit, remoteCommit); // BFS-based

// Three-way merge
var result = graphService.Merge(sourceBranch, targetBranch); // Uses base/local/remote snapshots
```

### Git Integration
**LibGit2Sharp** used for Git interop:
- `GitImportService.FullImport(gitRepoPath)`: Converts entire Git repo to Li'nage
- `GitImportService.QuickImport(gitRepoPath)`: Only imports HEAD commit
- Creates `.linage/` directory parallel to `.git/`
- Maps Git commits to Li'nage commits with Guid CommitIds

## Development Workflows

### Building & Running
```bash
# Build solution
dotnet build li'nage.sln

# Run application (requires SQL Server running)
dotnet run --project Linage.csproj
```

**Prerequisites:**
- Windows 10/11 (Windows Forms target)
- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or full edition)
- Connection validated on startup - failure shows error dialog

### Testing Database Connectivity
SQL Server must be accessible before app launch. Connection string in App.config:
```xml
<connectionStrings>
  <add name="LinageDbContext" connectionString="..." providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Working with Branches
Default branch logic:
1. Try loading `main` branch
2. Fallback to `master` branch  
3. Use first available branch
4. If no branches exist, create `main` on first commit

### File Storage Structure
```
project-root/
├── .linage/
│   ├── objects/          # Content-addressable blob storage (by SHA-256 hash)
│   │   ├── ab/cd1234...  # First 2 chars of hash = directory
│   └── metadata.db       # SQL Server database (remote connection)
```

### Adding New Controllers
1. Create in `Controllers/` namespace
2. Inject required services via constructor
3. **Never** create new service instances - use shared services from VersionController
4. Follow async pattern: use `ConfigureAwait(false)` on all awaits

### Adding New GUI Views
1. Inherit from `Form` or `UserControl`
2. Use `ModernTheme` for consistent styling
3. Update `MainWindow` to add view to appropriate panel/tab
4. Use `AsyncOperationHelper` for long-running operations to prevent UI blocking

## Common Pitfalls

1. **DO NOT create multiple DbContext instances** - use the shared one from MetadataStore
2. **DO NOT call sync database methods** - use async variants with `ConfigureAwait(false)`
3. **DO NOT initialize BlobStore before calling `LoadProject()`** - FileService needs project path
4. **DO NOT create new transport instances** - RemoteController manages these
5. **DO NOT forget to call `StartMonitoring()` on ChangeDetector** after LoadProject
6. **DO NOT assume branches exist** - check with `GetAllBranches()` first
7. **DO NOT directly modify UI from background threads** - use `Invoke()` or `AsyncOperationHelper`

## Key File References
- Entry point: [Program.cs](Program.cs) → `Application.Run(new MainWindow())`
- DI Root: [Controllers/VersionController.cs](Controllers/VersionController.cs#L60-L80)
- Database Schema: [Infrastructure/LiNageDbContext.cs](Infrastructure/LiNageDbContext.cs)
- Diff Algorithms: [Core/Diff/](Core/Diff/)
- Git Import: [Core/GitImportService.cs](Core/GitImportService.cs)
- UI Layout: [GUI/MainWindow.cs](GUI/MainWindow.cs#L80-L110)

## External Dependencies
- **EntityFramework 6.4.4**: SQL Server ORM
- **LibGit2Sharp 0.30.0**: Git repository interop
- **System.Data.SqlClient**: SQL Server connectivity
- Windows Forms (part of .NET runtime)

## Current Development Status
- **Phase 1 (Foundation)**: ✅ Complete
- **Phase 2 (Core VCS Features)**: ✅ Complete  
- **Phase 3 (Auth & Remote)**: 🚧 In Progress
  - Basic authentication framework implemented
  - Transport abstraction ready
  - Push/Pull operations need completion
- **Phase 4 (Advanced Features)**: ⏳ Planned (AI tracking, advanced merging)

## License
GNU General Public License v3.0 - Copyright © 2025 Hanan Bhatti
