using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Playnite.Configuration;
using Playnite.Library;
using Playnite.Launching;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.Emulation;
using Playnite.FullscreenApp.Avalonia.Services;
using Playnite.Metadata;

namespace Playnite.FullscreenApp.Avalonia.ViewModels;

public sealed class FullscreenShellViewModel : INotifyPropertyChanged
{
    private readonly ILibraryStore libraryStore;
    private readonly IFullscreenHost host;
    private readonly AppSettings settings;
    private readonly GameActionLauncher launcher = new GameActionLauncher();
    private ObservableCollection<Game> games = new ObservableCollection<Game>();
    private Game? selectedGame;
    private GameAction? selectedAction;
    private GameRom? selectedRom;
    private Emulator? selectedEmulator;
    private EmulatorProfile? selectedEmulatorProfile;
    private string status = "Ready";
    private bool lastLaunchChangedWindow;
    private readonly ObservableCollection<Emulator> emulators = new ObservableCollection<Emulator>();
    private readonly ObservableCollection<EmulatorProfile> emulatorProfiles = new ObservableCollection<EmulatorProfile>();

    public FullscreenShellViewModel(ILibraryStore libraryStore, IFullscreenHost host, AppSettings settings)
    {
        this.libraryStore = libraryStore;
        this.host = host;
        this.settings = settings ?? new AppSettings();
        PlayCommand = new RelayCommand(() => _ = PlaySelectedAsync());
        ReloadCommand = new RelayCommand(() => Reload());
        ToggleInstallCommand = new RelayCommand(() => ToggleInstall());
        DownloadMetadataCommand = new RelayCommand(() => _ = DownloadMetadataAsync());
        SaveActionsCommand = new RelayCommand(() => SaveActions());
        SetPlayActionCommand = new RelayCommand(() => SetSelectedAsPlayAction());
        RemoveActionCommand = new RelayCommand(() => RemoveSelectedAction());
        AddFileActionCommand = new RelayCommand(() => AddAction(GameActionType.File));
        AddUrlActionCommand = new RelayCommand(() => AddAction(GameActionType.URL));
        AddEmulatorActionCommand = new RelayCommand(() => AddAction(GameActionType.Emulator));
        AddScriptActionCommand = new RelayCommand(() => AddAction(GameActionType.Script));
        Reload();
    }

    public FullscreenShellViewModel() : this(new EmptyLibraryStore(), new DummyFullscreenHost(), new AppSettings())
    {
    }

    public ObservableCollection<Game> Games
    {
        get => games;
        private set
        {
            games = value;
            OnPropertyChanged();
        }
    }

