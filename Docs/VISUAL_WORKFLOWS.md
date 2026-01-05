# Visual UI Workflows - Branch & Remote Management

## Branch Switching Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│ Li'nage Main Window                                             │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ File  Edit  View  Remote  Help                              │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│ Status Bar:                                                       │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ [main] ▼  (CLICK HERE) │ Repo: MyProject │ Files: 42       │ │
│ └──────────────────────────────────────────────────────────────┘ │
│     ▲
│     └─ Click to open branch selection dialog
│
└─────────────────────────────────────────────────────────────────┘

         ↓ CLICK BRANCH NAME ↓

┌───────────────────────────────────┐
│  Switch Branch                    │
├───────────────────────────────────┤
│  [ListBox]                        │
│  ┌─────────────────────────────┐ │
│  │ main (current)              │ │
│  │ feature/login               │ │
│  │ feature/auth                │ │
│  │ bugfix/crash                │ │
│  │ develop                      │ │
│  └─────────────────────────────┘ │
│                                   │
│  [Switch]  [New Branch]  [Delete] │
└───────────────────────────────────┘

         ↓ SELECT & CLICK ↓

Options:
  1. Click "Switch" → Switch to selected branch
  2. Click "New Branch" → Create new branch
  3. Click "Delete" → Delete selected branch
```

## Remote Management Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│ Li'nage Main Window                                             │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ File  Edit  View  Remote  Help                              │ │
│ │                    ▼ CLICK                                   │ │
│ │                    ├─ Manage Remotes... ✨ NEW               │ │
│ │                    ├─ ──────────────────                    │ │
│ │                    ├─ Push                                   │ │
│ │                    └─ Pull                                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

         ↓ CLICK "Manage Remotes..." ↓

┌──────────────────────────────────────────┐
│  Manage Remotes                          │
├──────────────────────────────────────────┤
│  [ListBox]                               │
│  ┌────────────────────────────────────┐ │
│  │ origin (default)                   │ │
│  │ upstream                           │ │
│  │ backup                             │ │
│  └────────────────────────────────────┘ │
│                                          │
│  [Add Remote]  [Set as Default] [Remove]│
└──────────────────────────────────────────┘

         ↓ SELECT & CLICK ↓

Options:
  1. Click "Add Remote" → Add new remote
  2. Click "Set as Default" → Make selected default
  3. Click "Remove" → Delete selected remote
```

## Add Remote Dialog Sequence

```
User clicks "Add Remote"
         ↓
┌────────────────────────────────────┐
│ Enter remote name (e.g. "origin"):  │
│ [_____________________]             │
│              [OK]  [Cancel]         │
└────────────────────────────────────┘
         ↓
┌────────────────────────────────────┐
│ Enter remote URL:                  │
│ [___________________________]       │
│              [OK]  [Cancel]         │
└────────────────────────────────────┘
         ↓
Remote saved to database
         ↓
"Remote 'origin' added successfully"
```

## Create Branch Dialog Sequence

```
User clicks "New Branch"
         ↓
┌────────────────────────────────────┐
│ Enter new branch name:              │
│ (e.g. "feature/login")              │
│ [_____________________]             │
│              [OK]  [Cancel]         │
└────────────────────────────────────┘
         ↓
Branch created in database
         ↓
Automatically switch to new branch
         ↓
"Created and switched to 'feature/login'"
```

## Operation Flow with Progress

```
User clicks action (Switch/Create/Delete)
         ↓
┌──────────────────────────────────────┐
│  Loading... ⟳                        │
│  "Switching to branch 'develop'..."  │
│                                      │
│  [████████░░] 50%                    │
└──────────────────────────────────────┘
         ↓
Operation completes
         ↓
UI Updates:
  ✓ Status bar shows new branch
  ✓ Git graph refreshes
  ✓ File list updates
  ✓ Debug log records operation
         ↓
"Switched to branch 'develop'"
```

## Error Handling Flow

```
User clicks action
         ↓
Operation fails (e.g., invalid URL)
         ↓
┌──────────────────────────────────────┐
│         Error                        │
├──────────────────────────────────────┤
│  Failed to add remote:               │
│  Invalid URL format                  │
│                                      │
│  [OK]                                │
└──────────────────────────────────────┘
         ↓
Error logged to debug view:
  "Add remote error: Invalid URL format"
```

## State Transitions

```
BRANCHES
┌────────────────────────────────────┐
│        No Branches                  │
│   (First time setup)                │
│                                     │
│   └─ Create first commit            │
│      └─ Main/master branch created  │
└────────────────────────────────────┘
              ↓
┌────────────────────────────────────┐
│     Single Branch (main)            │
│   Switch button shows "main"        │
│   Can create new branches from here │
└────────────────────────────────────┘
              ↓
┌────────────────────────────────────┐
│    Multiple Branches               │
│   ├─ main (current)                │
│   ├─ feature/login                 │
│   ├─ bugfix/crash                  │
│   └─ develop                       │
│   Can switch/create/delete         │
└────────────────────────────────────┘
```

## Status Bar Layout

```
┌────────────────────────────────────────────────────────────────┐
│ [develop] ▼│ Repo: MyProject │ Files: 42 │ Ready            │
│  ↑         ↑                                                   │
│  │         │                                                   │
│  Branch    Click to open                                       │
│  name      dialog                                              │
│            Shows current branch
└────────────────────────────────────────────────────────────────┘
```

## Menu Structure

```
Remote (Menu) ✨ NEW
├── Manage Remotes...      ✨ NEW
│   ├─ Add Remote
│   ├─ View All
│   ├─ Set Default
│   └─ Remove
├── ─────────────────
├── Push
└── Pull
```

## Dialog Button Meanings

```
BRANCH DIALOG
┌─────────────────────────────┐
│ [Switch]       [New]  [Del] │
└─────────────────────────────┘
   ↓             ↓       ↓
Switch to    Create   Delete
selected     new      selected
branch       branch   branch

REMOTE DIALOG
┌────────────────────────────────────┐
│ [Add]       [Default]      [Remove] │
└────────────────────────────────────┘
   ↓             ↓               ↓
Add new      Set selected     Remove
remote       as default       selected
             remote           remote
```

## Integration Points

```
                    VersionController
                          ↑
         ┌────────────────┼────────────────┐
         ↓                ↓                ↓
    GraphService      RemoteService   ChangeDetector
         ↓                ↓                ↓
    Database       Database        File System
    (Branches)     (Remotes)       (File changes)
         ↑                ↑                ↑
         └────────────────┼────────────────┘
                          ↑
                      MainWindow
                      UI Updates:
                      - Status bar
                      - Git graph
                      - File list
```

---

## Keyboard Shortcuts (For Future Enhancement)

```
Ctrl+Shift+B    → Open branch dialog
Ctrl+Shift+M    → Open manage remotes dialog
Alt+→           → Switch to next branch (future)
Alt+←           → Switch to previous branch (future)
```

---

## Color Coding (For Future Enhancement)

```
Branch Status:
├─ Green   = Current branch (active)
├─ Blue    = Remote branch
├─ Gray    = Local branch
└─ Red     = Diverged branch

Remote Status:
├─ Green   = Default remote
├─ Blue    = Configured remote
└─ Yellow  = Inactive remote
```
