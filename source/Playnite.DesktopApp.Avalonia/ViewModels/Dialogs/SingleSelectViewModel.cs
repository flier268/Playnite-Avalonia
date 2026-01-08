using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class SingleSelectViewModel : INotifyPropertyChanged
{
    private SingleSelectItem? selectedItem;

    public SingleSelectViewModel(string title, IEnumerable<SingleSelectItem> items)
    {
        Title = title ?? string.Empty;
        Items = new ObservableCollection<SingleSelectItem>((items ?? Enumerable.Empty<SingleSelectItem>()).ToList());
        SelectedItem = Items.FirstOrDefault();
    }

    public string Title { get; }

    public ObservableCollection<SingleSelectItem> Items { get; }

    public SingleSelectItem? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (ReferenceEquals(selectedItem, value))
            {
                return;
            }

            selectedItem = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

