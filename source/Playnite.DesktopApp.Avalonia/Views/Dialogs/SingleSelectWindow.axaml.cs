using Avalonia.Controls;
using Avalonia.Interactivity;
using Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

namespace Playnite.DesktopApp.Avalonia.Views.Dialogs;

public sealed partial class SingleSelectWindow : Window
{
    public SingleSelectWindow()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SingleSelectViewModel viewModel)
        {
            Close(viewModel.SelectedItem);
        }
        else
        {
            Close(null);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnItemsDoubleTapped(object? sender, RoutedEventArgs e)
    {
        OnOkClick(sender, e);
    }
}

