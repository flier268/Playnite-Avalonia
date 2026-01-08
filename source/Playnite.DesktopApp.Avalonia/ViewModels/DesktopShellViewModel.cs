using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Configuration;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;
using Playnite.DesktopApp.Avalonia.Views.Dialogs;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class DesktopShellViewModel : INotifyPropertyChanged, IDesktopNavigationService
{
    private string statusText = "Ready";
    private object currentView;
    private readonly LibraryViewModel libraryViewModel;
    private readonly GameDetailsViewModel gameDetailsViewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly AddonsViewModel addonsViewModel;

    public string Title => "Playnite Desktop";

    public string StatusText
    {
        get => statusText;
        set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    public string FooterText => "Avalonia shell active - UI migration ongoing.";

    public ICommand ShowLibraryCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ShowAddonsCommand { get; }
    public ICommand OpenCommandPaletteCommand { get; }

    public object CurrentView
    {
        get => currentView;
        private set
        {
            if (ReferenceEquals(currentView, value))
            {
                return;
            }

            currentView = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DesktopShellViewModel()
    {
        ShowLibraryCommand = new RelayCommand(() => ShowLibrary());
        ShowAddonsCommand = new RelayCommand(() => ShowAddons());
        ShowSettingsCommand = new RelayCommand(() => ShowSettings());
        OpenCommandPaletteCommand = new RelayCommand(() => TaskUtilities.FireAndForget(OpenCommandPaletteAsync()));
        libraryViewModel = new LibraryViewModel(this);
        gameDetailsViewModel = new GameDetailsViewModel(this);
        addonsViewModel = new AddonsViewModel();
        settingsViewModel = new SettingsViewModel();

        var store = AppServices.SettingsStore ?? AppSettingsStoreFactory.CreateFromEnvironment();
        var settings = store.Load();
        if (settings.UpdateLibStartup)
        {
            libraryViewModel.Reload();
        }

        CurrentView = libraryViewModel;
        StatusText = "Library";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ShowLibrary()
    {
        StatusText = "Library";
        CurrentView = libraryViewModel;
    }

    public void ShowGameDetails(Game game)
    {
        gameDetailsViewModel.Game = game;
        StatusText = $"Details: {game?.Name}";
        CurrentView = gameDetailsViewModel;
    }

    public Game? GetCurrentGame()
    {
        if (CurrentView == gameDetailsViewModel && gameDetailsViewModel.Game != null)
        {
            return gameDetailsViewModel.Game;
        }

        return libraryViewModel.SelectedGame;
    }

    public void ShowSettings()
    {
        StatusText = "Settings";
        CurrentView = settingsViewModel;
    }

    public void ShowAddons()
    {
        StatusText = "Add-ons";
        CurrentView = addonsViewModel;
    }

    public void ShowAddonsSection(string sectionName)
    {
        ShowAddons();
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            return;
        }

        var section = addonsViewModel.Sections.FirstOrDefault(a => string.Equals(a.Name, sectionName, System.StringComparison.OrdinalIgnoreCase));
        if (section != null)
        {
            addonsViewModel.SelectedSection = section;
        }
    }

    public void ShowSettingsSection(string sectionName)
    {
        ShowSettings();
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            return;
        }

        var section = settingsViewModel.Sections.FirstOrDefault(a => string.Equals(a.Name, sectionName, System.StringComparison.OrdinalIgnoreCase));
        if (section != null)
        {
            settingsViewModel.SelectedSection = section;
        }
    }

    public void RescanAllInstallSizes()
    {
        libraryViewModel.RescanAllInstallSizesCommand.Execute(null);
        StatusText = "Rescanning all install sizes...";
        CurrentView = libraryViewModel;
    }

    public void DownloadMissingMetadataForFiltered()
    {
        libraryViewModel.DownloadMissingMetadataForFilteredCommand.Execute(null);
        StatusText = "Downloading missing metadata for filtered...";
        CurrentView = libraryViewModel;
    }

    public void PlayCurrentGame()
    {
        var game = GetCurrentGame();
        if (game == null)
        {
            StatusText = "No game selected.";
            return;
        }

        libraryViewModel.PlayCommand.Execute(game);
        StatusText = $"Play: {game.Name}";
    }

    public void OpenCurrentGameDetails()
    {
        var game = GetCurrentGame();
        if (game == null)
        {
            StatusText = "No game selected.";
            return;
        }

        ShowGameDetails(game);
    }

    public void ToggleCurrentGameFavorite()
    {
        var game = GetCurrentGame();
        if (game == null)
        {
            StatusText = "No game selected.";
            return;
        }

        libraryViewModel.ToggleFavoriteCommand.Execute(game);
        StatusText = $"Toggled favorite: {game.Name}";
    }

    public void ToggleCurrentGameHidden()
    {
        var game = GetCurrentGame();
        if (game == null)
        {
            StatusText = "No game selected.";
            return;
        }

        libraryViewModel.ToggleHiddenCommand.Execute(game);
        StatusText = $"Toggled hidden: {game.Name}";
    }

    public void DownloadMetadataForCurrentGame(bool overwriteExisting)
    {
        var game = GetCurrentGame();
        if (game == null)
        {
            StatusText = "No game selected.";
            return;
        }

        if (overwriteExisting)
        {
            libraryViewModel.DownloadMetadataCommand.Execute(game);
            StatusText = $"Download metadata: {game.Name}";
        }
        else
        {
            libraryViewModel.DownloadMissingMetadataCommand.Execute(game);
            StatusText = $"Download missing metadata: {game.Name}";
        }
    }

    public void ReloadLibrary()
    {
        libraryViewModel.Reload();
        StatusText = "Library reloaded";
        CurrentView = libraryViewModel;
    }

    private async System.Threading.Tasks.Task OpenCommandPaletteAsync()
    {
        if (AppServices.MainWindow == null)
        {
            return;
        }

        var window = new CommandPaletteWindow
        {
            DataContext = new CommandPaletteViewModel(this)
        };

        try
        {
            await window.ShowDialog(AppServices.MainWindow);
        }
        catch
        {
        }
    }
}
