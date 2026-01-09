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

    private sealed class FilterPresetsStoreSettings
    {
        public int Id { get; set; } = 0;
        public List<Guid> SortingOrder { get; set; } = new List<Guid>();
    }

    private const string FilterPresetsSettingsCollectionName = "FilterPresetsSettings";

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

    public static bool TryInitialize(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        try
        {
            var gamesDbPath = Path.Combine(rootPath, "games.db");

            // Create an empty database by opening it once
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using (var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Exclusive;Journal=false", mapper))
            {
                // Ensure the games collection exists
                var games = db.GetCollection<Game>();

                // Force the database to be written to disk
                // by performing a dummy operation
                _ = games.Count();
            }

            // Verify the file was created
            return File.Exists(gamesDbPath);
        }
        catch
        {
            return false;
        }
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

    public bool TryUpsertGames(IReadOnlyList<Game> games)
    {
        games ??= Array.Empty<Game>();

        var gamesDbPath = Path.Combine(rootPath, "games.db");
        if (!File.Exists(gamesDbPath))
        {
            // Try to initialize the database if it doesn't exist
            if (!TryInitialize(rootPath))
            {
                return false;
            }
        }

        try
        {
            var mapper = new BsonMapper();
            MapLiteDbEntities(mapper);

            using var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Exclusive;Journal=false", mapper);
            var col = db.GetCollection<Game>();

            foreach (var incoming in games.Where(g => g != null))
            {
                if (incoming.PluginId == Guid.Empty || string.IsNullOrWhiteSpace(incoming.GameId))
                {
                    continue;
                }

                var existing = col.FindOne(Query.And(
                    Query.EQ(nameof(Game.PluginId), incoming.PluginId),
                    Query.EQ(nameof(Game.GameId), incoming.GameId)));

                if (existing == null)
                {
                    if (incoming.Id == Guid.Empty)
                    {
                        incoming.Id = Guid.NewGuid();
                    }

                    if (incoming.Added == null)
                    {
                        incoming.Added = DateTime.Now;
                    }

                    col.Upsert(incoming);
                    continue;
                }

                existing.Name = string.IsNullOrWhiteSpace(incoming.Name) ? existing.Name : incoming.Name;
                existing.InstallDirectory = string.IsNullOrWhiteSpace(incoming.InstallDirectory) ? existing.InstallDirectory : incoming.InstallDirectory;
                existing.IsInstalled = incoming.IsInstalled;
                existing.PluginId = incoming.PluginId;
                existing.GameId = incoming.GameId;

                if ((existing.GameActions == null || existing.GameActions.Count == 0) && incoming.GameActions != null)
                {
                    existing.GameActions = incoming.GameActions;
                }

                existing.Modified = DateTime.Now;
                col.Upsert(existing);
            }

            return true;
        }
        catch
        {
            return false;
        }
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

    public IReadOnlyList<FilterPreset> LoadFilterPresets()
    {
        var dbPath = Path.Combine(rootPath, "filterpresets.db");
        if (!File.Exists(dbPath))
        {
            return Array.Empty<FilterPreset>();
        }

        var mapper = new BsonMapper();
        mapper.Entity<FilterPreset>().Id(a => a.Id, false);
        mapper.Entity<FilterPresetsStoreSettings>().Id(a => a.Id, false);

        using var db = new LiteDatabase($"Filename={dbPath};Mode=ReadOnly", mapper);
        var presets = db.GetCollection<FilterPreset>().FindAll().ToList();
        if (presets.Count == 0)
        {
            return presets;
        }

        var settingsCol = db.GetCollection<FilterPresetsStoreSettings>(FilterPresetsSettingsCollectionName);
        var settings = settingsCol.FindAll().FirstOrDefault();
        if (settings?.SortingOrder == null || settings.SortingOrder.Count == 0)
        {
            return presets.OrderBy(a => a?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();
        }

        var presetById = presets
            .Where(a => a != null && a.Id != Guid.Empty)
            .GroupBy(a => a.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var ordered = new List<FilterPreset>();
        foreach (var id in settings.SortingOrder)
        {
            if (presetById.TryGetValue(id, out var preset))
            {
                ordered.Add(preset);
            }
        }

        var remaining = presets
            .Where(p => p != null && !ordered.Any(o => o.Id == p.Id))
            .OrderBy(p => p?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        ordered.AddRange(remaining);

        return ordered;
    }

    public bool TrySaveFilterPresets(IReadOnlyList<FilterPreset> presets)
    {
        presets ??= Array.Empty<FilterPreset>();

        var dbPath = Path.Combine(rootPath, "filterpresets.db");
        try
        {
            var mapper = new BsonMapper();
            mapper.Entity<FilterPreset>().Id(a => a.Id, false);
            mapper.Entity<FilterPresetsStoreSettings>().Id(a => a.Id, false);

            using var db = new LiteDatabase($"Filename={dbPath};Mode=Exclusive;Journal=false", mapper);
            var col = db.GetCollection<FilterPreset>();
            var settingsCol = db.GetCollection<FilterPresetsStoreSettings>(FilterPresetsSettingsCollectionName);

            var ids = new List<Guid>();
            foreach (var preset in presets.Where(p => p != null))
            {
                if (preset.Id == Guid.Empty)
                {
                    preset.Id = Guid.NewGuid();
                }

                ids.Add(preset.Id);
                col.Upsert(preset);
            }

            var existing = col.FindAll().Select(a => a.Id).ToList();
            foreach (var existingId in existing)
            {
                if (!ids.Contains(existingId))
                {
                    col.Delete(existingId);
                }
            }

            settingsCol.Upsert(new FilterPresetsStoreSettings
            {
                Id = 0,
                SortingOrder = ids
            });

            return true;
        }
        catch
        {
            return false;
        }
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
