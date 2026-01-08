using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsAboutViewModel : INotifyPropertyChanged
{
    private string status = string.Empty;

    public SettingsAboutViewModel()
    {
        OpenProjectHomeCommand = new RelayCommand(() => OpenExternal("https://playnite.link/"));
        OpenReleasesCommand = new RelayCommand(() => OpenExternal("https://github.com/JosefNemec/Playnite/releases"));
        OpenIssuesCommand = new RelayCommand(() => OpenExternal("https://github.com/JosefNemec/Playnite/issues"));
        OpenUserDataCommand = new RelayCommand(() => OpenExternal(UserDataPath));
    }

    public string Header => "About";

    public string Description =>
        "Version information, runtime environment, and useful links.";

    public string AppVersion
    {
        get
        {
            try
            {
                var exe = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(exe) || !System.IO.File.Exists(exe))
                {
                    return "Unknown";
                }

                var info = FileVersionInfo.GetVersionInfo(exe);
                return info.ProductVersion ?? info.FileVersion ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public string Framework => RuntimeInformation.FrameworkDescription;
    public string Os => RuntimeInformation.OSDescription;
    public string ProcessArchitecture => RuntimeInformation.ProcessArchitecture.ToString();

    public string UserDataPath
    {
        get
        {
            try
            {
                var filePath = AppServices.SettingsStore?.FilePath ?? string.Empty;
                return string.IsNullOrWhiteSpace(filePath) ? string.Empty : (System.IO.Path.GetDirectoryName(filePath) ?? string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public string LibraryRootPath => AppServices.LibraryStore?.RootPath ?? string.Empty;

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

    public ICommand OpenProjectHomeCommand { get; }
    public ICommand OpenReleasesCommand { get; }
    public ICommand OpenIssuesCommand { get; }
    public ICommand OpenUserDataCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OpenExternal(string urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            Status = "Path is empty.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = urlOrPath,
                UseShellExecute = true
            });

            Status = string.Empty;
        }
        catch (Exception e)
        {
            Status = e.Message;
        }
    }
}
