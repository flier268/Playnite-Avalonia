using System.Collections.Generic;
using Playnite.SDK.Models;

namespace Playnite.Library;

public interface ILibraryStore
{
    string RootPath { get; }
    IReadOnlyList<Game> LoadGames();
    IReadOnlyList<LibraryIdName> LoadPlatforms();
    IReadOnlyList<LibraryIdName> LoadGenres();
    IReadOnlyList<Emulator> LoadEmulators();
    IReadOnlyList<FilterPreset> LoadFilterPresets();
    bool TrySaveFilterPresets(IReadOnlyList<FilterPreset> presets);
    bool TryUpsertGames(IReadOnlyList<Game> games);

    bool TryUpdateGameInstallation(Game game);
    bool TryUpdateGameActions(Game game);
    bool TryUpdateGameUserFlags(Game game);
    bool TryUpdateGameInstallSize(Game game);
    bool TryUpdateGameMetadata(Game game);
}
