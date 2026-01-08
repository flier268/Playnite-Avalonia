using System;
using Playnite.DesktopApp.Avalonia;
using Playnite.Addons.OutOfProc;
using Playnite.Configuration;
using Playnite.Library;

namespace Playnite.DesktopApp.Avalonia.Services;

public static class AppServices
{
    public static AppSettingsStore SettingsStore { get; private set; }
    public static IGameLaunchService GameLaunchService { get; private set; }
    public static MainWindow MainWindow { get; private set; }
    public static ILibraryStore LibraryStore { get; private set; }
    public static OutOfProcAddonsHost OutOfProcAddonsHost { get; private set; }
    public static event EventHandler SettingsChanged;
    public static event EventHandler LibraryStoreChanged;
    public static event EventHandler AddonsChanged;

    public static void Initialize(AppSettingsStore settingsStore)
    {
        SettingsStore = settingsStore;
    }

    public static void InitializeShell(MainWindow mainWindow, ILibraryStore libraryStore, IGameLaunchService gameLaunchService)
    {
        MainWindow = mainWindow;
        LibraryStore = libraryStore;
        LibraryStoreChanged?.Invoke(null, EventArgs.Empty);
        GameLaunchService = gameLaunchService;
    }

    public static void InitializeOutOfProcAddonsHost(OutOfProcAddonsHost host)
    {
        OutOfProcAddonsHost = host;
    }

    public static void InitializeLibraryStore(ILibraryStore libraryStore)
    {
        LibraryStore = libraryStore;
        LibraryStoreChanged?.Invoke(null, EventArgs.Empty);
    }

    public static AppSettings LoadSettings()
    {
        return SettingsStore.Load();
    }

    public static void SaveSettings(AppSettings settings)
    {
        SettingsStore.Save(settings);
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyAddonsChanged()
    {
        AddonsChanged?.Invoke(null, EventArgs.Empty);
    }
}
