using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Playnite.DesktopApp.Avalonia.ViewModels;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private void OnGamesDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is LibraryViewModel viewModel)
        {
            viewModel.OpenDetailsCommand.Execute(viewModel.SelectedGame);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
