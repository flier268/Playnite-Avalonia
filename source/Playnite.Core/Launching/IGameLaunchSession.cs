using System;
using System.Threading.Tasks;

namespace Playnite.Launching;

public interface IGameLaunchSession
{
    DateTime StartTime { get; }
    Task WaitForExitAsync();
    bool HasExited { get; }
}

