using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.Library;
using Playnite.Metadata;

namespace Playnite.DesktopApp.Avalonia
{
    public sealed partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime desktopLifetime;
        private MainWindow mainWindow;
        private TrayIcon trayIcon;

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
                AppServices.InitializeLibraryStore(libraryStore);
                MetadataProviderRegistry.Default.Register(new LocalFilesMetadataProvider());
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

            desktopLifetime?.Shutdown();
        }
    }
}
