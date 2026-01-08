# Repository Guidelines

## Project Structure & Module Organization
- `source/Playnite.Core/`: cross-platform core utilities and services (new home for shared logic).
- `source/PlayniteSDK/`: plugin SDK and public contracts.
- `source/Playnite.DesktopApp.Avalonia/` and `source/Playnite.FullscreenApp.Avalonia/`: Avalonia UI shells (current focus).
- Legacy WPF apps/libs remain under `source/Playnite/`, `source/Playnite.DesktopApp/`, `source/Playnite.FullscreenApp/` (kept for reference during migration).
- `source/Tools/`: installer and utilities (both legacy and `.Avalonia` replacements).
- `source/Tests/`: unit tests plus test apps/plugins (prefer `*.Avalonia` projects going forward).
- `source/plan.md`: migration milestones/status (keep current when making changes).

## Build, Test, and Development Commands
- `dotnet build source/Playnite.sln`: builds the Avalonia-first solution
- `dotnet run --project source/Playnite.DesktopApp.Avalonia/Playnite.DesktopApp.Avalonia.csproj`: run desktop shell.
- `dotnet run --project source/Playnite.FullscreenApp.Avalonia/Playnite.FullscreenApp.Avalonia.csproj`: run fullscreen shell.
- `dotnet test source/Tests/Playnite.Tests.Avalonia/Playnite.Tests.Avalonia.csproj`: run Avalonia-compatible tests.
- Optional data: set `PLAYNITE_DB_PATH` to a Playnite profile folder containing `games.db`/`platforms.db`/`genres.db` and `files/`.

## Coding Style & Naming Conventions
- C# conventions: PascalCase types/members, camelCase locals/fields; keep namespaces aligned to folders.
- Nullable annotations are often disabled in existing projects; match the project you touch.
- Keep changes minimal and migration-focused; avoid unrelated refactors.

## Testing Guidelines
- NUnit is used by `source/Tests/` projects.
- Prefer adding/maintaining tests in `*.Avalonia` harnesses; avoid introducing new WPF-only tests.

## Commit & Pull Request Guidelines
- Commit subjects are short, imperative, sentence-style (e.g., `Upgrade to .NET 10`).
- PRs: include scope summary, affected projects, and exact commands used to build/test.

## Agent-Specific Instructions
- Keep `source/plan.md` updated after meaningful changes (milestones, blockers, and next steps).
