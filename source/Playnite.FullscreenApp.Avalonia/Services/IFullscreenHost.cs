using System.Threading.Tasks;

namespace Playnite.FullscreenApp.Avalonia.Services;

public interface IFullscreenHost
{
    void Minimize();
    void Hide();
    void RestoreFullscreen();
    Task ShutdownAsync();
}

