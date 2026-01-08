using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.Library;

public sealed class LiteDbLibraryStore : ILibraryStore, IGameStatsStore
{
    private readonly string rootPath;

    public LiteDbLibraryStore(string rootPath)
    {
        this.rootPath = rootPath;
    }

    public string RootPath => rootPath;

    public static bool CanOpen(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        return File.Exists(Path.Combine(rootPath, "games.db"));
    }

    public IReadOnlyList<Game> LoadGames()
    {
        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return Array.Empty<Game>();
        }

        var mapper = new BsonMapper();
        MapLiteDbEntities(mapper);

        using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=ReadOnly", mapper);
        return db.GetCollection<Game>().FindAll().ToList();
    }

    public IReadOnlyList<LibraryIdName> LoadPlatforms()
    {
        return LoadIdNameCollection<Platform>("platforms.db", p => p.Name);
    }

    public IReadOnlyList<LibraryIdName> LoadGenres()
    {
        return LoadIdNameCollection<Genre>("genres.db", g => g.Name);
    }

    public IReadOnlyList<Emulator> LoadEmulators()
    {
        var dbPath = Path.Combine(rootPath, "emulators.db");
        if (!File.Exists(dbPath))
        {
            return Array.Empty<Emulator>();
        }

        var mapper = new BsonMapper();
        mapper.Entity<Emulator>().Id(a => a.Id, false);

        using var db = new LiteDatabase($"Filename={dbPath};Mode=ReadOnly", mapper);
        return db.GetCollection<Emulator>().FindAll().ToList();
    }

    public bool TryUpdateGameStats(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.LastActivity = game.LastActivity;
            existing.Playtime = game.Playtime;
            existing.PlayCount = game.PlayCount;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    public bool TryUpdateGameInstallation(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.IsInstalled = game.IsInstalled;
            existing.InstallDirectory = game.InstallDirectory;
            existing.IsInstalling = game.IsInstalling;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    public bool TryUpdateGameActions(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.GameActions = game.GameActions;
            existing.IncludeLibraryPluginAction = game.IncludeLibraryPluginAction;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    public bool TryUpdateGameUserFlags(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.Favorite = game.Favorite;
            existing.Hidden = game.Hidden;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    public bool TryUpdateGameInstallSize(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.InstallSize = game.InstallSize;
            existing.LastSizeScanDate = game.LastSizeScanDate;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    public bool TryUpdateGameMetadata(Game game)
    {
        if (game == null)
        {
            return false;
        }

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            return false;
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared", mapper);
            var games = db.GetCollection<Game>();

            var existing = games.FindById(game.Id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = game.Name;
            existing.SortingName = game.SortingName;
            existing.Description = game.Description;
            existing.ReleaseDate = game.ReleaseDate;
            existing.Icon = game.Icon;
            existing.CoverImage = game.CoverImage;
            existing.BackgroundImage = game.BackgroundImage;
            existing.InstallSize = game.InstallSize;
            existing.LastSizeScanDate = game.LastSizeScanDate;

            return games.Update(existing);
        }
        catch
        {
            return false;
        }
    }

    private IReadOnlyList<LibraryIdName> LoadIdNameCollection<T>(string dbFile, Func<T, string> nameSelector) where T : DatabaseObject
    {
        var dbPath = Path.Combine(rootPath, dbFile);
        if (!File.Exists(dbPath))
        {
            return Array.Empty<LibraryIdName>();
        }

        var mapper = new BsonMapper();
        mapper.Entity<T>().Id(a => a.Id, false);
        using var db = new LiteDatabase($"Filename={dbPath};Mode=ReadOnly", mapper);
        return db.GetCollection<T>().FindAll()
            .Select(item => new LibraryIdName(item.Id, nameSelector(item)))
            .OrderBy(item => item.Name)
            .ToList();
    }

    internal static void MapLiteDbEntities(BsonMapper mapper)
    {
        mapper.RegisterType<ReleaseDate>
        (
            date => date.Serialize(),
            bson => ReleaseDate.Deserialize(bson.AsString)
        );

        mapper.Entity<Game>().
            Id(a => a.Id, false).
            Ignore(a => a.Genres).
            Ignore(a => a.Developers).
            Ignore(a => a.Publishers).
            Ignore(a => a.Tags).
            Ignore(a => a.Features).
            Ignore(a => a.Categories).
            Ignore(a => a.Platforms).
            Ignore(a => a.Series).
            Ignore(a => a.AgeRatings).
            Ignore(a => a.Regions).
            Ignore(a => a.Source).
            Ignore(a => a.ReleaseYear).
            Ignore(a => a.UserScoreRating).
            Ignore(a => a.CommunityScoreRating).
            Ignore(a => a.CriticScoreRating).
            Ignore(a => a.UserScoreGroup).
            Ignore(a => a.CommunityScoreGroup).
            Ignore(a => a.CriticScoreGroup).
            Ignore(a => a.LastActivitySegment).
            Ignore(a => a.AddedSegment).
            Ignore(a => a.ModifiedSegment).
            Ignore(a => a.PlaytimeCategory).
            Ignore(a => a.IsCustomGame).
            Ignore(a => a.InstallationStatus);
    }
}
