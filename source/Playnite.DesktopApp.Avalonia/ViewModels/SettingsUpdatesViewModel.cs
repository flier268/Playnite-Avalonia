using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsUpdatesViewModel : INotifyPropertyChanged
{
    private string status = string.Empty;

    public SettingsUpdatesViewModel()
    {
        OpenReleasesPageCommand = new RelayCommand(OpenReleasesPage);
        OpenProjectHomeCommand = new RelayCommand(OpenProjectHome);
    }

    public string Header => "Updates";

    public string Description =>
        "Update behavior settings (auto-check, notifications). Actual update installation is not wired yet.";

    public UpdateChannel[] UpdateChannelOptions { get; } =
    [
        UpdateChannel.Stable,
        UpdateChannel.Nightly
    ];

    public UpdateChannel UpdateChannel
    {
        get => AppServices.LoadSettings().UpdateChannel;
        set
        {
            var settings = AppServices.LoadSettings();
            if (settings.UpdateChannel == value)
            {
                return;
            }

            settings.UpdateChannel = value;
            AppServices.SaveSettings(settings);
            OnPropertyChanged();
        }
    }

    public bool CheckForUpdatesOnStartup
    {
        get => AppServices.LoadSettings().CheckForUpdatesOnStartup;
        set
        {
            var settings = AppServices.LoadSettings();
            if (settings.CheckForUpdatesOnStartup == value)
            {
                return;
            }

            settings.CheckForUpdatesOnStartup = value;
            AppServices.SaveSettings(settings);
            OnPropertyChanged();
        }
    }

    public bool NotifyOnUpdates
    {
        get => AppServices.LoadSettings().NotifyOnUpdates;
        set
        {
            var settings = AppServices.LoadSettings();
            if (settings.NotifyOnUpdates == value)
            {
                return;
            }

            settings.NotifyOnUpdates = value;
            AppServices.SaveSettings(settings);
            OnPropertyChanged();
        }
    }

    public bool AutoDownloadUpdates
    {
        get => AppServices.LoadSettings().AutoDownloadUpdates;
        set
        {
            var settings = AppServices.LoadSettings();
            if (settings.AutoDownloadUpdates == value)
            {
                return;
            }

            settings.AutoDownloadUpdates = value;
            AppServices.SaveSettings(settings);
            OnPropertyChanged();
        }
    }

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

    public ICommand OpenReleasesPageCommand { get; }
    public ICommand OpenProjectHomeCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OpenReleasesPage()
    {
        var url = UpdateChannel == UpdateChannel.Nightly
            ? "https://github.com/JosefNemec/Playnite/releases"
            : "https://github.com/JosefNemec/Playnite/releases";

        OpenExternal(url);
    }

    private void OpenProjectHome()
    {
        OpenExternal("https://playnite.link/");
    }

    private void OpenExternal(string urlOrPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = urlOrPath,
                UseShellExecute = true
            });

            Status = string.Empty;
        }
        catch (System.Exception e)
        {
            Status = e.Message;
        }
    }
}
