using Playnite.SDK.Models;

namespace Playnite.Library;

public interface IGameStatsStore
{
    bool TryUpdateGameStats(Game game);
}

