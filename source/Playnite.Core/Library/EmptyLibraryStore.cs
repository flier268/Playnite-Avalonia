using System;
using System.Collections.Generic;
using Playnite.SDK.Models;

namespace Playnite.Library;

public sealed class EmptyLibraryStore : ILibraryStore
{
    public string RootPath => string.Empty;
    public IReadOnlyList<Game> LoadGames() => Array.Empty<Game>();
    public IReadOnlyList<LibraryIdName> LoadPlatforms() => Array.Empty<LibraryIdName>();
    public IReadOnlyList<LibraryIdName> LoadGenres() => Array.Empty<LibraryIdName>();
    public IReadOnlyList<Emulator> LoadEmulators() => Array.Empty<Emulator>();
    public bool TryUpdateGameInstallation(Game game) => false;
    public bool TryUpdateGameActions(Game game) => false;
    public bool TryUpdateGameUserFlags(Game game) => false;
    public bool TryUpdateGameInstallSize(Game game) => false;
    public bool TryUpdateGameMetadata(Game game) => false;
}
