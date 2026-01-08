using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class AddonsInstalledThemesView : UserControl
{
    public AddonsInstalledThemesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
