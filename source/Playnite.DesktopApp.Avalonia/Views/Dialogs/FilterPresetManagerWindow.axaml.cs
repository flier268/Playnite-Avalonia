using Avalonia.Controls;
using Avalonia.Interactivity;
using Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

namespace Playnite.DesktopApp.Avalonia.Views.Dialogs;

public sealed partial class FilterPresetManagerWindow : Window
{
    public FilterPresetManagerWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FilterPresetManagerViewModel viewModel)
        {
            if (viewModel.TrySave())
            {
                Close(true);
                return;
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _ = CancelAsync();
    }

    private async System.Threading.Tasks.Task CancelAsync()
    {
        if (DataContext is FilterPresetManagerViewModel viewModel && viewModel.IsDirty)
        {
            var ok = await ConfirmWindow.ShowAsync(this, "Discard changes", "Discard unsaved changes?");
            if (!ok)
            {
                return;
            }
        }

        Close(false);
    }
}
