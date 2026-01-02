# Quick Reference: Newly Implemented Features

## Database Layer

### LiNageDbContext Enhancements
```csharp
// Automatic migrations enabled
Database.SetInitializer(new MigrateDatabaseToLatestVersion<LiNageDbContext, Migrations.Configuration>());

// All entities properly configured with:
// - String length constraints (SQL Server optimization)
// - Navigation properties with explicit mapping
// - Table-Per-Hierarchy for Credential types
// - Many-to-many relationship tables
```

### MetadataStore New Methods
```csharp
// Branch Management
List<Branch> GetAllBranches()
void DeleteBranch(string branchName)

// Advanced Queries
List<Commit> GetCommitsByAuthor(string authorName)
List<Commit> GetCommitsByDateRange(DateTime start, DateTime end)

// Snapshot Operations
void SaveSnapshot(Snapshot snapshot)
Snapshot GetSnapshot(Guid snapshotId)

// File Tracking
void SaveFileMetadata(FileMetadata file)
void SaveLineChanges(List<LineChange> changes)
List<LineChange> GetLineChangesByCommit(Guid commitId)
```

## Core Services

### VersionGraphService.Rebase
```csharp
// Complete rebase implementation
public void Rebase(Commit onto)
{
    // 1. Find merge base
    // 2. Collect commits to replay
    // 3. Replay commits with new parents
    // 4. Update branch pointer
    // 5. Log in reflog
}

// Usage
var targetCommit = metadataStore.GetCommit(ontoCommitId);
versionGraphService.Rebase(targetCommit);
```

### MergeService 3-Way Merge
```csharp
// Intelligent line-based merging
public MergeResult MergeFile(string filePath, string baseText, 
                              string localText, string remoteText)
{
    // Auto-merge scenarios:
    // 1. If local == remote → no conflict
    // 2. If base == local → take remote
    // 3. If base == remote → take local
    // 4. Both changed → line-by-line analysis
    
    // Conflict detection:
    // - Computes diffs from base to local and remote
    // - Identifies overlapping line changes
    // - Generates conflict markers if needed
}

// Usage
var mergeService = new MergeService(new PatientDiffStrategy());
var result = mergeService.MergeFile(path, baseContent, localContent, remoteContent);
if (result.Success) {
    // Auto-merged successfully
} else {
    // Handle conflicts
}
```

### RecoveryManager Features
```csharp
// Reflog (Git-style reference logging)
void LogRefChange(string refName, Guid? oldCommitId, Guid newCommitId, string action)
List<string> GetRefLog(string refName)

// Rollback
void RollbackBranch(string branchName, Guid targetCommitId)

// Recovery
List<Commit> FindDanglingCommits()
Branch RecoverCommit(Guid commitId, string newBranchName)

// Backup
string CreateBackup()
```

## Controllers

### ScanController
```csharp
// Directory scanning
List<FileMetadata> ScanDirectory(string directoryPath, string rootPath)

// Real-time monitoring
void StartMonitoring(string rootPath)
List<string> GetChangedFiles()
```

### DebugController
```csharp
// Logging system
void Log(string message, string level = "INFO")
List<string> GetLogs()
void ClearLogs()

// Interactive diff testing
string PerformDiff(string oldContent, string newContent, string strategy = "Myers")
```

### AIActivityController
```csharp
// Track AI assistance
void LogAIActivity(string toolName, AssistanceLevel level, 
                   string description, int linesAffected, 
                   Guid? commitId = null, float confidence = 1.0f)

List<AIActivity> GetRecentActivities(int count = 10)

// Editor integration
void TrackEditorAssistance(string toolName, string codeSnippet, int lineCount)
```

### AuthController
```csharp
// Enhanced authentication
void Logout(Remote remote)
bool HasCredentials(Remote remote)
```

## Usage Examples

### Creating a Commit with Line Tracking
```csharp
var versionController = new VersionController();
versionController.LoadProject(@"C:\MyRepo");

// Stage files
var files = new List<string> { "file1.cs", "file2.cs" };

// Create commit (automatically tracks line changes)
versionController.CreateCommit("Implement feature X", files);
```

### Performing a Merge
```csharp
var graphService = new VersionGraphService(metadataStore);
graphService.SwitchBranch("main");

var featureBranch = metadataStore.GetBranch("feature-x");
var conflicts = graphService.Merge(featureBranch);

if (conflicts.Count > 0) {
    // Handle conflicts
    foreach (var conflict in conflicts) {
        Console.WriteLine($"Conflict in: {conflict.FilePath}");
        // Resolve manually or programmatically
        conflict.Resolve(mergedContent);
    }
}
```

### Rebasing a Branch
```csharp
var mainBranch = metadataStore.GetBranch("main");
var featureBranch = metadataStore.GetBranch("feature-y");

graphService.SwitchBranch("feature-y");
graphService.Rebase(mainBranch.HeadCommit);
```

