using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.Library;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsLibrariesViewModel : INotifyPropertyChanged
{
    private string libraryDbPath = string.Empty;
    private string steamPath = string.Empty;
    private string epicManifestsPath = string.Empty;
    private string status = string.Empty;

    public SettingsLibrariesViewModel()
    {
        var settings = AppServices.LoadSettings();
        libraryDbPath = settings.LibraryDbPath ?? string.Empty;
        steamPath = settings.SteamPath ?? string.Empty;
        epicManifestsPath = settings.EpicManifestsPath ?? string.Empty;
        SaveAndReloadCommand = new RelayCommand(() => SaveAndReload());
    }

    public string Header => "Libraries";

    public string LibraryDbPath
    {
        get => libraryDbPath;
        set
        {
            if (libraryDbPath == value)
            {
                return;
            }

            libraryDbPath = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string SteamPath
    {
        get => steamPath;
        set
        {
            if (steamPath == value)
            {
                return;
            }

            steamPath = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string EpicManifestsPath
    {
        get => epicManifestsPath;
        set
        {
            if (epicManifestsPath == value)
            {
                return;
            }

            epicManifestsPath = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string CurrentStoreRoot => AppServices.LibraryStore?.RootPath ?? string.Empty;

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

    public ICommand SaveAndReloadCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SaveAndReload()
    {
        var settings = AppServices.LoadSettings();
        settings.LibraryDbPath = LibraryDbPath ?? string.Empty;
        settings.SteamPath = SteamPath ?? string.Empty;
        settings.EpicManifestsPath = EpicManifestsPath ?? string.Empty;
        AppServices.SaveSettings(settings);

        var store = LibraryStoreFactory.Create(settings);
        AppServices.InitializeLibraryStore(store);
        OnPropertyChanged(nameof(CurrentStoreRoot));
        Status = store is EmptyLibraryStore
            ? "Library store: (not found) Set Libraries -> DB path or PLAYNITE_DB_PATH / PLAYNITE_USERDATA_PATH."
            : $"Library store: {store.RootPath}";
    }
}
