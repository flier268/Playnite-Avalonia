using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public interface IDesktopNavigationService
{
    void ShowLibrary();
    void ShowGameDetails(Game game);
}
