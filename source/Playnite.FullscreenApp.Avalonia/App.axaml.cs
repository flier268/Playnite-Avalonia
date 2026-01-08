using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Playnite.Configuration;
using Playnite.Addons;
using Playnite.Addons.OutOfProc;
using Playnite.Library;
using Playnite.FullscreenApp.Avalonia.ViewModels;
using Playnite.FullscreenApp.Avalonia.Services;
using Avalonia.Threading;
using System.Threading.Tasks;
using Playnite.Metadata;

namespace Playnite.FullscreenApp.Avalonia
{
    public sealed partial class App : Application
    {
        private OutOfProcAddonsHost outOfProcAddonsHost;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settingsStore = AppSettingsStoreFactory.CreateFromEnvironment();
                var settings = settingsStore.Load();
                ThemeService.Apply(settings.Theme);
                CultureService.Apply(settings.Language);
                var store = LibraryStoreFactory.Create(settings);
                MetadataProviderRegistry.Default.Register(new LocalFilesMetadataProvider());

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
                    outOfProcAddonsHost.StartAllEnabled(settings);
                    desktop.Exit += (_, _) => outOfProcAddonsHost?.Dispose();
                }
                catch
                {
                }

                var window = new MainWindow
                {
                    WindowState = WindowState.FullScreen
                };

                var host = new FullscreenHost(window, desktop);
                window.DataContext = new FullscreenShellViewModel(store, host, settings);

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    internal sealed class FullscreenHost : IFullscreenHost
    {
        private readonly Window window;
        private readonly IClassicDesktopStyleApplicationLifetime lifetime;

        public FullscreenHost(Window window, IClassicDesktopStyleApplicationLifetime lifetime)
        {
            this.window = window;
            this.lifetime = lifetime;
        }

        public void Minimize()
        {
            Dispatcher.UIThread.Post(() => window.WindowState = WindowState.Minimized);
        }

        public void Hide()
        {
            Dispatcher.UIThread.Post(() => window.Hide());
        }

        public void RestoreFullscreen()
        {
            Dispatcher.UIThread.Post(() =>
            {
                window.Show();
                window.WindowState = WindowState.FullScreen;
                window.Activate();
            });
        }

        public Task ShutdownAsync()
        {
            return Dispatcher.UIThread.InvokeAsync(() => lifetime.Shutdown()).GetTask();
        }
    }
}
