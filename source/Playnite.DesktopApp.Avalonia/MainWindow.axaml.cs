using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.DesktopApp.Avalonia.ViewModels;

namespace Playnite.DesktopApp.Avalonia
{
    public sealed partial class MainWindow : Window
    {
        private TrayWindowBehavior trayBehavior;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DesktopShellViewModel();
            trayBehavior = new TrayWindowBehavior(this);
        }

        internal void RestoreFromTray()
        {
            trayBehavior.RestoreFromTray();
        }

        internal void HideToTray()
        {
            ShowInTaskbar = false;
            Hide();
        }

        internal void AllowCloseOnce()
        {
            trayBehavior.AllowCloseOnce();
        }

        internal void ExitApplication()
        {
            AllowCloseOnce();
            if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Close();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
