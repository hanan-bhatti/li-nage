# Li'nage - Project Context

## Overview
Li'nage is an advanced version control system designed to provide granular line-level tracking combined with intelligent change detection and visualization. It differs from traditional VCS by tracking individual line modifications rather than file-based atomic units.

## Current State
- **Phase 1 (Foundation):** Completed. Data models (Commit, Snapshot, FileMetadata, etc.) and basic database context are set up.
- **Phase 2 (Core Features):** Completed (believed). Diff algorithms (Myers, Patient, Minimal), LineTracker, and ChangeDetector are implemented.
- **Phase 3 (Authentication & Remote):** Completed. `Push` and `Pull` implemented via `LibGit2Sharp`. Transport layers consolidated.

## Environment
- **Platform:** Linux (Development), Windows Forms (UI Target).
- **Framework:** .NET 8.0 (Windows target in csproj).
- **Database:** SQL Server (Current), SQLite (Legacy).

## Roadmap
- **Immediate Goal:**
    1.  **Verification**: Test Push/Pull with real GitHub repo.
    2.  **Refinement**: Improve error handling for merge conflicts.
    3.  **Docs**: Generate user manual.

## Key Architecture
- **Presentation:** Windows Forms (GUI).
- **Controller:** Orchestrates operations.
- **Business Logic:** Core services (VersionGraph, LineTracker, Auth).
- **Infrastructure:** File system monitoring, DB access, Network transport.

## Developer Notes
- Running on Linux with a Windows-specific project (`.csproj` targets `net8.0-windows`) implies code-level work or using compatibility layers (like Wine) for execution.
- `LiNageDbContext` currently defaults to SQLite.
- `CredentialStore` is an in-memory stub; needs persistence (simulating Windows Credential Manager or using a file-based approach for Linux dev).
