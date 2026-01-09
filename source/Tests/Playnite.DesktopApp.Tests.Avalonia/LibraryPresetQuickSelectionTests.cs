using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using NUnit.Framework;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.DesktopApp.Avalonia.ViewModels;
using Playnite.Configuration;
using Playnite.Library;
using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Tests.Avalonia;

public class LibraryPresetQuickSelectionTests
{
    private static void InitAppServices()
    {
        var settingsFile = Path.Combine(Path.GetTempPath(), "Playnite.Avalonia.Tests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = new AppSettingsStore(settingsFile);
        store.Save(new AppSettings());
        AppServices.Initialize(store);
    }

    private sealed class TestDataSource : ILibraryDataSource
    {
        private readonly IReadOnlyList<FilterPreset> presets;

        public TestDataSource(IReadOnlyList<FilterPreset> presets)
        {
            this.presets = presets ?? Array.Empty<FilterPreset>();
        }

        public ObservableCollection<Game> LoadGames() => new();
        public IReadOnlyList<LibraryIdName> LoadPlatforms() => Array.Empty<LibraryIdName>();
        public IReadOnlyList<LibraryIdName> LoadGenres() => Array.Empty<LibraryIdName>();
        public IReadOnlyList<FilterPreset> LoadFilterPresets() => presets;
    }

    [Test]
    public void Reload_BuildsQuickPresetList_FromDbFlag()
    {
        InitAppServices();

        var all = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "All",
            ShowInFullscreeQuickSelection = true,
            Settings = new FilterPresetSettings()
        };

        var installed = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "Installed",
            ShowInFullscreeQuickSelection = true,
            Settings = new FilterPresetSettings { IsInstalled = true }
        };

        var hidden = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "Hidden",
            ShowInFullscreeQuickSelection = false,
            Settings = new FilterPresetSettings { Hidden = true }
        };

        var vm = new LibraryViewModel(new TestDataSource(new[] { all, installed, hidden }), navigation: null);

        Assert.That(vm.QuickPresetOptions, Has.Count.EqualTo(3));
        Assert.That(vm.QuickPresetOptions[0].Preset, Is.Null);
        Assert.That(vm.QuickPresetOptions[1].Name, Is.EqualTo("All"));
        Assert.That(vm.QuickPresetOptions[2].Name, Is.EqualTo("Installed"));
    }

    [Test]
    public void SelectingQuickPreset_AppliesSupportedSettings()
    {
        InitAppServices();

        var installed = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "Installed",
            ShowInFullscreeQuickSelection = true,
            Settings = new FilterPresetSettings { IsInstalled = true, Favorite = true, Name = "foo" }
        };

        var vm = new LibraryViewModel(new TestDataSource(new[] { installed }), navigation: null);
        vm.SelectedPreset = vm.QuickPresetOptions[1];

        Assert.That(vm.ShowInstalledOnly, Is.True);
        Assert.That(vm.ShowFavoritesOnly, Is.True);
        Assert.That(vm.FilterText, Is.EqualTo("foo"));
    }

    [Test]
    public void EditingFilters_AfterPresetSelection_SetsCustomPreset()
    {
        InitAppServices();

        var installed = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "Installed",
            ShowInFullscreeQuickSelection = true,
            Settings = new FilterPresetSettings { IsInstalled = true }
        };

        var vm = new LibraryViewModel(new TestDataSource(new[] { installed }), navigation: null);
        vm.SelectedPreset = vm.QuickPresetOptions[1];

        vm.FilterText = "changed";

        Assert.That(vm.SelectedPreset, Is.Not.Null);
        Assert.That(vm.SelectedPreset.Preset, Is.Null);
    }

    [Test]
    public void SelectedPreset_PersistsToSettings()
    {
        InitAppServices();

        var installed = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "Installed",
            ShowInFullscreeQuickSelection = true,
            Settings = new FilterPresetSettings { IsInstalled = true }
        };

        var vm = new LibraryViewModel(new TestDataSource(new[] { installed }), navigation: null);
        vm.SelectedPreset = vm.QuickPresetOptions[1];

        var settings = AppServices.LoadSettings();
        Assert.That(settings.SelectedLibraryFilterPresetId, Is.EqualTo(installed.Id));
    }
}
