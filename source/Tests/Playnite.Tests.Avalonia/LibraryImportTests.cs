using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Playnite.Library;
using Playnite.LibraryImport.Epic;
using Playnite.LibraryImport.Steam;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.Tests.Avalonia;

public class LibraryImportTests
{
    [Test]
    public void SteamScanner_ImportsFromAppManifest()
    {
        var originalSteam = Environment.GetEnvironmentVariable("STEAM_PATH");
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var steamRoot = Path.Combine(testRoot, "Steam");
        var steamApps = Path.Combine(steamRoot, "steamapps");
        Directory.CreateDirectory(steamApps);

        File.WriteAllText(Path.Combine(steamApps, "libraryfolders.vdf"), "\"libraryfolders\" { \"0\" { \"path\" \"" + steamRoot.Replace("\\", "\\\\") + "\" } }");

        File.WriteAllText(Path.Combine(steamApps, "appmanifest_570.acf"),
            "\"AppState\" { \"appid\" \"570\" \"name\" \"Dota 2\" \"installdir\" \"dota 2 beta\" }");

        Directory.CreateDirectory(Path.Combine(steamApps, "common", "dota 2 beta"));

        try
        {
            Environment.SetEnvironmentVariable("STEAM_PATH", steamRoot);
            var scanner = new SteamLibraryScanner();
            var games = scanner.ScanInstalledGames();

            Assert.That(games, Has.Count.EqualTo(1));
            Assert.That(games[0].PluginId, Is.EqualTo(BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary)));
            Assert.That(games[0].GameId, Is.EqualTo("570"));
            Assert.That(games[0].Name, Is.EqualTo("Dota 2"));
            Assert.That(games[0].InstallDirectory, Does.Contain("dota 2 beta"));
            Assert.That(games[0].GameActions, Is.Not.Null);
            Assert.That(games[0].GameActions[0].Path, Does.Contain("steam://run/570"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("STEAM_PATH", originalSteam);
            try { Directory.Delete(testRoot, true); } catch { }
        }
    }

    [Test]
    public void EpicScanner_ImportsFromItemManifest()
    {
        var original = Environment.GetEnvironmentVariable("EPIC_MANIFESTS_PATH");
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);

        File.WriteAllText(Path.Combine(testRoot, "Foo.item"),
            "{\"AppName\":\"FooApp\",\"DisplayName\":\"Foo Game\",\"InstallLocation\":\"" + testRoot.Replace("\\", "\\\\") + "\"}");

        try
        {
            Environment.SetEnvironmentVariable("EPIC_MANIFESTS_PATH", testRoot);
            var scanner = new EpicLibraryScanner();
            var games = scanner.ScanInstalledGames();

            Assert.That(games, Has.Count.EqualTo(1));
            Assert.That(games[0].PluginId, Is.EqualTo(BuiltinExtensions.GetIdFromExtension(BuiltinExtension.EpicLibrary)));
            Assert.That(games[0].GameId, Is.EqualTo("FooApp"));
            Assert.That(games[0].Name, Is.EqualTo("Foo Game"));
            Assert.That(games[0].GameActions, Is.Not.Null);
            Assert.That(games[0].GameActions[0].Path, Does.Contain("com.epicgames.launcher://apps/FooApp"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EPIC_MANIFESTS_PATH", original);
            try { Directory.Delete(testRoot, true); } catch { }
        }
    }

    [Test]
    public void LibraryStore_TryUpsertGames_InsertsNewGames()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(Path.Combine(testRoot, "games.db"), string.Empty);

        try
        {
            var store = new LiteDbLibraryStore(testRoot);
            var game = new Game("Test Steam Game")
            {
                PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary),
                GameId = "123",
                IsInstalled = true,
                GameActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>
                {
                    new GameAction { Name = "Play", Type = GameActionType.URL, Path = "steam://run/123", IsPlayAction = true }
                }
            };

            Assert.That(store.TryUpsertGames(new[] { game }), Is.True);

            var loaded = store.LoadGames();
            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].PluginId, Is.EqualTo(game.PluginId));
            Assert.That(loaded[0].GameId, Is.EqualTo("123"));
            Assert.That(loaded[0].Name, Is.EqualTo("Test Steam Game"));
        }
        finally
        {
            try { Directory.Delete(testRoot, true); } catch { }
        }
    }
}

