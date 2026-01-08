using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class SettingsGeneralView : UserControl
{
    public SettingsGeneralView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
