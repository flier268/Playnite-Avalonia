using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Playnite.Configuration;
using Playnite.Addons;
using Playnite.Addons.OutOfProc;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.Library;
using Playnite.LibraryImport;
using Playnite.Metadata;
using Avalonia.Threading;

namespace Playnite.DesktopApp.Avalonia
{
    public sealed partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime desktopLifetime;
        private MainWindow mainWindow;
        private TrayIcon trayIcon;
        private OutOfProcAddonsHost outOfProcAddonsHost;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            trayIcon = ((TrayIcons)GetValue(TrayIcon.IconsProperty))?.FirstOrDefault();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktopLifetime = desktop;
                var settingsStore = AppSettingsStoreFactory.CreateFromEnvironment();
                AppServices.Initialize(settingsStore);
                var settings = settingsStore.Load();
                ThemeService.Apply(settings.Theme);
                CultureService.Apply(settings.Language);

                var libraryStore = LibraryStoreFactory.Create(settings);
                if (libraryStore is not EmptyLibraryStore
                    && string.IsNullOrWhiteSpace(settings.LibraryDbPath)
                    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH")))
                {
                    settings.LibraryDbPath = libraryStore.RootPath;
                    settingsStore.Save(settings);
                }
                AppServices.InitializeLibraryStore(libraryStore);
                MetadataProviderRegistry.Default.Register(new LocalFilesMetadataProvider());

                if (libraryStore is not EmptyLibraryStore && settings.UpdateLibStartup)
                {
                    Task.Run(() =>
                    {
                        var svc = new LibraryImportService(libraryStore);
                        _ = svc.ImportSteam();
                        _ = svc.ImportEpic();
                    }).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() => AppServices.InitializeLibraryStore(libraryStore));
                    });
                }

                try
                {
                    outOfProcAddonsHost = new OutOfProcAddonsHost(
                        AddonsManager.CreateDefault(),
                        new OutOfProcAddonsHostOptions
                        {
                            RequestTimeoutMs = settings.OutOfProcAddonRequestTimeoutMs,
                            RestartLimitPerMinute = settings.OutOfProcAddonRestartLimitPerMinute,
                            StderrTailLines = settings.OutOfProcAddonStderrTailLines
                        });
                    AppServices.InitializeOutOfProcAddonsHost(outOfProcAddonsHost);
                    outOfProcAddonsHost.StartAllEnabled(settings);
                    desktop.Exit += (_, _) => outOfProcAddonsHost?.Dispose();
                }
                catch
                {
                }

                mainWindow = new MainWindow();
                AppServices.InitializeShell(mainWindow, libraryStore, new GameLaunchService());
                if (settings.StartMinimized)
                {
                    mainWindow.WindowState = WindowState.Minimized;
                }
                else if (settings.StartInFullscreen)
                {
                    mainWindow.WindowState = WindowState.FullScreen;
                }

                desktop.MainWindow = mainWindow;
                if (trayIcon != null)
                {
                    trayIcon.IsVisible = settings.EnableTray;
                }

                AppServices.SettingsChanged += (_, __) =>
                {
                    var updated = AppServices.LoadSettings();
                    if (trayIcon != null)
                    {
                        trayIcon.IsVisible = updated.EnableTray;
                    }
                    ThemeService.Apply(updated.Theme);
                    CultureService.Apply(updated.Language);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnTrayIconClicked(object sender, EventArgs e)
        {
            mainWindow?.RestoreFromTray();
        }

        private void OnTrayShowClicked(object sender, EventArgs e)
        {
            mainWindow?.RestoreFromTray();
        }

        private void OnTrayExitClicked(object sender, EventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.AllowCloseOnce();
            }

            try
            {
                outOfProcAddonsHost?.Dispose();
            }
            catch
            {
            }

            desktopLifetime?.Shutdown();
        }
    }
}
