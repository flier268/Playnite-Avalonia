using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

namespace Playnite.DesktopApp.Avalonia.Views.Dialogs;

public sealed partial class CommandPaletteWindow : Window
{
    public CommandPaletteWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FilterBox?.Focus();
        KeyDown += OnKeyDown;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnItemsDoubleTapped(object? sender, RoutedEventArgs e)
    {
        RunSelectedAndClose();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            RunSelectedAndClose();
            e.Handled = true;
        }
    }

    private void RunSelectedAndClose()
    {
        if (DataContext is CommandPaletteViewModel vm && vm.CanRun)
        {
            vm.RunSelectedCommand.Execute(null);
        }

        Close();
    }
}

