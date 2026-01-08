using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Playnite.Configuration;
using Playnite.Launching;
using Playnite.Library;
using Playnite.SDK.Models;
using Avalonia.Threading;
using System.IO;
using Playnite.Emulation;
using Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;
using Playnite.DesktopApp.Avalonia.Views.Dialogs;
using System.Collections.Generic;

namespace Playnite.DesktopApp.Avalonia.Services;

public sealed class GameLaunchService : IGameLaunchService
{
    private const string CustomProfilePrefix = "#custom_";
    private readonly GameActionLauncher launcher = new GameActionLauncher();
    private bool lastLaunchChangedWindow;

    public GameLaunchService()
    {
    }

    public async Task<bool> LaunchAsync(Game game)
    {
        if (game is null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var action = SelectPlayAction(game);
        if (action is null)
        {
            return false;
        }

        Emulator? emulator = null;
        string selectedRomPath = string.Empty;
        string selectedProfileId = string.Empty;
        if (action.Type == GameActionType.Emulator)
        {
            var prep = await PrepareEmulatorLaunchAsync(game, action);
            if (!prep.Ok)
            {
                return false;
            }

            emulator = prep.Emulator;
            selectedRomPath = prep.RomPath;
            selectedProfileId = prep.ProfileId;
        }

        game.IsLaunching = true;
        game.IsRunning = false;

        var result = action.Type == GameActionType.Emulator
            ? LaunchEmulator(game, action, emulator, selectedRomPath, selectedProfileId)
            : launcher.Launch(game, action);
        if (!result.Started)
        {
            game.IsLaunching = false;
            return false;
        }

        game.IsLaunching = false;
        game.IsRunning = true;
        game.LastActivity = DateTime.Now;
        game.PlayCount = game.PlayCount + 1;
        (AppServices.LibraryStore as IGameStatsStore)?.TryUpdateGameStats(game);

        ApplyAfterLaunch();

        if (result.Session is null)
        {
            game.IsRunning = false;
            return true;
        }

        await result.Session.WaitForExitAsync();

        Dispatcher.UIThread.Post(() =>
        {
            game.IsRunning = false;
            var elapsed = DateTime.UtcNow - result.Session.StartTime;
            if (elapsed.TotalSeconds > 0)
            {
                try
                {
                    game.Playtime += (ulong)Math.Round(elapsed.TotalSeconds);
                }
                catch
                {
                }
            }

            ApplyAfterGameClose();
            (AppServices.LibraryStore as IGameStatsStore)?.TryUpdateGameStats(game);
        });
        return true;
    }

    private static GameAction? SelectPlayAction(Game game)
    {
        return game.GameActions?.FirstOrDefault(a => a?.IsPlayAction == true) ??
               game.GameActions?.FirstOrDefault();
    }

    private readonly record struct EmulatorLaunchPreparation(bool Ok, Emulator? Emulator, string RomPath, string ProfileId);

    private async Task<EmulatorLaunchPreparation> PrepareEmulatorLaunchAsync(Game game, GameAction emulatorAction)
    {
        if (AppServices.LibraryStore is null)
        {
            return new EmulatorLaunchPreparation(false, null, string.Empty, string.Empty);
        }

        var emulator = AppServices.LibraryStore.LoadEmulators().FirstOrDefault(a => a.Id == emulatorAction.EmulatorId);
        if (emulator is null)
        {
            return new EmulatorLaunchPreparation(false, null, string.Empty, string.Empty);
        }

        var selectedRomPath = await SelectRomPathAsync(game);
        if (selectedRomPath is null)
        {
            return new EmulatorLaunchPreparation(false, emulator, string.Empty, string.Empty);
        }

        var selectedProfileId = await SelectEmulatorProfileAsync(emulator, emulatorAction.EmulatorProfileId);
        if (selectedProfileId is null)
        {
            return new EmulatorLaunchPreparation(false, emulator, selectedRomPath, string.Empty);
        }

        return new EmulatorLaunchPreparation(true, emulator, selectedRomPath, selectedProfileId);
    }

    private static async Task<string?> SelectRomPathAsync(Game game)
    {
        var roms = game.Roms?.Where(a => a != null).ToList() ?? new List<GameRom>();
        if (roms.Count == 0)
        {
            return string.Empty;
        }

        if (roms.Count == 1)
        {
            return roms[0].Path ?? string.Empty;
        }

        var items = roms.Select(a =>
        {
            var primary = string.IsNullOrWhiteSpace(a.Name) ? (Path.GetFileName(a.Path) ?? string.Empty) : a.Name;
            var secondary = a.Path ?? string.Empty;
            return new SingleSelectItem(primary, secondary, a);
        }).ToList();

        var selected = await ShowSingleSelectAsync("Select ROM", items);
        if (selected?.Value is GameRom rom)
        {
            return rom.Path ?? string.Empty;
        }

        return null;
    }

    private static async Task<string?> SelectEmulatorProfileAsync(Emulator emulator, string? requestedProfileId)
    {
        var requested = requestedProfileId ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(requested) && emulator.GetProfile(requested) != null)
        {
            return requested;
        }

        if (!string.IsNullOrWhiteSpace(requested) &&
            !requested.StartsWith("#custom_", StringComparison.Ordinal) &&
            !requested.StartsWith("#builtin_", StringComparison.Ordinal))
        {
            // Compatibility: some callers may store BuiltInProfileName directly.
            return requested;
        }

        var profiles = emulator.AllProfiles ?? new List<EmulatorProfile>();
        if (profiles.Count == 0)
        {
            return null;
        }

        if (profiles.Count == 1)
        {
            return profiles[0].Id;
        }

        var items = profiles.Select(p =>
        {
            var primary = string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name;
            var secondary = p switch
            {
                BuiltInEmulatorProfile builtIn => $"Built-in: {builtIn.BuiltInProfileName}",
                CustomEmulatorProfile custom => $"Custom: {custom.Executable}",
                _ => p.Id
            };

            return new SingleSelectItem(primary, secondary, p);
        }).ToList();

        var selected = await ShowSingleSelectAsync("Select Emulator Profile", items);
        if (selected?.Value is EmulatorProfile profile)
        {
            return profile.Id;
        }

        return null;
    }

