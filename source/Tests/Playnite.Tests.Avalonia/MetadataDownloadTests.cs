using System;
using System.IO;
using System.Linq;
using LiteDB;
using NUnit.Framework;
using Playnite.Library;
using Playnite.Metadata;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Playnite.Tests.Avalonia;

[TestFixture]
public sealed class MetadataDownloadTests
{
    [Test]
    public void DownloadsMetadataFromProviderAndPersistsToDb()
    {
        var root = Path.Combine(Path.GetTempPath(), "playnite_tests", "metadata_" + Guid.NewGuid().ToString("N"));
        var libraryRoot = Path.Combine(root, "library");
        Directory.CreateDirectory(libraryRoot);

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Original Name"
        };

        var gamesDbPath = Path.Combine(libraryRoot, "games.db");
        using (var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared"))
        {
            var games = db.GetCollection<Game>();
            games.Insert(game);
        }

        var store = new LiteDbLibraryStore(libraryRoot);
        var service = new MetadataDownloadService(new[] { new TestMetadataProvider() }, store);

        var providers = service.GetProviders();
        Assert.That(providers.Count, Is.EqualTo(1));

        var result = service.Download(game, "test");
        Assert.That(result.Success, Is.True, result.ErrorMessage);

        var updated = store.LoadGames().First(g => g.Id == game.Id);
        Assert.That(updated.Name, Does.Contain("(Test)"));
        Assert.That(updated.Description, Is.EqualTo("Test description from metadata provider."));
        Assert.That(updated.CoverImage, Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(libraryRoot, "files", updated.CoverImage)), Is.True);
    }

    [Test]
    public void LocalFilesProviderFindsCoverFromInstallDir()
    {
        var root = Path.Combine(Path.GetTempPath(), "playnite_tests", "metadata_localfiles_" + Guid.NewGuid().ToString("N"));
        var libraryRoot = Path.Combine(root, "library");
        var installRoot = Path.Combine(root, "install");
        Directory.CreateDirectory(libraryRoot);
        Directory.CreateDirectory(installRoot);

        var coverBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABJzQnCgAAAABJRU5ErkJggg==");
        File.WriteAllBytes(Path.Combine(installRoot, "cover.png"), coverBytes);

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Game",
            InstallDirectory = installRoot
        };

        var gamesDbPath = Path.Combine(libraryRoot, "games.db");
        using (var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared"))
        {
            var games = db.GetCollection<Game>();
            games.Insert(game);
        }

        var store = new LiteDbLibraryStore(libraryRoot);
        var service = new MetadataDownloadService(new IMetadataProvider[] { new LocalFilesMetadataProvider() }, store);
        var result = service.Download(game, "builtin.localfiles");
        Assert.That(result.Success, Is.True, result.ErrorMessage);

        var updated = store.LoadGames().First(g => g.Id == game.Id);
        Assert.That(updated.CoverImage, Is.Not.Empty);
        Assert.That(File.Exists(Path.Combine(libraryRoot, "files", updated.CoverImage)), Is.True);
    }

    [Test]
    public void DownloadMissingDoesNotOverwriteExistingCover()
    {
        var root = Path.Combine(Path.GetTempPath(), "playnite_tests", "metadata_localfiles_nooverwrite_" + Guid.NewGuid().ToString("N"));
        var libraryRoot = Path.Combine(root, "library");
        var installRoot = Path.Combine(root, "install");
        Directory.CreateDirectory(libraryRoot);
        Directory.CreateDirectory(installRoot);

        var coverBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABJzQnCgAAAABJRU5ErkJggg==");
        File.WriteAllBytes(Path.Combine(installRoot, "cover.png"), coverBytes);

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Game",
            InstallDirectory = installRoot,
            CoverImage = "existing.png"
        };

        var gamesDbPath = Path.Combine(libraryRoot, "games.db");
        using (var db = new LiteDatabase($"Filename={gamesDbPath};Mode=Shared"))
        {
            var games = db.GetCollection<Game>();
            games.Insert(game);
        }

        var store = new LiteDbLibraryStore(libraryRoot);
        var service = new MetadataDownloadService(new IMetadataProvider[] { new LocalFilesMetadataProvider() }, store);
        var result = service.Download(game, "builtin.localfiles", overwriteExisting: false);
        Assert.That(result.Success, Is.True, result.ErrorMessage);

        var updated = store.LoadGames().First(g => g.Id == game.Id);
        Assert.That(updated.CoverImage, Is.EqualTo("existing.png"));
    }

    private sealed class TestMetadataProvider : IMetadataProvider
    {
        public string Id => "test";
        public string Name => "Test Metadata Provider";

        public OnDemandMetadataProvider CreateProvider(Game game)
        {
            return new TestOnDemandMetadataProvider(game);
        }
    }

    private sealed class TestOnDemandMetadataProvider : OnDemandMetadataProvider
    {
        private readonly Game game;

        public TestOnDemandMetadataProvider(Game game)
        {
            this.game = game ?? new Game();
        }

        public override System.Collections.Generic.List<MetadataField> AvailableFields =>
            new System.Collections.Generic.List<MetadataField> { MetadataField.Name, MetadataField.Description, MetadataField.CoverImage };

        public override string GetName(GetMetadataFieldArgs args)
        {
            return string.IsNullOrWhiteSpace(game.Name) ? "Test Game Name" : $"{game.Name} (Test)";
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return "Test description from metadata provider.";
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            var bytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABJzQnCgAAAABJRU5ErkJggg==");
            return new MetadataFile("cover.png", bytes);
        }
    }
}
