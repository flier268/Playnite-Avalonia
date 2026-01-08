using System;
using Avalonia;
using Avalonia.Controls;
using Playnite.Configuration;

namespace Playnite.DesktopApp.Avalonia.Services;

public sealed class TrayWindowBehavior
{
    private readonly Window window;
    private bool allowClose;

    public TrayWindowBehavior(Window window)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        this.window.Closing += Window_Closing;
        this.window.PropertyChanged += Window_PropertyChanged;
    }

    public void AllowCloseOnce()
    {
        allowClose = true;
    }

    public void RestoreFromTray()
    {
        window.ShowInTaskbar = true;
        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    private void Window_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Window.WindowStateProperty)
        {
            return;
        }

        if (window.WindowState != WindowState.Minimized)
        {
            return;
        }

        var settings = AppServices.LoadSettings();
        if (!settings.EnableTray || !settings.MinimizeToTray)
        {
            return;
        }

        HideToTray();
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        if (allowClose)
        {
            return;
        }

        var settings = AppServices.LoadSettings();
        if (!settings.EnableTray || !settings.CloseToTray)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void HideToTray()
    {
        window.Hide();
        window.ShowInTaskbar = false;
    }
}
