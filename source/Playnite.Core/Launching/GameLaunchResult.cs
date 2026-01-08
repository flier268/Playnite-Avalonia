using System;

namespace Playnite.Launching;

public sealed class GameLaunchResult
{
    public bool Started { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public IGameLaunchSession Session { get; init; }

    public static GameLaunchResult Failed(string message)
    {
        return new GameLaunchResult
        {
            Started = false,
            ErrorMessage = message ?? string.Empty
        };
    }

    public static GameLaunchResult Success(IGameLaunchSession session)
    {
        return new GameLaunchResult
        {
            Started = true,
            Session = session
        };
    }

    public static GameLaunchResult Success()
    {
        return new GameLaunchResult
        {
            Started = true,
            Session = null
        };
    }
}
