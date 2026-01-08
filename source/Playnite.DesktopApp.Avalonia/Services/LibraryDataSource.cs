using System;
using System.Collections.ObjectModel;
using Playnite.Library;
using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Avalonia.Services;

public interface ILibraryDataSource
{
    ObservableCollection<Game> LoadGames();
    IReadOnlyList<LibraryIdName> LoadPlatforms();
    IReadOnlyList<LibraryIdName> LoadGenres();
}

public static class LibraryDataSourceFactory
{
    public static ILibraryDataSource Create()
    {
        var store = AppServices.LibraryStore ?? LibraryStoreFactory.CreateFromEnvironment();
        if (store is not EmptyLibraryStore)
        {
            return new CoreLibraryDataSource(store);
        }

        return new MockLibraryDataSource();
    }
}

public sealed class CoreLibraryDataSource : ILibraryDataSource
{
    private readonly ILibraryStore store;

    public CoreLibraryDataSource(ILibraryStore store)
    {
        this.store = store;
    }

    public IReadOnlyList<LibraryIdName> LoadPlatforms()
    {
        return store.LoadPlatforms();
    }

    public IReadOnlyList<LibraryIdName> LoadGenres()
    {
        return store.LoadGenres();
    }

    public ObservableCollection<Game> LoadGames()
    {
        return new ObservableCollection<Game>(store.LoadGames());
    }
}

public sealed class MockLibraryDataSource : ILibraryDataSource
{
    public ObservableCollection<Game> LoadGames()
    {
        return new ObservableCollection<Game>
        {
            new Game("Hades")
            {
                Description = "Escape the Underworld in this action roguelike.",
                IsInstalled = true,
                LastActivity = DateTime.Now.AddDays(-2)
            },
            new Game("Stardew Valley")
            {
                Description = "Build a farm and a life in a cozy pixel town.",
                IsInstalled = true,
                LastActivity = DateTime.Now.AddDays(-10)
            },
            new Game("Disco Elysium")
            {
                Description = "A narrative RPG of investigation and identity.",
                IsInstalled = false,
                LastActivity = DateTime.Now.AddMonths(-1)
            },
            new Game("Hollow Knight")
            {
                Description = "Explore Hallownest in a hand-drawn metroidvania.",
                IsInstalled = true,
                LastActivity = DateTime.Now.AddDays(-30)
            }
        };
    }

    public IReadOnlyList<LibraryIdName> LoadPlatforms()
    {
        return Array.Empty<LibraryIdName>();
    }

    public IReadOnlyList<LibraryIdName> LoadGenres()
    {
        return Array.Empty<LibraryIdName>();
    }
}
