using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsGeneralViewModel : INotifyPropertyChanged
{
    private readonly AppSettings settings;
    private string statusMessage = string.Empty;
    private CultureOption selectedLanguage;

    public string Header => "General";
    public string Description => "Startup, tray, import, and performance options.";

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            if (statusMessage == value)
            {
                return;
            }

            statusMessage = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<CultureOption> LanguageOptions { get; }

    public CultureOption SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            if (ReferenceEquals(selectedLanguage, value))
            {
                return;
            }

            selectedLanguage = value;
            settings.Language = selectedLanguage?.Name ?? string.Empty;
            CultureService.Apply(settings.Language);
            Save();
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<AfterLaunchOption> AfterLaunchOptions { get; } = new[]
    {
        AfterLaunchOption.None,
        AfterLaunchOption.Minimize,
        AfterLaunchOption.Close
    };

    public AfterLaunchOption AfterLaunch
    {
        get => settings.AfterLaunch;
        set
        {
            if (settings.AfterLaunch == value)
            {
                return;
            }

            settings.AfterLaunch = value;
            Save();
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<AfterGameCloseOption> AfterGameCloseOptions { get; } = new[]
    {
        AfterGameCloseOption.None,
        AfterGameCloseOption.Restore,
        AfterGameCloseOption.RestoreOnlyFromUI,
        AfterGameCloseOption.Exit
    };

    public AfterGameCloseOption AfterGameClose
    {
        get => settings.AfterGameClose;
        set
        {
            if (settings.AfterGameClose == value)
            {
                return;
            }

            settings.AfterGameClose = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool DownloadMetadataOnImport
    {
        get => settings.DownloadMetadataOnImport;
        set
        {
            if (settings.DownloadMetadataOnImport == value)
            {
                return;
            }

            settings.DownloadMetadataOnImport = value;
            Save();
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<PlaytimeImportMode> PlaytimeImportModeOptions { get; } = new[]
    {
        PlaytimeImportMode.Always,
        PlaytimeImportMode.NewImportsOnly,
        PlaytimeImportMode.Never
    };

    public PlaytimeImportMode PlaytimeImportMode
    {
        get => settings.PlaytimeImportMode;
        set
        {
            if (settings.PlaytimeImportMode == value)
            {
                return;
            }

            settings.PlaytimeImportMode = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool EnableTray
    {
        get => settings.EnableTray;
        set
        {
            if (settings.EnableTray == value)
            {
                return;
            }

            settings.EnableTray = value;
            if (!settings.EnableTray)
            {
                settings.MinimizeToTray = false;
                settings.CloseToTray = false;
                OnPropertyChanged(nameof(MinimizeToTray));
                OnPropertyChanged(nameof(CloseToTray));
            }

            Save();
            OnPropertyChanged();
        }
    }

    public bool MinimizeToTray
    {
        get => settings.MinimizeToTray;
        set
        {
            if (settings.MinimizeToTray == value)
            {
                return;
            }

            settings.MinimizeToTray = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool CloseToTray
    {
        get => settings.CloseToTray;
        set
        {
            if (settings.CloseToTray == value)
            {
                return;
            }

            settings.CloseToTray = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool StartInFullscreen
    {
        get => settings.StartInFullscreen;
        set
        {
            if (settings.StartInFullscreen == value)
            {
                return;
            }

            settings.StartInFullscreen = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool StartMinimized
    {
        get => settings.StartMinimized;
        set
        {
            if (settings.StartMinimized == value)
            {
                return;
            }

            settings.StartMinimized = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool StartOnBoot
    {
        get => settings.StartOnBoot;
        set
        {
            if (settings.StartOnBoot == value)
            {
                return;
            }

            StatusMessage = string.Empty;
            if (!AutoStartService.IsSupported())
            {
                StatusMessage = "Start on boot is not supported on this OS.";
                OnPropertyChanged();
                return;
            }

            if (!AutoStartService.TrySetEnabled(value, out var error))
            {
                StatusMessage = $"Failed to update start-on-boot: {error}";
                OnPropertyChanged();
                return;
            }

            settings.StartOnBoot = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool UpdateLibStartup
    {
        get => settings.UpdateLibStartup;
        set
        {
            if (settings.UpdateLibStartup == value)
            {
                return;
            }

            settings.UpdateLibStartup = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool DisableHwAcceleration
    {
        get => settings.DisableHwAcceleration;
        set
        {
            if (settings.DisableHwAcceleration == value)
            {
                return;
            }

            settings.DisableHwAcceleration = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool ScanLibInstallSizeOnLibUpdate
    {
        get => settings.ScanLibInstallSizeOnLibUpdate;
        set
        {
            if (settings.ScanLibInstallSizeOnLibUpdate == value)
            {
                return;
            }

            settings.ScanLibInstallSizeOnLibUpdate = value;
            Save();
            OnPropertyChanged();
        }
    }

    public bool FuzzyMatchingInNameFilter
    {
        get => settings.FuzzyMatchingInNameFilter;
        set
        {
            if (settings.FuzzyMatchingInNameFilter == value)
            {
                return;
            }

            settings.FuzzyMatchingInNameFilter = value;
            Save();
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public SettingsGeneralViewModel()
    {
        settings = AppServices.LoadSettings();

        LanguageOptions = BuildDefaultLanguageOptions(settings.Language);
        selectedLanguage = LanguageOptions.FirstOrDefault(a => a.Name == settings.Language) ?? LanguageOptions.FirstOrDefault();
        if (selectedLanguage != null && string.IsNullOrWhiteSpace(settings.Language))
        {
            settings.Language = selectedLanguage.Name;
            Save();
        }

        CultureService.Apply(settings.Language);
    }

    private void Save()
    {
        AppServices.SaveSettings(settings);
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static IReadOnlyList<CultureOption> BuildDefaultLanguageOptions(string preferredCultureName)
    {
        var defaultNames = new[]
        {
            preferredCultureName,
            CultureInfo.CurrentUICulture.Name,
            "en-US",
            "zh-TW",
            "zh-CN",
            "ja-JP",
            "de-DE",
            "fr-FR"
        }.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();

        return defaultNames
            .Select(name =>
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(name);
                    return new CultureOption(culture.Name, culture.DisplayName);
                }
                catch
                {
                    return null;
                }
            })
            .Where(a => a != null)
            .ToList();
    }
}

public sealed class CultureOption
{
    public CultureOption(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }

    public string Name { get; }
    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}
