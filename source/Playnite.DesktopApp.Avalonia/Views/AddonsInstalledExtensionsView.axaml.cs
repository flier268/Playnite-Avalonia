using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class AddonsInstalledExtensionsView : UserControl
{
    public AddonsInstalledExtensionsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
