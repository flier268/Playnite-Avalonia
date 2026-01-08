using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Configuration;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.DesktopApp.Avalonia.Services;

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

    private void ShowSettings()
    {
        StatusText = "Settings";
        CurrentView = settingsViewModel;
    }

    private void ShowAddons()
    {
        StatusText = "Add-ons";
        CurrentView = addonsViewModel;
    }
}
