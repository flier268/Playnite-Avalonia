using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private object currentSection;
    private SettingsSection? selectedSection;

    public SettingsViewModel()
    {
        Sections = new ObservableCollection<SettingsSection>
        {
            new SettingsSection("General", new SettingsGeneralViewModel()),
            new SettingsSection("Appearance", new SettingsAppearanceViewModel()),
            new SettingsSection("Libraries", new SettingsLibrariesViewModel()),
            new SettingsSection("Updates", new SettingsUpdatesViewModel()),
            new SettingsSection("Advanced", new SettingsAdvancedViewModel()),
            new SettingsSection("About", new SettingsAboutViewModel())
        };

        SelectedSection = Sections.FirstOrDefault();
    }

    public string Title => "Settings";

    public ObservableCollection<SettingsSection> Sections { get; }

    public SettingsSection? SelectedSection
    {
        get => selectedSection;
        set
        {
            if (ReferenceEquals(selectedSection, value))
            {
                return;
            }

            selectedSection = value;
            CurrentSection = selectedSection?.ViewModel ?? new SettingsGeneralViewModel();
            OnPropertyChanged();
        }
    }

    public object CurrentSection
    {
        get => currentSection;
        private set
        {
            if (ReferenceEquals(currentSection, value))
            {
                return;
            }

            currentSection = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SettingsSection
{
    public SettingsSection(string name, object viewModel)
    {
        Name = name;
        ViewModel = viewModel;
    }

    public string Name { get; }
    public object ViewModel { get; }

    public override string ToString() => Name;
}
