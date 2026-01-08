using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Addons;
using Playnite.Addons.OutOfProc;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class AddonsInstalledExtensionsViewModel : INotifyPropertyChanged
{
    private readonly AddonsManager manager = AddonsManager.CreateDefault();
    private AddonListItem? selectedAddon;
    private string status = string.Empty;

    public AddonsInstalledExtensionsViewModel()
    {
        Addons = new ObservableCollection<AddonListItem>();

        RefreshCommand = new RelayCommand(Refresh);
        InstallFromFileCommand = new RelayCommand(() => TaskUtilities.FireAndForget(InstallFromFileAsync()));
        UninstallCommand = new RelayCommand(UninstallSelected);
        ToggleEnabledCommand = new RelayCommand(ToggleEnabledSelected);
        RestartOutOfProcCommand = new RelayCommand(RestartSelectedOutOfProc);
        CopyOutOfProcStatusCommand = new RelayCommand(() => TaskUtilities.FireAndForget(CopySelectedOutOfProcStatusAsync()));
        ViewOutOfProcLogCommand = new RelayCommand(() => TaskUtilities.FireAndForget(ViewSelectedOutOfProcLogAsync()));
        ViewOutOfProcCommandsCommand = new RelayCommand(() => TaskUtilities.FireAndForget(ViewSelectedOutOfProcCommandsAsync()));

        AppServices.AddonsChanged += (_, _) => Refresh();

        Refresh();
    }

    public string Header => "Installed extensions";
    public string Description => "Install, update (reinstall), uninstall extensions.";

    public ObservableCollection<AddonListItem> Addons { get; }

    public AddonListItem? SelectedAddon
    {
        get => selectedAddon;
        set
        {
            if (ReferenceEquals(selectedAddon, value))
            {
                return;
            }

            selectedAddon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanToggleEnabled));
            OnPropertyChanged(nameof(CanUninstall));
            OnPropertyChanged(nameof(CanManageOutOfProc));
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

    public ICommand RefreshCommand { get; }
    public ICommand InstallFromFileCommand { get; }
    public ICommand UninstallCommand { get; }
    public ICommand ToggleEnabledCommand { get; }
    public ICommand RestartOutOfProcCommand { get; }
    public ICommand CopyOutOfProcStatusCommand { get; }
    public ICommand ViewOutOfProcLogCommand { get; }
    public ICommand ViewOutOfProcCommandsCommand { get; }

    public bool CanToggleEnabled => SelectedAddon?.Manifest != null;
    public bool CanUninstall => SelectedAddon?.Manifest?.IsUserInstall == true;
    public bool CanManageOutOfProc => SelectedAddon?.IsOutOfProc == true;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Refresh()
    {
        Addons.Clear();

        var settings = AppServices.LoadSettings();
        var disabled = settings.DisabledAddons?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
        var hostStatuses = AppServices.OutOfProcAddonsHost?.GetStatusSnapshots()
            ?.ToDictionary(a => a.AddonId, StringComparer.OrdinalIgnoreCase) ?? new();

        foreach (var addon in manager.GetInstalledExtensions())
        {
            if (OutOfProcAddonResolver.IsOutOfProc(addon))
            {
                hostStatuses.TryGetValue(addon.Id, out var status);
                Addons.Add(new AddonListItem(addon, disabled.Contains(addon.Id), isOutOfProc: true, outOfProcStatus: status));
            }
            else
            {
                Addons.Add(new AddonListItem(addon, disabled.Contains(addon.Id), isOutOfProc: false, outOfProcStatus: null));
            }
        }

        SelectedAddon = Addons.Count > 0 ? Addons[0] : null;
        Status = $"Extensions: {Addons.Count}";
    }

    private async System.Threading.Tasks.Task InstallFromFileAsync()
    {
        var path = await FilePickerService.PickPackageFileAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var result = manager.InstallFromPackage(path);
        Status = result.Success ? $"Installed: {result.Manifest?.Name ?? result.Manifest?.Id}" : result.ErrorMessage;
        AppServices.NotifyAddonsChanged();
        Refresh();
    }

    private void UninstallSelected()
    {
        if (SelectedAddon is null)
        {
            return;
        }

        if (SelectedAddon.Manifest == null)
        {
            return;
        }

        if (!SelectedAddon.Manifest.IsUserInstall)
        {
            Status = "Cannot uninstall built-in add-ons.";
            return;
        }

        if (!manager.Uninstall(SelectedAddon.Manifest))
        {
            Status = "Uninstall failed.";
            return;
        }

        Status = $"Uninstalled: {SelectedAddon.Manifest.Name}";
        AppServices.NotifyAddonsChanged();
        Refresh();
    }

    private void ToggleEnabledSelected()
    {
        if (SelectedAddon?.Manifest == null)
        {
            return;
        }

        var host = AppServices.OutOfProcAddonsHost;

        var settings = AppServices.LoadSettings();
        settings.DisabledAddons ??= new System.Collections.Generic.List<string>();

        var id = SelectedAddon.Manifest.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (settings.DisabledAddons.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            settings.DisabledAddons = settings.DisabledAddons.Where(a => !string.Equals(a, id, StringComparison.OrdinalIgnoreCase)).ToList();
            Status = $"Enabled: {SelectedAddon.Manifest.Name}";
            if (host != null && OutOfProcAddonResolver.IsOutOfProc(SelectedAddon.Manifest))
            {
                host.TryStart(SelectedAddon.Manifest, out _);
            }
        }
        else
        {
            settings.DisabledAddons.Add(id);
            Status = $"Disabled: {SelectedAddon.Manifest.Name}";
            if (host != null && OutOfProcAddonResolver.IsOutOfProc(SelectedAddon.Manifest))
            {
                host.TryStop(id);
            }
        }

        AppServices.SaveSettings(settings);
        Refresh();
    }

    private void RestartSelectedOutOfProc()
    {
        if (SelectedAddon?.Manifest == null || !SelectedAddon.IsOutOfProc)
        {
            return;
        }

        var host = AppServices.OutOfProcAddonsHost;
        if (host == null)
        {
            Status = "Out-of-proc host unavailable.";
            return;
        }

        var id = SelectedAddon.Manifest.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        host.TryStop(id);
        if (!host.TryStart(SelectedAddon.Manifest, out var error))
        {
            Status = string.IsNullOrWhiteSpace(error) ? "Restart failed." : error;
        }
        else
        {
            Status = $"Restarted: {SelectedAddon.Manifest.Name}";
        }

        Refresh();
    }

    private async System.Threading.Tasks.Task CopySelectedOutOfProcStatusAsync()
    {
        if (SelectedAddon == null || !SelectedAddon.IsOutOfProc)
        {
            return;
        }

        if (AppServices.MainWindow == null)
        {
            return;
        }

        var text = SelectedAddon.BuildOutOfProcDetailsText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            await AppServices.MainWindow.Clipboard.SetTextAsync(text);
            Status = "Copied out-of-proc status to clipboard.";
        }
        catch
        {
            Status = "Failed to copy to clipboard.";
        }
    }

    private async System.Threading.Tasks.Task ViewSelectedOutOfProcLogAsync()
    {
        if (SelectedAddon == null || !SelectedAddon.IsOutOfProc)
        {
            return;
        }

        if (AppServices.MainWindow == null)
        {
            return;
        }

        var window = new Views.Dialogs.OutOfProcAddonStatusWindow
        {
            DataContext = new ViewModels.Dialogs.OutOfProcAddonStatusViewModel(
                title: SelectedAddon.Manifest.Name,
                detailsText: SelectedAddon.BuildOutOfProcDetailsText())
        };

        try
        {
            await window.ShowDialog(AppServices.MainWindow);
        }
        catch
        {
        }
    }

    private async System.Threading.Tasks.Task ViewSelectedOutOfProcCommandsAsync()
    {
        if (SelectedAddon == null || !SelectedAddon.IsOutOfProc)
        {
            return;
        }

        if (AppServices.MainWindow == null)
        {
            return;
        }

        var id = SelectedAddon.Manifest.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var window = new Views.Dialogs.OutOfProcAddonCommandsWindow
        {
            DataContext = new ViewModels.Dialogs.OutOfProcAddonCommandsViewModel(
                title: $"{SelectedAddon.Manifest.Name} - Commands",
                addonId: id)
        };

        try
        {
            await window.ShowDialog(AppServices.MainWindow);
        }
        catch
        {
        }
    }
}

