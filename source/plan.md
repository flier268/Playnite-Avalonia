# Migration Plan (WPF → Avalonia, cross-platform)

## Status (updated)
- Overall: In progress
- Current milestone: Feature parity checklist (Desktop + Fullscreen)

## Definition of Done
- `source/Playnite.sln` builds and tests without any WPF/WindowsDesktop projects/packages in the build graph. [done]
- Desktop + Fullscreen reach feature parity for the checklist below. [in progress]
- Tools (installer/utilities/toolbox) have Avalonia or CLI equivalents. [in progress]
- Cross-platform hardening: Windows-only calls are isolated/guarded and Linux/macOS runs are unblocked. [in progress]
  - No reflection usage in Avalonia build graph (avoid `System.Reflection`/dynamic loading). [done]
  - Script actions don't require PowerShell on non-Windows (fallback to `sh`). [done]
- Legacy WPF source tree retired/removed after parity is reached. [pending]

## Milestones
1. Avalonia-only build graph + CI (remove WPF project dependencies). [done]
2. Establish cross-platform core layer (`Playnite.Core`) for shared logic. [in progress]
3. Refactor `PlayniteSDK` to be UI-agnostic/Avalonia-first (remove WPF types). [in progress]
4. Desktop Avalonia parity (Library/Details/Settings/Add-ons). [in progress]
5. Fullscreen Avalonia parity (Library/Details/Settings). [in progress]
6. Add-ons / extensions management (install/update/remove; themes). [pending]
7. Tools parity (Installer/Utilities/Toolbox + templates). [in progress]
8. Cross-platform hardening pass (Windows-only isolation; path/process/script handling). [pending]
9. Retire legacy WPF source tree (delete or move to archive branch). [pending]
10. End-to-end validation + docs (run commands, env vars, known gaps). [pending]

## Feature Parity Checklist

### Desktop (Avalonia)
- Settings
  - General (startup/tray/autostart/culture). [done]
  - Appearance (theme select + persistence). [done]
  - Libraries (DB path + reload). [done]
  - Updates. [done]
  - Advanced. [done]
  - About. [done]
- Library / browsing
  - List/grid + cover rendering. [done]
  - Search + basic filters (installed/favorites/hidden/platform/genre). [done]
  - Sorting/grouping parity. [done]
  - Context actions parity (install, play, edit, etc.). [in progress]
  - Metadata download / refresh flows. [in progress]
    - Install size scanning (manual + auto on reload/install). [done]
    - Metadata provider registry + on-demand download (Game Details; offline `MetadataFile.Content` images). [done]
    - Extension-based metadata providers (dynamic loading). [pending] (blocked: no reflection rule)
    - Built-in Local Files provider (reads `icon/cover/background` from install folder). [done]
    - Library/Fullscreen metadata download commands (missing-only). [done]
- Game details
  - Launch File/URL/Script actions. [done]
  - Launch Emulator actions (custom + built-in + ScriptStartup). [done]
  - Runtime prompts (ROM selection; emulator profile selection). [done]
  - Play stats persistence (playtime/last activity/play count). [done]
  - Process tracking modes parity. [done]
  - Game actions editor UI (add/remove/edit; set play action; persist). [done]
  - Install/uninstall (toggle + persist). [done]
- Add-ons UI
  - Browse + Installed sections UI. [done]
  - Install/update/remove extensions/themes. [in progress]
  - Enable/disable installed extensions. [done]

### Fullscreen (Avalonia)
- Shell + navigation
  - Library grid + details panel. [done]
  - Apply theme/culture on startup. [done]
- Game details
  - Install/uninstall (toggle + persist). [done]
  - Game actions editor UI (add/remove/edit; set play action; persist). [done]
  - Launch actions (File/URL/Script/Emulator). [done]
  - Runtime prompts (ROM selection; emulator/profile selection). [done]
  - AfterLaunch/AfterGameClose behavior. [done]
- Remaining parity items (dialogs, overlays, etc.). [pending]

### Tools / Tests
- Tests
  - `Tests/Playnite.Tests.Avalonia` running in CI. [done]
  - Expand CI coverage to other Avalonia test projects. [done]
- Installer
  - Avalonia shell exists. [in progress]
  - Functional install/uninstall/update parity. [pending]
- Utilities / Toolbox
  - Avalonia shells exist. [in progress]
  - Avalonia-first templates for extensions/themes. [pending]

## Legacy WPF Inventory
- WPF `.csproj` files under `source/` were deleted to enforce Avalonia-only build graph/CI. [done]
- Legacy WPF source directories remain in-tree for reference until parity is reached. [in progress]
