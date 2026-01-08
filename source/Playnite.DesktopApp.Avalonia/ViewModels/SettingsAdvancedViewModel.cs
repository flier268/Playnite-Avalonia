using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.Library;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsAdvancedViewModel : INotifyPropertyChanged
{
    private string status = string.Empty;

    public SettingsAdvancedViewModel()
    {
        OpenUserDataFolderCommand = new RelayCommand(OpenUserDataFolder);
        OpenSettingsFileCommand = new RelayCommand(OpenSettingsFile);
        OpenLibraryRootCommand = new RelayCommand(OpenLibraryRoot);
        ReloadLibraryCommand = new RelayCommand(ReloadLibrary);
        ResetSettingsCommand = new RelayCommand(ResetSettings);
    }

    public string Header => "Advanced";

    public string Description =>
        "Diagnostics and advanced controls. Use this page to inspect paths and reset/reload state.";

    public string SettingsFilePath => AppServices.SettingsStore?.FilePath ?? string.Empty;

    public string UserDataPath
    {
        get
        {
            var filePath = SettingsFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetDirectoryName(filePath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public string LibraryRootPath => AppServices.LibraryStore?.RootPath ?? string.Empty;

    public string EnvUserDataPath => Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH") ?? string.Empty;
    public string EnvDbPath => Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH") ?? string.Empty;

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

    public ICommand OpenUserDataFolderCommand { get; }
    public ICommand OpenSettingsFileCommand { get; }
    public ICommand OpenLibraryRootCommand { get; }
    public ICommand ReloadLibraryCommand { get; }
    public ICommand ResetSettingsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OpenUserDataFolder()
    {
        OpenExternal(UserDataPath);
    }

    private void OpenSettingsFile()
    {
        OpenExternal(SettingsFilePath);
    }

    private void OpenLibraryRoot()
    {
        OpenExternal(LibraryRootPath);
    }

    private void ReloadLibrary()
    {
        try
        {
            var settings = AppServices.LoadSettings();
            var store = LibraryStoreFactory.Create(settings);
            AppServices.InitializeLibraryStore(store);
            OnPropertyChanged(nameof(LibraryRootPath));
            Status = store is EmptyLibraryStore ? "Library store: Mock (no DB)" : $"Library store reloaded: {store.RootPath}";
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }

    private void ResetSettings()
    {
        try
        {
            AppServices.SaveSettings(new Playnite.Configuration.AppSettings());
            Status = "Settings reset to defaults.";
            OnPropertyChanged(nameof(SettingsFilePath));
            OnPropertyChanged(nameof(UserDataPath));
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }

    private void OpenExternal(string urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            Status = "Path is empty.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = urlOrPath,
                UseShellExecute = true
            });

            Status = string.Empty;
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }
}
