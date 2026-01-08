using Avalonia;
using Avalonia.Styling;
using Playnite.Configuration;

namespace Playnite.FullscreenApp.Avalonia.Services;

public static class ThemeService
{
    public static void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}

