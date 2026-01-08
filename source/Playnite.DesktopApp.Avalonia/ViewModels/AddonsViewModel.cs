using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class AddonsViewModel : INotifyPropertyChanged
{
    private object currentSection;
    private AddonsSection? selectedSection;

    public AddonsViewModel()
    {
        Sections = new ObservableCollection<AddonsSection>
        {
            new AddonsSection("Browse", new AddonsBrowseViewModel()),
            new AddonsSection("Installed extensions", new AddonsInstalledExtensionsViewModel()),
            new AddonsSection("Installed themes", new AddonsInstalledThemesViewModel())
        };

        SelectedSection = Sections.FirstOrDefault();
    }

    public string Title => "Add-ons";

    public ObservableCollection<AddonsSection> Sections { get; }

    public AddonsSection? SelectedSection
    {
        get => selectedSection;
        set
        {
            if (ReferenceEquals(selectedSection, value))
            {
                return;
            }

            selectedSection = value;
            CurrentSection = selectedSection?.ViewModel ?? new AddonsBrowseViewModel();
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

public sealed class AddonsSection
{
    public AddonsSection(string name, object viewModel)
    {
        Name = name;
        ViewModel = viewModel;
    }

    public string Name { get; }
    public object ViewModel { get; }

    public override string ToString() => Name;
}
