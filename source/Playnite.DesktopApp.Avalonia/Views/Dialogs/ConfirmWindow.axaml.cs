using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Playnite.DesktopApp.Avalonia.Views.Dialogs;

public sealed partial class ConfirmWindow : Window
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var window = new ConfirmWindow
        {
            Title = title ?? "Confirm",
            DataContext = new ConfirmWindowViewModel(message ?? string.Empty)
        };

        try
        {
            return await window.ShowDialog<bool>(owner);
        }
        catch
        {
            return false;
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

public sealed class ConfirmWindowViewModel
{
    public ConfirmWindowViewModel(string message)
    {
        Message = message ?? string.Empty;
    }

    public string Message { get; }
}

