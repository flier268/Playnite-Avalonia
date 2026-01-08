using System.Threading.Tasks;
using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Avalonia.Services;

public interface IGameLaunchService
{
    Task<bool> LaunchAsync(Game game);
}

