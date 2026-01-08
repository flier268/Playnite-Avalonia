using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsAppearanceViewModel : INotifyPropertyChanged
{
    private AppTheme selectedTheme;

    public string Header => "Appearance";
    public string Description => "Theme and layout preferences (more to be ported from WPF).";

    public IReadOnlyList<AppTheme> ThemeOptions { get; } = new[]
    {
        AppTheme.Default,
        AppTheme.Light,
        AppTheme.Dark
    };

    public AppTheme SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (selectedTheme == value)
            {
                return;
            }

            selectedTheme = value;
            ApplyAndSaveTheme();
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsAppearanceViewModel()
    {
        var settings = AppServices.LoadSettings();
        selectedTheme = settings.Theme;
    }

    private void ApplyAndSaveTheme()
    {
        var settings = AppServices.LoadSettings();
        settings.Theme = selectedTheme;
        AppServices.SaveSettings(settings);

        ThemeService.Apply(selectedTheme);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