    public Game? SelectedGame
    {
        get => selectedGame;
        set
        {
            if (ReferenceEquals(selectedGame, value))
            {
                return;
            }

            selectedGame = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Actions));
            OnPropertyChanged(nameof(Roms));
            RefreshEmulators();
            SelectedAction = selectedGame?.GameActions?.FirstOrDefault(a => a?.IsPlayAction == true) ??
                             selectedGame?.GameActions?.FirstOrDefault();
            SelectedRom = Roms.FirstOrDefault();
        }
    }

    public ObservableCollection<GameAction> Actions => SelectedGame?.GameActions ?? new ObservableCollection<GameAction>();

    public GameAction? SelectedAction
    {
        get => selectedAction;
        set
        {
            if (ReferenceEquals(selectedAction, value))
            {
                return;
            }

            selectedAction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmulatorActionSelected));
            OnPropertyChanged(nameof(IsScriptActionSelected));

            if (selectedAction != null && selectedAction.Type == GameActionType.Emulator)
            {
                SelectedEmulator = emulators.FirstOrDefault(a => a.Id == selectedAction.EmulatorId);
                RefreshEmulatorProfiles();
                if (SelectedEmulator != null)
                {
                    var prof = SelectedEmulator.GetProfile(selectedAction.EmulatorProfileId);
                    SelectedEmulatorProfile = prof ?? emulatorProfiles.FirstOrDefault();
                }
            }
            else
            {
                SelectedEmulator = null;
                emulatorProfiles.Clear();
                SelectedEmulatorProfile = null;
            }
        }
    }

    public ObservableCollection<GameRom> Roms => SelectedGame?.Roms ?? new ObservableCollection<GameRom>();

    public GameRom? SelectedRom
    {
        get => selectedRom;
        set
        {
            if (ReferenceEquals(selectedRom, value))
            {
                return;
            }

            selectedRom = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Emulator> Emulators => emulators;

    public Emulator? SelectedEmulator
    {
        get => selectedEmulator;
        set
        {
            if (ReferenceEquals(selectedEmulator, value))
            {
                return;
            }

            selectedEmulator = value;
            OnPropertyChanged();
            RefreshEmulatorProfiles();

            if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator && selectedEmulator != null)
            {
                SelectedAction.EmulatorId = selectedEmulator.Id;
            }
        }
    }

    public ObservableCollection<EmulatorProfile> EmulatorProfiles => emulatorProfiles;

    public EmulatorProfile? SelectedEmulatorProfile
    {
        get => selectedEmulatorProfile;
        set
        {
            if (ReferenceEquals(selectedEmulatorProfile, value))
            {
                return;
            }

            selectedEmulatorProfile = value;
            OnPropertyChanged();

            if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator && selectedEmulatorProfile != null)
            {
                SelectedAction.EmulatorProfileId = selectedEmulatorProfile.Id;
            }
        }
    }

    public bool IsEmulatorActionSelected => SelectedAction?.Type == GameActionType.Emulator;
    public bool IsScriptActionSelected => SelectedAction?.Type == GameActionType.Script;

    public string Status
    {
        get => status;
        private set
        {
            if (status == value)
            {
                return;
            }

            status = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public ICommand PlayCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ToggleInstallCommand { get; }
    public ICommand DownloadMetadataCommand { get; }
    public ICommand SaveActionsCommand { get; }
    public ICommand SetPlayActionCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand AddFileActionCommand { get; }
    public ICommand AddUrlActionCommand { get; }
    public ICommand AddEmulatorActionCommand { get; }
    public ICommand AddScriptActionCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Reload()
    {
        Games = new ObservableCollection<Game>(libraryStore.LoadGames());
        SelectedGame = Games.FirstOrDefault();
        Status = $"Loaded: {Games.Count}";
    }

    private async Task DownloadMetadataAsync()
    {
        if (SelectedGame == null)
        {
            Status = "Download metadata (no selection)";
            return;
        }

        if (libraryStore is EmptyLibraryStore)
        {
            Status = "Download metadata (no DB)";
            return;
        }

        var provider = MetadataProviderRegistry.Default.Providers.FirstOrDefault();
        if (provider == null)
        {
            Status = "Download metadata (no provider)";
            return;
        }

        Status = "Downloading missing metadata...";

        var result = await Task.Run(() =>
        {
            var service = new MetadataDownloadService(MetadataProviderRegistry.Default.Providers, libraryStore);
            return service.Download(SelectedGame, provider.Id, overwriteExisting: false);
        });

        Status = result.Success ? "Missing metadata downloaded" : $"Metadata download failed: {result.ErrorMessage}";
        Reload();
    }

    private async Task PlaySelectedAsync()
    {
        if (SelectedGame == null)
        {
            Status = "Play (no selection)";
            return;
        }

        var action = SelectedAction ??
                     SelectedGame.GameActions?.FirstOrDefault(a => a?.IsPlayAction == true) ??
                     SelectedGame.GameActions?.FirstOrDefault();
        if (action == null)
        {
            Status = "Play (no action)";
            return;
        }

        SelectedGame.IsLaunching = true;
        SelectedGame.IsRunning = false;

        var result = action.Type == GameActionType.Emulator
            ? LaunchEmulator(SelectedGame, action)
            : launcher.Launch(SelectedGame, action);

        if (!result.Started)
        {
            SelectedGame.IsLaunching = false;
            Status = $"Start failed: {result.ErrorMessage}";
            return;
        }

        SelectedGame.IsLaunching = false;
        SelectedGame.IsRunning = true;
        SelectedGame.LastActivity = DateTime.Now;
        SelectedGame.PlayCount = SelectedGame.PlayCount + 1;
        (libraryStore as IGameStatsStore)?.TryUpdateGameStats(SelectedGame);

        ApplyAfterLaunch();
        Status = $"Started: {SelectedGame.Name}";

        if (result.Session == null)
        {
            SelectedGame.IsRunning = false;
            ApplyAfterGameClose();
            return;
        }

        try
        {
            await result.Session.WaitForExitAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        SelectedGame.IsRunning = false;
        var elapsed = DateTime.UtcNow - result.Session.StartTime;
        if (elapsed.TotalSeconds > 0)
        {
            try
            {
                SelectedGame.Playtime += (ulong)Math.Round(elapsed.TotalSeconds);
            }
            catch
            {
            }
        }

        (libraryStore as IGameStatsStore)?.TryUpdateGameStats(SelectedGame);
        await ApplyAfterGameCloseAsync().ConfigureAwait(false);
    }

    private void ApplyAfterLaunch()
    {
        lastLaunchChangedWindow = false;

        switch (settings.AfterLaunch)
        {
            case AfterLaunchOption.Minimize:
                lastLaunchChangedWindow = true;
                host.Minimize();
                break;
            case AfterLaunchOption.Close:
                lastLaunchChangedWindow = true;
                host.Hide();
                break;
            default:
                break;
        }
    }

    private void ApplyAfterGameClose()
    {
        // Kept for API parity with desktop; actual implementation is async (Exit can shutdown).
    }

    private async Task ApplyAfterGameCloseAsync()
    {
        switch (settings.AfterGameClose)
        {
            case AfterGameCloseOption.Restore:
                host.RestoreFullscreen();
                break;
            case AfterGameCloseOption.RestoreOnlyFromUI:
                if (lastLaunchChangedWindow)
                {
                    host.RestoreFullscreen();
                }
                break;
            case AfterGameCloseOption.Exit:
                await host.ShutdownAsync().ConfigureAwait(false);
                break;
            default:
                break;
        }
    }

    private void ToggleInstall()
    {
        if (SelectedGame == null)
        {
            Status = "Install/Uninstall (no selection)";
            return;
        }

        SelectedGame.IsInstalled = !SelectedGame.IsInstalled;
        libraryStore.TryUpdateGameInstallation(SelectedGame);
        Status = $"Install/Uninstall: {SelectedGame.Name}";
        OnPropertyChanged(nameof(SelectedGame));
    }

    private void SaveActions()
    {
        if (SelectedGame == null)
        {
            Status = "Save actions (no selection)";
            return;
        }

        var ok = libraryStore.TryUpdateGameActions(SelectedGame);
        Status = ok ? "Actions saved" : "Failed to save actions";
    }

    private void SetSelectedAsPlayAction()
    {
        if (SelectedGame?.GameActions == null || SelectedAction == null)
        {
            return;
        }

        foreach (var action in SelectedGame.GameActions)
        {
            action.IsPlayAction = false;
        }

        SelectedAction.IsPlayAction = true;
        Status = "Play action updated";
    }

    private void RemoveSelectedAction()
    {
        if (SelectedGame?.GameActions == null || SelectedAction == null)
        {
            return;
        }

        var idx = SelectedGame.GameActions.IndexOf(SelectedAction);
        SelectedGame.GameActions.Remove(SelectedAction);
        SelectedAction = SelectedGame.GameActions.Count > 0
            ? SelectedGame.GameActions[Math.Clamp(idx, 0, SelectedGame.GameActions.Count - 1)]
            : null;
        Status = "Action removed";
    }

    private void AddAction(GameActionType type)
    {
        if (SelectedGame == null)
        {
            return;
        }

        if (SelectedGame.GameActions == null)
        {
            SelectedGame.GameActions = new ObservableCollection<GameAction>();
            OnPropertyChanged(nameof(Actions));
        }

        var action = new GameAction
        {
            Type = type,
            Name = type.ToString(),
            IsPlayAction = SelectedGame.GameActions.Count == 0
        };

        SelectedGame.GameActions.Add(action);
        SelectedAction = action;
        Status = $"Action added: {type}";
    }

    private void RefreshEmulators()
    {
        emulators.Clear();
        foreach (var emulator in (libraryStore.LoadEmulators() ?? Array.Empty<Emulator>()).OrderBy(a => a.Name))
        {
            emulators.Add(emulator);
        }

        if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator)
        {
            SelectedEmulator = emulators.FirstOrDefault(a => a.Id == SelectedAction.EmulatorId);
            RefreshEmulatorProfiles();
        }
    }

    private void RefreshEmulatorProfiles()
    {
        emulatorProfiles.Clear();
        if (SelectedEmulator?.AllProfiles == null)
        {
            return;
        }

        foreach (var profile in SelectedEmulator.AllProfiles)
        {
            emulatorProfiles.Add(profile);
        }

        if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator)
        {
            var prof = SelectedEmulator.GetProfile(SelectedAction.EmulatorProfileId);
            SelectedEmulatorProfile = prof ?? emulatorProfiles.FirstOrDefault();
        }
    }

    private GameLaunchResult LaunchEmulator(Game game, GameAction emulatorAction)
    {
        var emulator = emulators.FirstOrDefault(a => a.Id == emulatorAction.EmulatorId) ??
                       libraryStore.LoadEmulators().FirstOrDefault(a => a.Id == emulatorAction.EmulatorId);
        if (emulator == null)
        {
            return GameLaunchResult.Failed("Emulator not found.");
        }

        var playniteDir = AppContext.BaseDirectory ?? string.Empty;
        var emulatorDir = LaunchVariableExpander.Expand(emulator.InstallDir ?? string.Empty, game, string.Empty, string.Empty, playniteDir);

        var romPath = SelectedRom?.Path ?? game.Roms?.FirstOrDefault()?.Path ?? string.Empty;
        romPath = LaunchVariableExpander.Expand(romPath, game, emulatorDir, string.Empty, playniteDir);

        var profile = emulator.GetProfile(emulatorAction.EmulatorProfileId);
        if (profile is BuiltInEmulatorProfile builtInProfile)
        {
            return LaunchBuiltInEmulatorProfile(game, emulator, emulatorAction, emulatorDir, romPath, playniteDir, builtInProfile);
        }

        if (profile is not CustomEmulatorProfile customProfile)
        {
            // Compatibility: allow passing BuiltInProfileName directly.
            if (!string.IsNullOrWhiteSpace(emulatorAction.EmulatorProfileId) &&
                !emulatorAction.EmulatorProfileId.StartsWith("#custom_", StringComparison.Ordinal) &&
                !emulatorAction.EmulatorProfileId.StartsWith("#builtin_", StringComparison.Ordinal))
            {
                return LaunchBuiltInEmulatorProfile(game, emulator, emulatorAction, emulatorDir, romPath, playniteDir, null);
            }

            return GameLaunchResult.Failed("Emulator profile is not set.");
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

        var finalArgs = emulatorAction.OverrideDefaultArgs ? extraArgs : CombineArgs(baseArgs, extraArgs);

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

    private GameLaunchResult LaunchBuiltInEmulatorProfile(Game game, Emulator emulator, GameAction emulatorAction, string emulatorDir, string romPath, string playniteDir, BuiltInEmulatorProfile? builtInProfile)
    {
        var profileName = builtInProfile?.BuiltInProfileName ?? emulatorAction.EmulatorProfileId;
        if (!BuiltInEmulatorDefinitionStore.TryGetProfile(emulator.BuiltInConfigId, profileName, out var profileDef))
        {
            return GameLaunchResult.Failed($"Built-in emulator profile not found: {emulator.BuiltInConfigId} / {profileName}");
        }

        if (profileDef.ScriptStartup)
        {
            return LaunchBuiltInEmulatorProfileScript(game, emulator, emulatorAction, profileName, emulatorDir, romPath, playniteDir);
        }

        if (!EmulatorExecutableFinder.TryFindExecutable(emulatorDir, profileDef.StartupExecutable, out var executable))
        {
            return GameLaunchResult.Failed($"Emulator executable not found (regex: {profileDef.StartupExecutable}).");
        }

        string baseArgs;
        if (builtInProfile?.OverrideDefaultArgs == true)
        {
            baseArgs = LaunchVariableExpander.Expand(builtInProfile.CustomArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        }
        else
        {
            baseArgs = LaunchVariableExpander.Expand(profileDef.StartupArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        }

        var extraArgs = LaunchVariableExpander.Expand(emulatorAction.AdditionalArguments ?? string.Empty, game, emulatorDir, romPath, playniteDir);
        var finalArgs = emulatorAction.OverrideDefaultArgs ? extraArgs : CombineArgs(baseArgs, extraArgs);

        var trackingMode = emulatorAction.TrackingMode;
        if (trackingMode == TrackingMode.Default)
        {
            trackingMode = OperatingSystem.IsWindows() ? TrackingMode.Process : TrackingMode.OriginalProcess;
        }

        var trackingPath = !string.IsNullOrWhiteSpace(emulatorAction.TrackingPath)
            ? LaunchVariableExpander.Expand(emulatorAction.TrackingPath, game, emulatorDir, romPath, playniteDir)
            : emulatorDir;

        var fileAction = new GameAction
        {
            Type = GameActionType.File,
            Path = executable,
            WorkingDir = emulatorDir,
            Arguments = finalArgs,
            TrackingMode = trackingMode,
            TrackingPath = trackingPath,
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

        var wrapper = BuildEmulatorPowerShellWrapper(expandedBody, emulator, customProfile.Name ?? string.Empty, emulatorDir, romPath);

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

    private GameLaunchResult LaunchBuiltInEmulatorProfileScript(Game game, Emulator emulator, GameAction emulatorAction, string profileName, string emulatorDir, string romPath, string playniteDir)
    {
        if (!BuiltInEmulatorDefinitionStore.TryGetStartupScriptPath(emulator.BuiltInConfigId, out var startupScriptPath) ||
            string.IsNullOrWhiteSpace(startupScriptPath) ||
            !File.Exists(startupScriptPath))
        {
            return GameLaunchResult.Failed($"Startup script not found for built-in emulator definition: {emulator.BuiltInConfigId}");
        }

        var wrapper = BuildEmulatorPowerShellWrapperForFile(startupScriptPath, emulator, profileName, emulatorDir, romPath);

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

        if (string.IsNullOrWhiteSpace(a))
        {
            return b;
        }

        if (string.IsNullOrWhiteSpace(b))
        {
            return a;
        }

        return a + " " + b;
    }

    private static string ResolvePath(string path, string baseDir)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            if (string.IsNullOrWhiteSpace(baseDir))
            {
                return path;
            }

            return Path.Combine(baseDir, path);
        }
        catch
        {
            return path;
        }
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

    private sealed class DummyFullscreenHost : IFullscreenHost
    {
        public void Minimize() { }
        public void Hide() { }
        public void RestoreFullscreen() { }
        public Task ShutdownAsync() => Task.CompletedTask;
    }
}
