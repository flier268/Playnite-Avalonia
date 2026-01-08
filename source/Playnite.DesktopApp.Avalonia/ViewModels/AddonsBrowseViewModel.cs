using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Addons;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class AddonsBrowseViewModel : INotifyPropertyChanged
{
    private readonly AddonsManager manager = AddonsManager.CreateDefault();
    private string browsePath = string.Empty;
    private AddonPackageItem? selectedPackage;
    private string status = string.Empty;

    public AddonsBrowseViewModel()
    {
        Packages = new ObservableCollection<AddonPackageItem>();

        ChooseFolderCommand = new RelayCommand(() => TaskUtilities.FireAndForget(ChooseFolderAsync()));
        RefreshCommand = new RelayCommand(Refresh);
        InstallSelectedCommand = new RelayCommand(InstallSelected);

        var settings = AppServices.LoadSettings();
        browsePath = settings.AddonsBrowsePath ?? string.Empty;

        AppServices.AddonsChanged += (_, _) => Refresh();
        Refresh();
    }

    public string Header => "Browse";

    public string Description =>
        "Browse local add-on packages (.pext/.pthm) from a folder and install them.";

    public ObservableCollection<AddonPackageItem> Packages { get; }

    public AddonPackageItem? SelectedPackage
    {
        get => selectedPackage;
        set
        {
            if (ReferenceEquals(selectedPackage, value))
            {
                return;
            }

            selectedPackage = value;
            OnPropertyChanged();
        }
    }

    public string BrowsePath
    {
        get => browsePath;
        private set
        {
            if (browsePath == value)
            {
                return;
            }

            browsePath = value ?? string.Empty;
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

    public ICommand ChooseFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand InstallSelectedCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async System.Threading.Tasks.Task ChooseFolderAsync()
    {
        var path = await FilePickerService.PickFolderAsync("Select add-ons package folder");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var settings = AppServices.LoadSettings();
        settings.AddonsBrowsePath = path;
        AppServices.SaveSettings(settings);

        BrowsePath = path;
        Refresh();
    }

    private void Refresh()
    {
        Packages.Clear();

        var root = BrowsePath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            Status = "Select a folder containing .pext/.pthm packages.";
            return;
        }

        var installedIds = manager.GetInstalledExtensions().Select(a => a.Id)
            .Concat(manager.GetInstalledThemes().Select(a => a.Id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = Enumerable.Empty<string>();
        try
        {
            files = Directory.EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly)
                .Where(p =>
                {
                    var ext = Path.GetExtension(p);
                    return string.Equals(ext, AddonsManager.PackedExtensionFileExtention, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(ext, AddonsManager.PackedThemeFileExtention, StringComparison.OrdinalIgnoreCase);
                });
        }
        catch
        {
        }

        foreach (var file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var manifest = manager.TryReadPackageManifest(file);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
            {
                continue;
            }

            Packages.Add(new AddonPackageItem(file, manifest, installedIds.Contains(manifest.Id)));
        }

        SelectedPackage = Packages.FirstOrDefault();
        Status = $"Packages: {Packages.Count}";
    }

    private void InstallSelected()
    {
        if (SelectedPackage is null)
        {
            return;
        }

        var result = manager.InstallFromPackage(SelectedPackage.PackagePath);
        if (!result.Success)
        {
            Status = result.ErrorMessage;
            return;
        }

        Status = $"Installed: {result.Manifest?.Name ?? result.Manifest?.Id}";
        AppServices.NotifyAddonsChanged();
        Refresh();
    }
}

public sealed class AddonPackageItem
{
    public AddonPackageItem(string packagePath, AddonManifest manifest, bool isInstalled)
    {
        PackagePath = packagePath ?? string.Empty;
        Manifest = manifest;
        IsInstalled = isInstalled;
    }

    public string PackagePath { get; }
    public AddonManifest Manifest { get; }
    public bool IsInstalled { get; }
    public override string ToString() => $"{Manifest?.Name ?? string.Empty} ({Manifest?.Id ?? string.Empty})";
}