    private static async Task<SingleSelectItem?> ShowSingleSelectAsync(string title, IReadOnlyList<SingleSelectItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return null;
        }

        if (AppServices.MainWindow is null)
        {
            return items[0];
        }

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var window = new SingleSelectWindow
            {
                DataContext = new SingleSelectViewModel(title, items)
            };
            return await window.ShowDialog<SingleSelectItem?>(AppServices.MainWindow);
        });
    }

    private GameLaunchResult LaunchEmulator(Game game, GameAction emulatorAction, Emulator? emulator, string selectedRomPath, string selectedProfileId)
    {
        if (emulator is null)
        {
            return GameLaunchResult.Failed("Emulator not found.");
        }

        var playniteDir = AppContext.BaseDirectory ?? string.Empty;
        var emulatorDir = LaunchVariableExpander.Expand(emulator.InstallDir ?? string.Empty, game, string.Empty, string.Empty, playniteDir);

        var romPath = selectedRomPath ?? string.Empty;
        romPath = LaunchVariableExpander.Expand(romPath, game, emulatorDir, string.Empty, playniteDir);

        var requestedProfileId = !string.IsNullOrWhiteSpace(selectedProfileId) ? selectedProfileId : emulatorAction.EmulatorProfileId;
        if (string.IsNullOrWhiteSpace(requestedProfileId))
        {
            return GameLaunchResult.Failed("Emulator profile is not set.");
        }

        var profile = emulator.GetProfile(requestedProfileId);
        if (profile is BuiltInEmulatorProfile builtInProfile)
        {
            return LaunchBuiltInEmulatorProfile(game, emulator, emulatorAction, emulatorDir, romPath, playniteDir, builtInProfile.BuiltInProfileName);
        }

        if (profile is null)
        {
            // Compatibility path: allow passing BuiltInProfileName directly.
            if (!requestedProfileId.StartsWith(CustomProfilePrefix, StringComparison.Ordinal) &&
                !requestedProfileId.StartsWith("#builtin_", StringComparison.Ordinal))
            {
                return LaunchBuiltInEmulatorProfile(game, emulator, emulatorAction, emulatorDir, romPath, playniteDir, requestedProfileId);
            }

            return GameLaunchResult.Failed("Emulator profile not found.");
        }

        if (profile is not CustomEmulatorProfile customProfile)
        {
            return GameLaunchResult.Failed("Unsupported emulator profile type.");
        }

        if (!string.IsNullOrWhiteSpace(customProfile.StartupScript))
        {
            return LaunchCustomEmulatorProfileScript(game, emulator, emulatorAction, customProfile, emulatorDir, romPath, playniteDir);
        }

        var executable = LaunchVariableExpander.Expand(customProfile.Executable ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        executable = ResolvePath(executable, emulatorDir);
        if (string.IsNullOrWhiteSpace(executable))
        {
            return GameLaunchResult.Failed("Emulator executable is empty.");
        }

        var workingDir = LaunchVariableExpander.Expand(customProfile.WorkingDirectory ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        workingDir = ResolvePath(workingDir, emulatorDir);

        var baseArgs = LaunchVariableExpander.Expand(customProfile.Arguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        var extraArgs = LaunchVariableExpander.Expand(emulatorAction.AdditionalArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);

        var finalArgs = emulatorAction.OverrideDefaultArgs
            ? extraArgs
            : CombineArgs(baseArgs, extraArgs);

        var trackingMode = emulatorAction.TrackingMode != TrackingMode.Default ? emulatorAction.TrackingMode : customProfile.TrackingMode;
        var trackingPath = !string.IsNullOrWhiteSpace(emulatorAction.TrackingPath)
            ? LaunchVariableExpander.Expand(emulatorAction.TrackingPath, game, emulatorDir, romPath, playniteDir)
            : LaunchVariableExpander.Expand(customProfile.TrackingPath ?? string.Empty, game, emulatorDir, romPath, playniteDir);

        var fileAction = new GameAction
        {
            Type = GameActionType.File,
            Path = executable,
            WorkingDir = workingDir,
            Arguments = finalArgs,
            TrackingMode = trackingMode,
            TrackingPath = trackingPath,
            InitialTrackingDelay = emulatorAction.InitialTrackingDelay,
            TrackingFrequency = emulatorAction.TrackingFrequency
        };

        return launcher.Launch(game, fileAction);
    }

    private GameLaunchResult LaunchBuiltInEmulatorProfile(Game game, Emulator emulator, GameAction emulatorAction, string emulatorDir, string romPath, string playniteDir, string builtInProfileName)
    {
        var profileName = builtInProfileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return GameLaunchResult.Failed("Built-in emulator profile name is empty.");
        }

        if (!BuiltInEmulatorDefinitionStore.TryGetProfile(emulator.BuiltInConfigId, profileName, out var profileDef))
        {
            return GameLaunchResult.Failed($"Built-in emulator profile not found: {emulator.BuiltInConfigId} / {profileName}");
        }

        if (profileDef.ScriptStartup)
        {
            return LaunchBuiltInEmulatorProfileScript(game, emulator, emulatorAction, profileDef, profileName, emulatorDir, romPath, playniteDir);
        }

        if (!EmulatorExecutableFinder.TryFindExecutable(emulatorDir, profileDef.StartupExecutable, out var executable))
        {
            return GameLaunchResult.Failed($"Emulator executable not found (regex: {profileDef.StartupExecutable}).");
        }

        var builtInOverrides = emulator.BuiltinProfiles?.FirstOrDefault(a => string.Equals(a.BuiltInProfileName, profileName, StringComparison.Ordinal));

        string baseArgs;
        if (builtInOverrides?.OverrideDefaultArgs == true)
        {
            baseArgs = LaunchVariableExpander.Expand(builtInOverrides.CustomArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        }
        else
        {
            baseArgs = LaunchVariableExpander.Expand(profileDef.StartupArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        }

        var extraArgs = LaunchVariableExpander.Expand(emulatorAction.AdditionalArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        var finalArgs = emulatorAction.OverrideDefaultArgs ? extraArgs : CombineArgs(baseArgs, extraArgs);

        var fileAction = new GameAction
        {
            Type = GameActionType.File,
            Path = executable,
            WorkingDir = emulatorDir,
            Arguments = finalArgs,
            TrackingMode = OperatingSystem.IsWindows() ? TrackingMode.Process : TrackingMode.OriginalProcess,
            TrackingPath = emulatorDir,
            InitialTrackingDelay = emulatorAction.InitialTrackingDelay,
            TrackingFrequency = emulatorAction.TrackingFrequency
        };

        return launcher.Launch(game, fileAction);
    }

    private GameLaunchResult LaunchCustomEmulatorProfileScript(Game game, Emulator emulator, GameAction emulatorAction, CustomEmulatorProfile customProfile, string emulatorDir, string romPath, string playniteDir)
    {
        var expandedBody = LaunchVariableExpander.Expand(customProfile.StartupScript ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        if (string.IsNullOrWhiteSpace(expandedBody))
        {
            return GameLaunchResult.Failed("Emulator StartupScript is empty.");
        }

        var wrapper = BuildEmulatorPowerShellWrapper(
            expandedBody,
            emulator,
            customProfile.Name ?? string.Empty,
            emulatorDir,
            romPath);

        var trackingMode = emulatorAction.TrackingMode != TrackingMode.Default ? emulatorAction.TrackingMode : customProfile.TrackingMode;
        if (trackingMode == TrackingMode.Default)
        {
            trackingMode = OperatingSystem.IsWindows() ? TrackingMode.Process : TrackingMode.OriginalProcess;
        }

        var trackingPath = !string.IsNullOrWhiteSpace(emulatorAction.TrackingPath)
            ? LaunchVariableExpander.Expand(emulatorAction.TrackingPath, game, emulatorDir, romPath, playniteDir)
            : LaunchVariableExpander.Expand(customProfile.TrackingPath ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        if (string.IsNullOrWhiteSpace(trackingPath))
        {
            trackingPath = emulatorDir;
        }

        var scriptAction = new GameAction
        {
            Type = GameActionType.Script,
            Script = wrapper,
            WorkingDir = emulatorDir,
            TrackingMode = trackingMode,
            TrackingPath = trackingPath,
            InitialTrackingDelay = emulatorAction.InitialTrackingDelay,
            TrackingFrequency = emulatorAction.TrackingFrequency
        };

        return launcher.Launch(game, scriptAction);
    }

    private GameLaunchResult LaunchBuiltInEmulatorProfileScript(Game game, Emulator emulator, GameAction emulatorAction, EmulatorDefinitionProfile profileDef, string profileName, string emulatorDir, string romPath, string playniteDir)
    {
        if (!BuiltInEmulatorDefinitionStore.TryGetStartupScriptPath(emulator.BuiltInConfigId, out var startupScriptPath) ||
            string.IsNullOrWhiteSpace(startupScriptPath) ||
            !File.Exists(startupScriptPath))
        {
            return GameLaunchResult.Failed($"Startup script not found for built-in emulator definition: {emulator.BuiltInConfigId}");
        }

        var wrapper = BuildEmulatorPowerShellWrapperForFile(
            startupScriptPath,
            emulator,
            profileName,
            emulatorDir,
            romPath);

        var trackingMode = emulatorAction.TrackingMode;
        if (trackingMode == TrackingMode.Default)
        {
            trackingMode = OperatingSystem.IsWindows() ? TrackingMode.Process : TrackingMode.OriginalProcess;
        }

        var trackingPath = !string.IsNullOrWhiteSpace(emulatorAction.TrackingPath)
            ? LaunchVariableExpander.Expand(emulatorAction.TrackingPath, game, emulatorDir, romPath, playniteDir)
            : emulatorDir;

        var scriptAction = new GameAction
        {
            Type = GameActionType.Script,
            Script = wrapper,
            WorkingDir = emulatorDir,
            TrackingMode = trackingMode,
            TrackingPath = trackingPath,
            InitialTrackingDelay = emulatorAction.InitialTrackingDelay,
            TrackingFrequency = emulatorAction.TrackingFrequency
        };

        return launcher.Launch(game, scriptAction);
    }

    private static string CombineArgs(string a, string b)
    {
        a = a?.Trim() ?? string.Empty;
        b = b?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(a))
        {
            return b;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a;
        }

        return a + " " + b;
    }

    private static string BuildEmulatorPowerShellWrapper(string scriptBody, Emulator emulator, string profileName, string emulatorDir, string romPath)
    {
        var emuName = EscapePowerShellSingleQuoted(emulator?.Name ?? string.Empty);
        var emuInstallDir = EscapePowerShellSingleQuoted(emulatorDir ?? string.Empty);
        var emuConfigId = EscapePowerShellSingleQuoted(emulator?.BuiltInConfigId ?? string.Empty);
        var emuId = EscapePowerShellSingleQuoted(emulator?.Id.ToString() ?? string.Empty);
        var profName = EscapePowerShellSingleQuoted(profileName ?? string.Empty);
        var rom = EscapePowerShellSingleQuoted(romPath ?? string.Empty);

        return
$@"$ErrorActionPreference = 'Stop'
$global:RomPath = '{rom}'
$global:Emulator = [pscustomobject]@{{ Name = '{emuName}'; InstallDir = '{emuInstallDir}'; BuiltInConfigId = '{emuConfigId}'; Id = '{emuId}' }}
$global:EmulatorProfile = [pscustomobject]@{{ Name = '{profName}'; BuiltInProfileName = '{profName}' }}

{scriptBody}
";
    }

    private static string BuildEmulatorPowerShellWrapperForFile(string scriptPath, Emulator emulator, string profileName, string emulatorDir, string romPath)
    {
        var wrapperBody = $". '{EscapePowerShellSingleQuoted(scriptPath)}'";
        return BuildEmulatorPowerShellWrapper(wrapperBody, emulator, profileName, emulatorDir, romPath);
    }

    private static string EscapePowerShellSingleQuoted(string value)
    {
        return (value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal);
    }

    private static string ResolvePath(string value, string baseDir)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            if (Path.IsPathRooted(value))
            {
                return value;
            }

            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                return Path.Combine(baseDir, value);
            }

            return Path.GetFullPath(value);
        }
        catch
        {
            return value;
        }
    }

    private void ApplyAfterLaunch()
    {
        lastLaunchChangedWindow = false;
        var settings = AppServices.LoadSettings();

        switch (settings.AfterLaunch)
        {
            case AfterLaunchOption.Minimize:
                AppServices.MainWindow.WindowState = WindowState.Minimized;
                lastLaunchChangedWindow = true;
                break;
            case AfterLaunchOption.Close:
                if (settings.EnableTray)
                {
                    AppServices.MainWindow.HideToTray();
                }
                else
                {
                    AppServices.MainWindow.WindowState = WindowState.Minimized;
                }

                lastLaunchChangedWindow = true;
                break;
        }
    }

    private void ApplyAfterGameClose()
    {
        var settings = AppServices.LoadSettings();
        switch (settings.AfterGameClose)
        {
            case AfterGameCloseOption.Restore:
                AppServices.MainWindow.RestoreFromTray();
                break;
            case AfterGameCloseOption.RestoreOnlyFromUI:
                if (lastLaunchChangedWindow)
                {
                    AppServices.MainWindow.RestoreFromTray();
                }

                break;
            case AfterGameCloseOption.Exit:
                AppServices.MainWindow.ExitApplication();
                break;
        }
    }
}
