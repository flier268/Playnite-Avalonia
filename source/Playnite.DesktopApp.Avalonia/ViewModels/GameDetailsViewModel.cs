using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.DesktopApp.Avalonia.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Playnite.Library;
using Playnite.Metadata;
using System.Collections.Generic;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class GameDetailsViewModel : INotifyPropertyChanged
{
    private readonly IDesktopNavigationService navigation;
    private Game? game;
    private string lastAction = "None";
    private GameAction? selectedAction;
    private Emulator? selectedEmulator;
    private readonly ObservableCollection<Emulator> emulators = new ObservableCollection<Emulator>();
    private readonly ObservableCollection<MetadataProviderListItem> metadataProviders = new ObservableCollection<MetadataProviderListItem>();
    private MetadataProviderListItem? selectedMetadataProvider;

    public GameDetailsViewModel(IDesktopNavigationService navigation)
    {
        this.navigation = navigation;
        BackCommand = new RelayCommand(() => navigation.ShowLibrary());
        PlayCommand = new RelayCommand(() =>
        {
            if (Game == null)
            {
                LastAction = "Play (no game)";
                return;
            }
            
            LastAction = $"Play: {Game.Name}";
            TaskUtilities.FireAndForget(AppServices.GameLaunchService.LaunchAsync(Game));
        });
        InstallCommand = new RelayCommand(() =>
        {
            if (Game == null)
            {
                LastAction = "Install/Uninstall (no game)";
                return;
            }

            Game.IsInstalled = !Game.IsInstalled;
            AppServices.LibraryStore?.TryUpdateGameInstallation(Game);
            LastAction = $"Install/Uninstall: {Game.Name}";

            if (Game.IsInstalled && AppServices.LoadSettings().ScanLibInstallSizeOnLibUpdate)
            {
                TaskUtilities.FireAndForget(RescanInstallSizeAsync());
            }
        });

        AddFileActionCommand = new RelayCommand(() => AddAction(GameActionType.File));
        AddUrlActionCommand = new RelayCommand(() => AddAction(GameActionType.URL));
        AddEmulatorActionCommand = new RelayCommand(() => AddAction(GameActionType.Emulator));
        AddScriptActionCommand = new RelayCommand(() => AddAction(GameActionType.Script));
        RemoveActionCommand = new RelayCommand(() => RemoveSelectedAction());
        SetPlayActionCommand = new RelayCommand(() => SetSelectedAsPlayAction());
        SaveActionsCommand = new RelayCommand(() => SaveActions());
        RescanInstallSizeCommand = new RelayCommand(() => TaskUtilities.FireAndForget(RescanInstallSizeAsync()));
        RefreshMetadataProvidersCommand = new RelayCommand(() => RefreshMetadataProviders());
        DownloadMetadataCommand = new RelayCommand(() => TaskUtilities.FireAndForget(DownloadMetadataAsync()));

        AppServices.AddonsChanged += (_, _) => RefreshMetadataProviders();
        RefreshMetadataProviders();
    }

    public Game? Game
    {
        get => game;
        set
        {
            if (ReferenceEquals(game, value))
            {
                return;
            }

            game = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Actions));

            SelectedAction = game?.GameActions?.FirstOrDefault();
            LoadEmulators();
        }
    }

    public ObservableCollection<GameAction> Actions => Game?.GameActions ?? new ObservableCollection<GameAction>();

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

            if (selectedAction != null && selectedAction.Type == GameActionType.Emulator)
            {
                SelectedEmulator = emulators.FirstOrDefault(a => a.Id == selectedAction.EmulatorId);
            }
            else
            {
                SelectedEmulator = null;
            }
        }
    }

    public ObservableCollection<Emulator> Emulators => emulators;

    public ObservableCollection<MetadataProviderListItem> MetadataProviders => metadataProviders;

    public MetadataProviderListItem? SelectedMetadataProvider
    {
        get => selectedMetadataProvider;
        set
        {
            if (ReferenceEquals(selectedMetadataProvider, value))
            {
                return;
            }

            selectedMetadataProvider = value;
            OnPropertyChanged();
        }
    }

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

            if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator && selectedEmulator != null)
            {
                SelectedAction.EmulatorId = selectedEmulator.Id;
            }
        }
    }

    public ICommand BackCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand AddFileActionCommand { get; }
    public ICommand AddUrlActionCommand { get; }
    public ICommand AddEmulatorActionCommand { get; }
    public ICommand AddScriptActionCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand SetPlayActionCommand { get; }
    public ICommand SaveActionsCommand { get; }
    public ICommand RescanInstallSizeCommand { get; }
    public ICommand RefreshMetadataProvidersCommand { get; }
    public ICommand DownloadMetadataCommand { get; }

    public string LastAction
    {
        get => lastAction;
        private set
        {
            if (lastAction == value)
            {
                return;
            }

            lastAction = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LoadEmulators()
    {
        emulators.Clear();
        if (AppServices.LibraryStore == null)
        {
            return;
        }

        foreach (var emulator in AppServices.LibraryStore.LoadEmulators().OrderBy(a => a.Name))
        {
            emulators.Add(emulator);
        }

        if (SelectedAction != null && SelectedAction.Type == GameActionType.Emulator)
        {
            SelectedEmulator = emulators.FirstOrDefault(a => a.Id == SelectedAction.EmulatorId);
        }
    }

    private void AddAction(GameActionType type)
    {
        if (Game == null)
        {
            return;
        }

        if (Game.GameActions == null)
        {
            Game.GameActions = new ObservableCollection<GameAction>();
        }

        var action = new GameAction
        {
            Type = type,
            Name = type.ToString(),
            IsPlayAction = Game.GameActions.Count == 0
        };

        Game.GameActions.Add(action);
        SelectedAction = action;
        LastAction = $"Action added: {type}";
    }

    private void RemoveSelectedAction()
    {
        if (Game?.GameActions == null || SelectedAction == null)
        {
            return;
        }

        var idx = Game.GameActions.IndexOf(SelectedAction);
        Game.GameActions.Remove(SelectedAction);
        SelectedAction = Game.GameActions.Count > 0
            ? Game.GameActions[Math.Clamp(idx, 0, Game.GameActions.Count - 1)]
            : null;
        LastAction = "Action removed";
    }

    private void SetSelectedAsPlayAction()
    {
        if (Game?.GameActions == null || SelectedAction == null)
        {
            return;
        }

        foreach (var action in Game.GameActions)
        {
            action.IsPlayAction = false;
        }

        SelectedAction.IsPlayAction = true;
        LastAction = "Play action updated";
    }

    private void SaveActions()
    {
        if (Game == null)
        {
            return;
        }

        var ok = AppServices.LibraryStore?.TryUpdateGameActions(Game) == true;
        LastAction = ok ? "Actions saved" : "Failed to save actions";
    }

    private void RefreshMetadataProviders()
    {
        metadataProviders.Clear();

        var store = AppServices.LibraryStore;
        if (store == null || store is EmptyLibraryStore)
        {
            metadataProviders.Add(new MetadataProviderListItem(string.Empty, "(no library store)", "No DB loaded"));
            SelectedMetadataProvider = metadataProviders.FirstOrDefault();
            return;
        }

        var providers = MetadataProviderRegistry.Default.Providers;
        foreach (var p in providers)
        {
            metadataProviders.Add(new MetadataProviderListItem(p.Id, p.Name, string.Empty));
        }

        if (providers.Count == 0)
        {
            metadataProviders.Add(new MetadataProviderListItem(string.Empty, "(none)", "No metadata providers registered."));
        }

        SelectedMetadataProvider = metadataProviders.FirstOrDefault();
    }

    private async System.Threading.Tasks.Task DownloadMetadataAsync()
    {
        if (Game == null)
        {
            LastAction = "Download metadata (no game)";
            return;
        }

        var store = AppServices.LibraryStore;
        if (store == null || store is EmptyLibraryStore)
        {
            LastAction = "Download metadata (no DB)";
            return;
        }

        var providerId = SelectedMetadataProvider?.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(providerId))
        {
            LastAction = "Download metadata (no provider)";
            return;
        }

        LastAction = "Downloading metadata...";

        var result = await System.Threading.Tasks.Task.Run(() =>
        {
            var service = new MetadataDownloadService(MetadataProviderRegistry.Default.Providers, store);
            return service.Download(Game, providerId);
        });

        LastAction = result.Success ? "Metadata downloaded" : $"Metadata download failed: {result.ErrorMessage}";
        OnPropertyChanged(nameof(Game));
    }

    private async System.Threading.Tasks.Task RescanInstallSizeAsync()
    {
        if (Game == null)
        {
            LastAction = "Rescan install size (no game)";
            return;
        }

        var installDir = Game.InstallDirectory ?? string.Empty;
        if (string.IsNullOrWhiteSpace(installDir))
        {
            LastAction = "Rescan install size (missing install directory)";
            return;
        }

        LastAction = "Scanning install size...";

        var ok = await System.Threading.Tasks.Task.Run(() =>
        {
            return InstallSizeScanner.TryGetDirectorySizeBytes(installDir, out var bytes) ? (true, bytes) : (false, 0UL);
        });

        if (!ok.Item1)
        {
            LastAction = "Failed to scan install size";
            return;
        }

        Game.InstallSize = ok.Item2;
        Game.LastSizeScanDate = DateTime.UtcNow;

        var saved = AppServices.LibraryStore?.TryUpdateGameInstallSize(Game) == true;
        LastAction = saved ? "Install size updated" : "Failed to save install size";
        OnPropertyChanged(nameof(Game));
    }
}
