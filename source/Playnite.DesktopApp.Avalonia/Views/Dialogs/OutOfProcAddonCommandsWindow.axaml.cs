using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Playnite.DesktopApp.Avalonia.Views.Dialogs;

public sealed partial class OutOfProcAddonCommandsWindow : Window
{
    public OutOfProcAddonCommandsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

