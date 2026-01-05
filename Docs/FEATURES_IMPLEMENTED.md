# Branch Switching & Remote Management Features

## Overview
This document describes the newly implemented branch switching and remote management features in Li'nage.

## Branch Switching

### Features Implemented
1. **Branch Switching Dialog** - Interactive dialog to switch between branches
2. **Create New Branches** - Create and automatically switch to new branches
3. **Delete Branches** - Delete branches (with protection against deleting current branch)
4. **Visual Feedback** - Current branch is highlighted in the dialog

### How to Use

#### Switch Between Branches
1. Click the branch name in the status bar (bottom-left) **OR**
2. Use menu: **Remote** → *Branch selection from status bar*
3. Select a branch from the list and click "Switch"
4. UI will automatically update to show commits and files for the new branch

#### Create a New Branch
1. Open the branch switching dialog (click branch name in status bar)
2. Click "New Branch" button
3. Enter the new branch name
4. The new branch will be created and you'll automatically switch to it
5. Start making commits on the new branch

#### Delete a Branch
1. Open the branch switching dialog (click branch name in status bar)
2. Select the branch you want to delete
3. Click "Delete" button
4. Confirm the deletion
5. The branch will be removed from the database

### Protected Operations
- **Cannot delete current branch** - You must switch to another branch first before deleting
- **Confirmation dialogs** - Destructive operations require confirmation

### UI Updates
When switching branches, the following are automatically refreshed:
- Status bar shows current branch name
- Git graph updates to show commits from the new branch
- File staging view updates with files from the new branch HEAD commit
- Debug log records the branch switch action

---

## Remote Management

### Features Implemented
1. **View All Remotes** - See configured remotes with their URLs
2. **Add Remote** - Configure new remote repositories
3. **Remove Remote** - Delete remote configurations
4. **Set Default Remote** - Mark a remote as the default for push/pull operations

### How to Use

#### Access Remote Management
1. Use menu: **Remote** → **Manage Remotes...**
2. A dialog will open showing all configured remotes

#### Add a Remote
1. Click "Add Remote" button in the Manage Remotes dialog
2. Enter the remote name (e.g., "origin", "upstream")
3. Enter the remote URL (e.g., "https://github.com/user/repo.git")
4. Click OK to confirm
5. The remote will be saved to the database

#### View Remote Details
- Remote name and URL are displayed in the Manage Remotes dialog
- Default remote is marked with "(default)" indicator
- First remote added is automatically set as default

#### Remove a Remote
1. Select the remote from the list
2. Click "Remove" button
3. Confirm the deletion
4. The remote configuration will be deleted

#### Set Default Remote
1. Select a remote from the list
2. Click "Set as Default" button
3. This remote will now be used for default push/pull operations
4. Only one remote can be default at a time

### Menu Structure
```
Remote (Menu)
├── Manage Remotes...     [New Feature]
├── ─────────────────
├── Push                  [Existing]
└── Pull                  [Existing]
```

---

## Technical Implementation Details

### Architecture
- **MainWindow.cs**: UI dialogs and event handlers
- **VersionController**: Provides access to branch operations
- **RemoteService**: Manages remote configurations
- **MetadataStore**: Persists remotes and branches to SQL Server

### Key Methods Added

#### Branch Operations
```csharp
// In MainWindow
private async void OnBranches(object sender, EventArgs e)           // Branch switching dialog
private async Task SwitchBranchAsync(string branchName)             // Switch to branch
private async Task CreateNewBranchAsync()                           // Create branch
private async Task DeleteBranchAsync(string branchName)             // Delete branch
```

#### Remote Operations
```csharp
// In MainWindow
private async void OnManageRemotes(object sender, EventArgs e)      // Remote management dialog
private async Task AddRemoteAsync()                                 // Add new remote
private async Task RemoveRemoteAsync(string remoteName)             // Remove remote
private async Task SetDefaultRemoteAsync(string remoteName)         // Set default remote
```

### Database Persistence
- All branches are persisted to `Branches` table
- All remotes are persisted to `Remotes` table
- Operations are async and use ConfigureAwait(false) for UI responsiveness

### Error Handling
- All operations include try-catch blocks with user-friendly error messages
- Failed operations display error dialogs and log to debug view
- Invalid operations (e.g., deleting current branch) are prevented before execution

---

## Integration with Existing Features

### Push/Pull Operations
Remotes added via "Manage Remotes" can be used for:
- Push operations (uploads commits to remote)
- Pull operations (fetches and merges from remote)

### Branch Navigation
The branch switching system integrates with:
- Git graph visualization (updates when branch changes)
- File staging (shows files for new branch's HEAD)
- Status bar (displays current branch)

---

## Known Limitations

1. **Remote Protocol Detection** - Currently defaults to HTTPS. SSH and custom protocols require configuration.
2. **Batch Operations** - Remote and branch operations are performed one at a time (no bulk operations)
3. **Merge Conflicts** - Branch switching doesn't handle uncommitted changes; ensure branch is clean first

---

## Future Enhancements

1. Merge branches directly from the UI
2. Rebase operations
3. Branch history and tracking
4. Automatic remote branch tracking
5. Repository cloning directly from remotes
6. Credential management per remote

---

## Testing Checklist

- [ ] Create new branch and verify it appears in the dialog
- [ ] Switch between multiple branches and verify UI updates
- [ ] Try to delete current branch and verify protection works
- [ ] Delete a non-current branch successfully
- [ ] Add a remote with HTTPS URL
- [ ] Set a remote as default
- [ ] Remove a remote
- [ ] Verify remotes persist after application restart
- [ ] Push/Pull with configured remotes

---

## Support

For issues or feature requests related to branch switching or remote management, check the logs folder (`logs/`) for detailed error information.
