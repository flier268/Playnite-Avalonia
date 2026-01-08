using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Addons;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class AddonsInstalledThemesViewModel : INotifyPropertyChanged
{
    private readonly AddonsManager manager = AddonsManager.CreateDefault();
    private AddonManifest? selectedTheme;
    private string status = string.Empty;

    public AddonsInstalledThemesViewModel()
    {
        Themes = new ObservableCollection<AddonManifest>();
        RefreshCommand = new RelayCommand(Refresh);
        InstallFromFileCommand = new RelayCommand(() => TaskUtilities.FireAndForget(InstallFromFileAsync()));
        UninstallCommand = new RelayCommand(UninstallSelected);

        AppServices.AddonsChanged += (_, _) => Refresh();
        Refresh();
    }

    public string Header => "Installed themes";
    public string Description => "Install, update (reinstall), uninstall themes.";

    public ObservableCollection<AddonManifest> Themes { get; }

    public AddonManifest? SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (ReferenceEquals(selectedTheme, value))
            {
                return;
            }

            selectedTheme = value;
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

    public ICommand RefreshCommand { get; }
    public ICommand InstallFromFileCommand { get; }
    public ICommand UninstallCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Refresh()
    {
        Themes.Clear();
        foreach (var theme in manager.GetInstalledThemes())
        {
            Themes.Add(theme);
        }

        SelectedTheme = Themes.Count > 0 ? Themes[0] : null;
        Status = $"Themes: {Themes.Count}";
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
        if (SelectedTheme is null)
        {
            return;
        }

        if (!SelectedTheme.IsUserInstall)
        {
            Status = "Cannot uninstall built-in add-ons.";
            return;
        }

        if (!manager.Uninstall(SelectedTheme))
        {
            Status = "Uninstall failed.";
            return;
        }

        Status = $"Uninstalled: {SelectedTheme.Name}";
        AppServices.NotifyAddonsChanged();
        Refresh();
    }
}
