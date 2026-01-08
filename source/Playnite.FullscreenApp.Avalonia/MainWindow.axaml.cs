using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Playnite.FullscreenApp.Avalonia
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
