using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class AddonsView : UserControl
{
    public AddonsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
