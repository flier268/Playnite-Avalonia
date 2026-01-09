using System;
using System.IO;
using LiteDB;
using NUnit.Framework;
using Playnite.Configuration;
using Playnite.Library;
using Playnite.SDK.Models;

namespace Playnite.Tests.Avalonia;

public class LibraryStoreFactoryTests
{
    [Test]
    public void CreateFromEnvironment_FallsBackToUserDataLibraryFolder()
    {
        var originalDbPath = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        var originalUserDataPath = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");

        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var userDataPath = Path.Combine(testRoot, "UserData");
        var libraryPath = Path.Combine(userDataPath, "library");

        Directory.CreateDirectory(libraryPath);
        File.WriteAllText(Path.Combine(libraryPath, "games.db"), string.Empty);

        try
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", null);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", userDataPath);

            var store = LibraryStoreFactory.CreateFromEnvironment();

            Assert.That(store, Is.Not.InstanceOf<EmptyLibraryStore>());
            Assert.That(store.RootPath, Is.EqualTo(libraryPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", originalDbPath);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", originalUserDataPath);

            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void Create_FromSettings_FallsBackToDefaultWhenUnset()
    {
        var originalDbPath = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        var originalUserDataPath = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");

        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var userDataPath = Path.Combine(testRoot, "UserData");
        var libraryPath = Path.Combine(userDataPath, "library");

        Directory.CreateDirectory(libraryPath);
        File.WriteAllText(Path.Combine(libraryPath, "games.db"), string.Empty);

        try
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", null);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", userDataPath);

            var store = LibraryStoreFactory.Create(new AppSettings { LibraryDbPath = string.Empty });

            Assert.That(store, Is.Not.InstanceOf<EmptyLibraryStore>());
            Assert.That(store.RootPath, Is.EqualTo(libraryPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", originalDbPath);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", originalUserDataPath);

            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void LiteDbLibraryStore_LoadsFilterPresets()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(Path.Combine(testRoot, "games.db"), string.Empty);

        var presetDbPath = Path.Combine(testRoot, "filterpresets.db");
        var mapper = new BsonMapper();
        mapper.Entity<FilterPreset>().Id(a => a.Id, false);

        using (var db = new LiteDatabase($"Filename={presetDbPath};Mode=Exclusive;Journal=false", mapper))
        {
            var col = db.GetCollection<FilterPreset>();
            col.Insert(new FilterPreset
            {
                Name = "Installed",
                Settings = new FilterPresetSettings { IsInstalled = true }
            });
        }

        try
        {
            var store = new LiteDbLibraryStore(testRoot);
            var presets = store.LoadFilterPresets();

            Assert.That(presets, Has.Count.EqualTo(1));
            Assert.That(presets[0].Name, Is.EqualTo("Installed"));
            Assert.That(presets[0].Settings.IsInstalled, Is.True);
        }
        finally
        {
            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void LiteDbLibraryStore_SavesFilterPresets_WithSortingOrder()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(Path.Combine(testRoot, "games.db"), string.Empty);

        try
        {
            var store = new LiteDbLibraryStore(testRoot);
            var a = new FilterPreset { Id = Guid.NewGuid(), Name = "A", Settings = new FilterPresetSettings() };
            var b = new FilterPreset { Id = Guid.NewGuid(), Name = "B", Settings = new FilterPresetSettings() };

            Assert.That(store.TrySaveFilterPresets(new[] { b, a }), Is.True);

            var loaded = store.LoadFilterPresets();
            Assert.That(loaded, Has.Count.EqualTo(2));
            Assert.That(loaded[0].Id, Is.EqualTo(b.Id));
            Assert.That(loaded[1].Id, Is.EqualTo(a.Id));
        }
        finally
        {
            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void LiteDbLibraryStore_LoadsFilterPresets_UsingWpfSettingsCollectionName()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        File.WriteAllText(Path.Combine(testRoot, "games.db"), string.Empty);

        var presetDbPath = Path.Combine(testRoot, "filterpresets.db");
        var a = new FilterPreset { Id = Guid.NewGuid(), Name = "A", Settings = new FilterPresetSettings() };
        var b = new FilterPreset { Id = Guid.NewGuid(), Name = "B", Settings = new FilterPresetSettings() };

        try
        {
            var mapper = new BsonMapper();
            mapper.Entity<FilterPreset>().Id(p => p.Id, false);

            using (var db = new LiteDatabase($"Filename={presetDbPath};Mode=Exclusive;Journal=false", mapper))
            {
                db.GetCollection<FilterPreset>().Insert(a);
                db.GetCollection<FilterPreset>().Insert(b);

                var settingsCol = db.GetCollection<FilterPresetsSettingsShim>("FilterPresetsSettings");
                settingsCol.Upsert(new FilterPresetsSettingsShim
                {
                    Id = 0,
                    SortingOrder = new System.Collections.Generic.List<Guid> { b.Id, a.Id }
                });
            }

            var store = new LiteDbLibraryStore(testRoot);
            var loaded = store.LoadFilterPresets();

            Assert.That(loaded, Has.Count.EqualTo(2));
            Assert.That(loaded[0].Id, Is.EqualTo(b.Id));
            Assert.That(loaded[1].Id, Is.EqualTo(a.Id));
        }
        finally
        {
            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void CreateFromEnvironment_FallsBackToLegacyConfigDatabasePath()
    {
        var originalDbPath = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        var originalUserDataPath = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");

        var testRoot = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var userDataPath = Path.Combine(testRoot, "UserData");
        var customLibraryPath = Path.Combine(testRoot, "CustomLibrary");

        Directory.CreateDirectory(userDataPath);
        Directory.CreateDirectory(customLibraryPath);
        File.WriteAllText(Path.Combine(customLibraryPath, "games.db"), string.Empty);

        File.WriteAllText(
            Path.Combine(userDataPath, "config.json"),
            System.Text.Json.JsonSerializer.Serialize(new { DatabasePath = customLibraryPath }));

        try
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", null);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", userDataPath);

            var store = LibraryStoreFactory.CreateFromEnvironment();

            Assert.That(store, Is.Not.InstanceOf<EmptyLibraryStore>());
            Assert.That(store.RootPath, Is.EqualTo(customLibraryPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYNITE_DB_PATH", originalDbPath);
            Environment.SetEnvironmentVariable("PLAYNITE_USERDATA_PATH", originalUserDataPath);

            try
            {
                Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed class FilterPresetsSettingsShim
    {
        public int Id { get; set; }
        public System.Collections.Generic.List<Guid> SortingOrder { get; set; } = new();
    }
}
