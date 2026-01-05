# Li'nage Branch & Remote Features - Quick Reference

## Branch Operations (Click branch name in status bar)

| Action | Steps |
|--------|-------|
| **Switch Branch** | 1. Click branch name in status bar → Select branch → Click "Switch" |
| **Create Branch** | 1. Click branch name → Click "New Branch" → Enter name → Confirm |
| **Delete Branch** | 1. Click branch name → Select branch → Click "Delete" → Confirm |

## Remote Operations (Remote → Manage Remotes...)

| Action | Steps |
|--------|-------|
| **Add Remote** | 1. Open Manage Remotes → Click "Add Remote" → Enter name & URL |
| **Remove Remote** | 1. Open Manage Remotes → Select remote → Click "Remove" → Confirm |
| **Set Default** | 1. Open Manage Remotes → Select remote → Click "Set as Default" |
| **View Remotes** | 1. Open Manage Remotes to see all configured remotes |

## Key Features

✅ **Branch Switching** - Switch between branches with automatic UI updates
✅ **Create Branches** - Create new branches from current commit
✅ **Delete Branches** - Remove branches (with safety checks)
✅ **Remote Management** - Add, view, and remove remote repositories
✅ **Default Remote** - Set which remote is used for push/pull
✅ **Persistent Storage** - All settings saved to SQL Server
✅ **Error Handling** - Clear error messages for all operations
✅ **Async Operations** - Non-blocking UI during operations

## Status Bar Indicators

- **Branch name (left)** - Shows current branch
- Click to open branch switching dialog
- "(current)" marker indicates active branch in dialog

## Menu Access

```
File
  ├── Open Repository...
  ├── Close Repository
  └── Exit

Edit
  ├── Undo
  └── Redo

View
  ├── ... 
  └── Refresh Status

Remote ✨ NEW
  ├── Manage Remotes... ✨ NEW
  ├── ───────────────
  ├── Push
  └── Pull

Help
  ├── About Li'nage
  └── Simulate Notifications
```

## Common Workflows

### Workflow 1: Work on a Feature Branch
```
1. Click branch name in status bar
2. Click "New Branch"
3. Enter branch name (e.g., "feature/new-ui")
4. Make commits on the feature branch
5. Switch back to main branch when done
```

### Workflow 2: Push to Remote
```
1. Make commits on your branch
2. Go to Remote → Push
3. Enter remote URL (or use configured remote)
4. Commits are pushed to remote
```

### Workflow 3: Sync with Remote Repository
```
1. Go to Remote → Manage Remotes...
2. Click "Add Remote" if not configured
3. Enter origin URL
4. Go to Remote → Pull to fetch and merge
5. Or go to Remote → Push to upload your commits
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Can't delete branch | Make sure you're not on that branch. Switch to another branch first. |
| Remote URL invalid | Verify the HTTPS URL format: `https://github.com/user/repo.git` |
| Operations hanging | Check status bar for progress. Ensure network connection is active. |
| Branch doesn't appear | Refresh the app or reopen the branch dialog |
| Permission denied | Verify credentials and ensure SSH keys are configured (for SSH URLs) |

## Pro Tips

💡 **Tip 1**: Set "origin" as the default remote - most workflows use this convention
💡 **Tip 2**: Branch names with "/" create hierarchies (e.g., "feature/login", "bugfix/crash")
💡 **Tip 3**: Delete merged branches to keep the repository clean
💡 **Tip 4**: Always switch branches before deleting them
💡 **Tip 5**: Keep default remote set to avoid accidental pushes to wrong locations
