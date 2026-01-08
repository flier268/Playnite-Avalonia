using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.DesktopApp.Avalonia.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