public sealed class AddonListItem
{
    public AddonListItem(AddonManifest manifest, bool disabled, bool isOutOfProc, OutOfProcAddonStatus outOfProcStatus)
    {
        Manifest = manifest;
        Disabled = disabled;
        IsOutOfProc = isOutOfProc;
        OutOfProcStatus = outOfProcStatus;
    }

    public AddonManifest Manifest { get; }
    public bool Disabled { get; }
    public bool IsOutOfProc { get; }
    public OutOfProcAddonStatus OutOfProcStatus { get; }

    public bool HasOutOfProcStatus => OutOfProcStatus != null;

    public string OutOfProcStatusText
    {
        get
        {
            if (!IsOutOfProc)
            {
                return string.Empty;
            }

            if (OutOfProcStatus == null)
            {
                return "OutOfProc: not started";
            }

            var running = OutOfProcStatus.IsRunning ? "running" : "stopped";
            var err = string.IsNullOrWhiteSpace(OutOfProcStatus.LastError) ? string.Empty : $" error: {OutOfProcStatus.LastError}";
            var tailLine = (OutOfProcStatus.StderrTail != null && OutOfProcStatus.StderrTail.Count > 0)
                ? $" stderr: {OutOfProcStatus.StderrTail[OutOfProcStatus.StderrTail.Count - 1]}"
                : string.Empty;
            return $"OutOfProc: {running}{err}{tailLine}";
        }
    }

    public string BuildOutOfProcDetailsText()
    {
        if (!IsOutOfProc)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Name: {Manifest?.Name ?? string.Empty}");
        sb.AppendLine($"Id: {Manifest?.Id ?? string.Empty}");
        sb.AppendLine($"Version: {Manifest?.Version}");
        sb.AppendLine($"Mode: {Manifest?.Mode ?? string.Empty}");
        sb.AppendLine();

        if (OutOfProcStatus == null)
        {
            sb.AppendLine("Status: not started");
            return sb.ToString();
        }

        sb.AppendLine($"Status: {(OutOfProcStatus.IsRunning ? "running" : "stopped")}");
        if (OutOfProcStatus.LastStartUtc != default)
        {
            sb.AppendLine($"LastStartUtc: {OutOfProcStatus.LastStartUtc:O}");
        }

        if (!string.IsNullOrWhiteSpace(OutOfProcStatus.LastError))
        {
            sb.AppendLine($"LastError: {OutOfProcStatus.LastError}");
        }

        sb.AppendLine();
        sb.AppendLine("stderr tail:");
        if (OutOfProcStatus.StderrTail != null && OutOfProcStatus.StderrTail.Count > 0)
        {
            foreach (var line in OutOfProcStatus.StderrTail)
            {
                sb.AppendLine(line);
            }
        }
        else
        {
            sb.AppendLine("(empty)");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var name = Manifest?.Name ?? string.Empty;
        var id = Manifest?.Id ?? string.Empty;
        var status = Disabled ? "disabled" : "enabled";
        return $"{name} ({id}) [{status}]";
    }
}
