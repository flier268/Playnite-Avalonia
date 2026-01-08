using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Addons;
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

    public bool CanToggleEnabled => SelectedAddon?.Manifest != null;
    public bool CanUninstall => SelectedAddon?.Manifest?.IsUserInstall == true;

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

        foreach (var addon in manager.GetInstalledExtensions())
        {
            Addons.Add(new AddonListItem(addon, disabled.Contains(addon.Id)));
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
        }
        else
        {
            settings.DisabledAddons.Add(id);
            Status = $"Disabled: {SelectedAddon.Manifest.Name}";
        }

        AppServices.SaveSettings(settings);
        Refresh();
    }
}

public sealed class AddonListItem
{
    public AddonListItem(AddonManifest manifest, bool disabled)
    {
        Manifest = manifest;
        Disabled = disabled;
    }

    public AddonManifest Manifest { get; }
    public bool Disabled { get; }

    public override string ToString()
    {
        var name = Manifest?.Name ?? string.Empty;
        var id = Manifest?.Id ?? string.Empty;
        var status = Disabled ? "disabled" : "enabled";
        return $"{name} ({id}) [{status}]";
    }
}
