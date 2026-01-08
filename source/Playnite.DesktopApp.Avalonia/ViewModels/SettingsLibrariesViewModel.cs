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
    private string status = string.Empty;

    public SettingsLibrariesViewModel()
    {
        var settings = AppServices.LoadSettings();
        libraryDbPath = settings.LibraryDbPath ?? string.Empty;
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
        AppServices.SaveSettings(settings);

        var store = LibraryStoreFactory.Create(settings);
        AppServices.InitializeLibraryStore(store);
        OnPropertyChanged(nameof(CurrentStoreRoot));
        Status = store is EmptyLibraryStore ? "Library store: Mock (no DB)" : $"Library store: {store.RootPath}";
    }
}
