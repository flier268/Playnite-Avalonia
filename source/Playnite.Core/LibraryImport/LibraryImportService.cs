using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.Library;
using Playnite.LibraryImport.Epic;
using Playnite.LibraryImport.Steam;
using Playnite.SDK.Models;

namespace Playnite.LibraryImport;

public sealed class LibraryImportService
{
    private readonly ILibraryStore store;
    private readonly SteamLibraryScanner steamScanner;
    private readonly EpicLibraryScanner epicScanner;

    public LibraryImportService(ILibraryStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        steamScanner = new SteamLibraryScanner();
        epicScanner = new EpicLibraryScanner();
    }

    public LibraryImportResult ImportSteam()
    {
        var scan = steamScanner.ScanInstalledGamesDetailed();
        if (!scan.Success)
        {
            return new LibraryImportResult(false, 0, scan.Message);
        }

        return Upsert(scan.Games, $"Steam: {scan.Message}");
    }

    public LibraryImportResult ImportEpic()
    {
        var scan = epicScanner.ScanInstalledGamesDetailed();
        if (!scan.Success)
        {
            return new LibraryImportResult(false, 0, scan.Message);
        }

        return Upsert(scan.Games, $"Epic: {scan.Message}");
    }

    private LibraryImportResult Upsert(IReadOnlyList<Game> games, string messagePrefix)
    {
        games ??= Array.Empty<Game>();
        if (games.Count == 0)
        {
            return new LibraryImportResult(true, 0, $"{messagePrefix}; no games found");
        }

        var ok = store.TryUpsertGames(games);
        return new LibraryImportResult(ok, games.Count, ok ? $"{messagePrefix}; imported {games.Count}" : $"{messagePrefix}; DB update failed");
    }
}

public readonly record struct LibraryImportResult(bool Success, int ImportedCount, string Message);