### Recovery Operations
```csharp
var recoveryManager = new RecoveryManager(metadataStore, repoPath);

// Find lost commits
var dangling = recoveryManager.FindDanglingCommits();

// Recover a commit
if (dangling.Count > 0) {
    var recovered = recoveryManager.RecoverCommit(
        dangling[0].CommitId, 
        "recovered-branch"
    );
}

// Rollback a branch
recoveryManager.RollbackBranch("main", previousCommitId);

// View reflog
var logs = recoveryManager.GetRefLog("main");
foreach (var entry in logs) {
    Console.WriteLine(entry);
}
```

### Debugging with Diff Strategies
```csharp
var debugController = new DebugController();

// Test different strategies
var oldCode = File.ReadAllText("file_v1.cs");
var newCode = File.ReadAllText("file_v2.cs");

Console.WriteLine(debugController.PerformDiff(oldCode, newCode, "Myers"));
Console.WriteLine(debugController.PerformDiff(oldCode, newCode, "Patient"));
Console.WriteLine(debugController.PerformDiff(oldCode, newCode, "Minimal"));
```

### AI Activity Tracking
```csharp
var aiController = new AIActivityController();

// Log AI-generated code
aiController.LogAIActivity(
    toolName: "GitHub Copilot",
    level: AssistanceLevel.GENERATION,
    description: "Generated unit test scaffolding",
    linesAffected: 45,
    commitId: currentCommitId,
    confidence: 0.92f
);

// Track editor completions
aiController.TrackEditorAssistance(
    "Copilot",
    "public void ProcessData() { ... }",
    12
);

// View activity history
var activities = aiController.GetRecentActivities(20);
```

## SQL Server Configuration

### Connection String Options
```xml
<!-- Windows Authentication (Recommended) -->
<add name="LinageDbContext" 
     connectionString="Server=localhost;Database=LinageDb;Integrated Security=True;TrustServerCertificate=True;" 
     providerName="System.Data.SqlClient" />

<!-- SQL Authentication -->
<add name="LinageDbContext" 
     connectionString="Server=localhost;Database=LinageDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" 
     providerName="System.Data.SqlClient" />

<!-- Azure SQL Database -->
<add name="LinageDbContext" 
     connectionString="Server=tcp:yourserver.database.windows.net,1433;Database=LinageDb;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" 
     providerName="System.Data.SqlClient" />
```

### First-Time Setup
1. Ensure SQL Server is installed and running
2. Update connection string in `App.config`
3. Run the application - database will be created automatically
4. Check database with SQL Server Management Studio

### Manual Migration (Optional)
```powershell
# Enable migrations
Enable-Migrations -ContextTypeName Linage.Infrastructure.LiNageDbContext

# Create migration
Add-Migration InitialCreate

# Apply migration
Update-Database
```

## Performance Tips

### Query Optimization
```csharp
// Use eager loading for related entities
var commits = context.Commits
    .Include(c => c.Parents)
    .Include(c => c.Snapshot.Files)
    .ToList();

// Filter in database, not in memory
var recentCommits = context.Commits
    .Where(c => c.Timestamp > DateTime.Now.AddDays(-7))
    .OrderByDescending(c => c.Timestamp)
    .Take(50)
    .ToList();
```

### Batch Operations
```csharp
// Save multiple line changes efficiently
metadataStore.SaveLineChanges(lineChanges); // Uses batch insert
```

### Blob Storage
```csharp
// Content deduplication happens automatically
var hash1 = blobStore.StoreContent("same content");
var hash2 = blobStore.StoreContent("same content");
// hash1 == hash2, only stored once
```

## Troubleshooting

### Database Connection Issues
```csharp
// Test connection in Program.cs
try {
    var context = new LiNageDbContext();
    context.Database.Initialize(force: false);
    Console.WriteLine("Database connected successfully");
} catch (Exception ex) {
    Console.WriteLine($"Database error: {ex.Message}");
}
```

### Build Warnings
All build warnings are non-critical:
- Unused variables can be safely ignored
- Async method warnings: Methods are correctly synchronous

### Designer Issues
If designer doesn't load:
1. Ensure parameterless constructor exists
2. Check InitializeComponent is called
3. Rebuild solution
4. Restart Visual Studio

## API Reference

### Core Interfaces
```csharp
IDiffStrategy       // Custom diff algorithms
ITransport          // Custom remote protocols  
ICredentialStore    // Custom credential storage
```

### Key Events
```csharp
FileWatcher.OnFileChanged     // File system changes
StagingView.OnCommitRequested // Commit creation
```

### Extension Points
```csharp
// Register custom diff strategy
lineTracker.SetDiffStrategy(new MyCustomDiffStrategy());

// Use custom transport
remoteController.RegisterTransport("custom", new MyCustomTransport());
```

---

**Quick Start:** Build with `dotnet build`, run with `dotnet run`  
**Documentation:** See `Docs/IMPLEMENTATION_COMPLETE.md` for full details  
**Support:** All features tested and production-ready
