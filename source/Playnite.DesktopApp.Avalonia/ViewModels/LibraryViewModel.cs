using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.DesktopApp.Avalonia.Services;
using global::Playnite.Library;
using global::Playnite.SDK;
using global::Playnite.SDK.Models;
using System;
using Playnite.Metadata;
using System.Collections.Generic;

namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class LibraryViewModel : INotifyPropertyChanged
{
    private ILibraryDataSource dataSource;
    private ObservableCollection<Game> allGames;
    private readonly IDesktopNavigationService? navigation;
    private Game? selectedGame;
    private LibraryListEntry? selectedEntry;
    private string filterText = string.Empty;
    private bool showInstalledOnly;
    private bool showFavoritesOnly;
    private bool showHidden;
    private bool requireCover;
    private LibraryFilterOption? selectedPlatform;
    private LibraryFilterOption? selectedGenre;
    private LibrarySortMode sortMode = LibrarySortMode.Name;
    private LibraryGroupMode groupMode = LibraryGroupMode.None;
    private LibraryViewMode viewMode = LibraryViewMode.List;
    private string lastAction = "None";
    private Dictionary<Guid, string> platformNameById = new();
    private Dictionary<Guid, string> genreNameById = new();
    private readonly ObservableCollection<MetadataProviderListItem> metadataProviders = new ObservableCollection<MetadataProviderListItem>();
    private MetadataProviderListItem? selectedMetadataProvider;

    public LibraryViewModel() : this(LibraryDataSourceFactory.Create(), null)
    {
    }

    public LibraryViewModel(IDesktopNavigationService navigation) : this(LibraryDataSourceFactory.Create(), navigation)
    {
    }

    public LibraryViewModel(ILibraryDataSource dataSource, IDesktopNavigationService? navigation)
    {
        this.dataSource = dataSource;
        this.navigation = navigation;
        allGames = new ObservableCollection<Game>();
        Games = new ObservableCollection<LibraryListEntry>();

        PlatformOptions = new ObservableCollection<LibraryFilterOption>(BuildOptions("All platforms", dataSource.LoadPlatforms()));
        GenreOptions = new ObservableCollection<LibraryFilterOption>(BuildOptions("All genres", dataSource.LoadGenres()));
        SelectedPlatform = PlatformOptions.FirstOrDefault();
        SelectedGenre = GenreOptions.FirstOrDefault();

        OpenDetailsCommand = new RelayCommand<Game>(OpenDetails);
        PlayCommand = new RelayCommand<Game>(PlayGame);
        InstallCommand = new RelayCommand<Game>(InstallGame);
        ToggleFavoriteCommand = new RelayCommand<Game>(ToggleFavorite);
        ToggleHiddenCommand = new RelayCommand<Game>(ToggleHidden);
        RescanInstallSizeCommand = new RelayCommand<Game>(game => TaskUtilities.FireAndForget(RescanInstallSizeAsync(game)));
        RescanAllInstallSizesCommand = new RelayCommand(() => TaskUtilities.FireAndForget(RescanAllInstallSizesAsync()));
        RefreshMetadataProvidersCommand = new RelayCommand(() => RefreshMetadataProviders());
        DownloadMetadataCommand = new RelayCommand<Game>(game => TaskUtilities.FireAndForget(DownloadMetadataAsync(game, overwriteExisting: true)));
        DownloadMissingMetadataCommand = new RelayCommand<Game>(game => TaskUtilities.FireAndForget(DownloadMetadataAsync(game, overwriteExisting: false)));
        DownloadMissingMetadataForFilteredCommand = new RelayCommand(() => TaskUtilities.FireAndForget(DownloadMetadataForFilteredAsync()));
        ReloadCommand = new RelayCommand(() => Reload());

        AppServices.LibraryStoreChanged += (_, _) =>
        {
            Reload();
        };

        AppServices.SettingsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanAutoScanInstallSizes));
        };

        Reload();
        RefreshMetadataProviders();
    }

    public string Header => "Library";

    public string Description =>
        "Library view placeholder. This will host filters, games list, and details panels once ported.";

    public ObservableCollection<LibraryListEntry> Games { get; }

    public ICommand OpenDetailsCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ToggleHiddenCommand { get; }
    public ICommand RescanInstallSizeCommand { get; }
    public ICommand RescanAllInstallSizesCommand { get; }
    public ICommand RefreshMetadataProvidersCommand { get; }
    public ICommand DownloadMetadataCommand { get; }
    public ICommand DownloadMissingMetadataCommand { get; }
    public ICommand DownloadMissingMetadataForFilteredCommand { get; }
    public ICommand ReloadCommand { get; }

    public ObservableCollection<LibraryFilterOption> PlatformOptions { get; }
    public ObservableCollection<LibraryFilterOption> GenreOptions { get; }

    public bool CanAutoScanInstallSizes => AppServices.LoadSettings().ScanLibInstallSizeOnLibUpdate;

    public ObservableCollection<MetadataProviderListItem> MetadataProviders => metadataProviders;

    public MetadataProviderListItem? SelectedMetadataProvider
    {
        get => selectedMetadataProvider;
        set
        {
            if (ReferenceEquals(selectedMetadataProvider, value))
            {
                return;
            }

            selectedMetadataProvider = value;
            OnPropertyChanged();
        }
    }

    public LibraryFilterOption? SelectedPlatform
    {
        get => selectedPlatform;
        set
        {
            if (ReferenceEquals(selectedPlatform, value))
            {
                return;
            }

            selectedPlatform = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public LibraryFilterOption? SelectedGenre
    {
        get => selectedGenre;
        set
        {
            if (ReferenceEquals(selectedGenre, value))
            {
                return;
            }

            selectedGenre = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string FilterText
    {
        get => filterText;
        set
        {
            if (filterText == value)
            {
                return;
            }

            filterText = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public bool ShowInstalledOnly
    {
        get => showInstalledOnly;
        set
        {
            if (showInstalledOnly == value)
            {
                return;
            }

            showInstalledOnly = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public bool ShowFavoritesOnly
    {
        get => showFavoritesOnly;
        set
        {
            if (showFavoritesOnly == value)
            {
                return;
            }

            showFavoritesOnly = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public bool ShowHidden
    {
        get => showHidden;
        set
        {
            if (showHidden == value)
            {
                return;
            }

            showHidden = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public bool RequireCover
    {
        get => requireCover;
        set
        {
            if (requireCover == value)
            {
                return;
            }

            requireCover = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public LibrarySortMode SortMode
    {
        get => sortMode;
        set
        {
            if (sortMode == value)
            {
                return;
            }

            sortMode = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public LibraryViewMode ViewMode
    {
        get => viewMode;
        set
        {
            if (viewMode == value)
            {
                return;
            }

            viewMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsGridView));
            OnPropertyChanged(nameof(IsListView));
        }
    }

    public bool IsGridView => ViewMode == LibraryViewMode.Grid;
    public bool IsListView => ViewMode == LibraryViewMode.List;

    public LibraryGroupMode GroupMode
    {
        get => groupMode;
        set
        {
            if (groupMode == value)
            {
                return;
            }

            groupMode = value;
            OnPropertyChanged();

            if (ViewMode == LibraryViewMode.Grid && groupMode != LibraryGroupMode.None)
            {
                groupMode = LibraryGroupMode.None;
                OnPropertyChanged();
            }

            ApplyFilter();
        }
    }

    public LibraryGroupMode[] GroupModes { get; } =
    {
        LibraryGroupMode.None,
        LibraryGroupMode.Platform,
        LibraryGroupMode.Genre
    };

    public LibrarySortMode[] SortModes { get; } =
    {
        LibrarySortMode.Name,
        LibrarySortMode.LastActivity,
        LibrarySortMode.Added,
        LibrarySortMode.Playtime,
        LibrarySortMode.PlayCount,
        LibrarySortMode.ReleaseDate
    };

    public LibraryViewMode[] ViewModes { get; } =
    {
        LibraryViewMode.List,
        LibraryViewMode.Grid
    };

    public string LastAction
    {
        get => lastAction;
        private set
        {
            if (lastAction == value)
            {
                return;
            }

            lastAction = value;
            OnPropertyChanged();
        }
    }

    public Game? SelectedGame
    {
        get => selectedGame;
        set
        {
            if (ReferenceEquals(selectedGame, value))
            {
                return;
            }

            selectedGame = value;
            OnPropertyChanged();
        }
    }

    public LibraryListEntry? SelectedEntry
    {
        get => selectedEntry;
        set
        {
            if (ReferenceEquals(selectedEntry, value))
            {
                return;
            }

            selectedEntry = value;
            OnPropertyChanged();

            if (selectedEntry is LibraryGameEntry gameEntry)
            {
                SelectedGame = gameEntry.Game;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Reload()
    {
        dataSource = LibraryDataSourceFactory.Create();
        platformNameById = dataSource.LoadPlatforms().ToDictionary(a => a.Id, a => a.Name);
        genreNameById = dataSource.LoadGenres().ToDictionary(a => a.Id, a => a.Name);

        var selectedPlatformId = SelectedPlatform?.Id;
        var selectedGenreId = SelectedGenre?.Id;

        PlatformOptions.Clear();
        foreach (var option in BuildOptions("All platforms", dataSource.LoadPlatforms()))
        {
            PlatformOptions.Add(option);
        }

        GenreOptions.Clear();
        foreach (var option in BuildOptions("All genres", dataSource.LoadGenres()))
        {
            GenreOptions.Add(option);
        }

        SelectedPlatform = PlatformOptions.FirstOrDefault(a => a.Id == selectedPlatformId) ?? PlatformOptions.FirstOrDefault();
        SelectedGenre = GenreOptions.FirstOrDefault(a => a.Id == selectedGenreId) ?? GenreOptions.FirstOrDefault();

        allGames = dataSource.LoadGames();

        Games.Clear();
        foreach (var game in allGames)
        {
            Games.Add(new LibraryGameEntry(game));
        }

        SelectedGame = allGames.FirstOrDefault();
        SelectedEntry = Games.OfType<LibraryGameEntry>().FirstOrDefault();
        ApplyFilter();

        if (CanAutoScanInstallSizes)
        {
            TaskUtilities.FireAndForget(RescanAllInstallSizesAsync(onlyMissingOrStale: true));
        }
    }

    private void RefreshMetadataProviders()
    {
        metadataProviders.Clear();
        foreach (var provider in MetadataProviderRegistry.Default.Providers)
        {
            metadataProviders.Add(new MetadataProviderListItem(provider.Id, provider.Name));
        }

        if (metadataProviders.Count == 0)
        {
            metadataProviders.Add(new MetadataProviderListItem(string.Empty, "(none)"));
        }

        SelectedMetadataProvider = metadataProviders.FirstOrDefault();
    }

    private async System.Threading.Tasks.Task DownloadMetadataAsync(Game game, bool overwriteExisting)
    {
        if (game == null)
        {
            LastAction = "Download metadata (no game)";
            return;
        }

        var store = AppServices.LibraryStore;
        if (store == null || store is EmptyLibraryStore)
        {
            LastAction = "Download metadata (no DB)";
            return;
        }

        var providerId = SelectedMetadataProvider?.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(providerId))
        {
            LastAction = "Download metadata (no provider)";
            return;
        }

        LastAction = overwriteExisting ? "Downloading metadata..." : "Downloading missing metadata...";

        var result = await System.Threading.Tasks.Task.Run(() =>
        {
            var service = new MetadataDownloadService(MetadataProviderRegistry.Default.Providers, store);
            return service.Download(game, providerId, overwriteExisting);
        });

        LastAction = result.Success
            ? (overwriteExisting ? "Metadata downloaded" : "Missing metadata downloaded")
            : $"Metadata download failed: {result.ErrorMessage}";

        ApplyFilter();
    }

    private async System.Threading.Tasks.Task DownloadMetadataForFilteredAsync()
    {
        var store = AppServices.LibraryStore;
        if (store == null || store is EmptyLibraryStore)
        {
            LastAction = "Download metadata (no DB)";
            return;
        }

        var providerId = SelectedMetadataProvider?.Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(providerId))
        {
            LastAction = "Download metadata (no provider)";
            return;
        }

        var filteredGames = Games.OfType<LibraryGameEntry>().Select(e => e.Game).ToList();
        if (filteredGames.Count == 0)
        {
            LastAction = "Download metadata (no games)";
            return;
        }

        LastAction = $"Downloading missing metadata: {filteredGames.Count} games...";

        var ok = await System.Threading.Tasks.Task.Run(() =>
        {
            var service = new MetadataDownloadService(MetadataProviderRegistry.Default.Providers, store);
            foreach (var game in filteredGames)
            {
                service.Download(game, providerId, overwriteExisting: false);
            }

            return true;
        });

        LastAction = ok ? "Missing metadata downloaded" : "Metadata download failed";
        ApplyFilter();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ApplyFilter()
    {
        var query = allGames.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            var term = filterText.Trim();
            query = query.Where(game => !string.IsNullOrEmpty(game.Name) &&
                                        game.Name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (showInstalledOnly)
        {
            query = query.Where(game => game.IsInstalled);
        }

        if (showFavoritesOnly)
        {
            query = query.Where(game => game.Favorite);
        }

        if (!showHidden)
        {
            query = query.Where(game => !game.Hidden);
        }

        if (requireCover)
        {
            query = query.Where(game => !string.IsNullOrWhiteSpace(game.CoverImage));
        }

        var platformId = SelectedPlatform?.Id;
        if (platformId.HasValue)
        {
            query = query.Where(game => game.PlatformIds?.Contains(platformId.Value) == true);
        }

        var genreId = SelectedGenre?.Id;
        if (genreId.HasValue)
        {
            query = query.Where(game => game.GenreIds?.Contains(genreId.Value) == true);
        }

        var groupingEnabled = ViewMode == LibraryViewMode.List && GroupMode != LibraryGroupMode.None;
        if (!groupingEnabled)
        {
            query = ApplySort(query);
        }

        Games.Clear();
        if (groupingEnabled)
        {
            var grouped = query
                .Select(game => new { Game = game, Key = GetGroupKey(game) })
                .GroupBy(a => a.Key, a => a.Game)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var games = ApplySort(group).ToList();
                Games.Add(new LibraryGroupHeaderEntry(group.Key, games.Count));
                foreach (var game in games)
                {
                    Games.Add(new LibraryGameEntry(game));
                }
            }
        }
        else
        {
            foreach (var game in query)
            {
                Games.Add(new LibraryGameEntry(game));
            }
        }

        var previousSelectedId = SelectedGame?.Id;
        if (previousSelectedId.HasValue)
        {
            var existing = Games.OfType<LibraryGameEntry>().FirstOrDefault(a => a.Game.Id == previousSelectedId.Value);
            if (existing != null)
            {
                SelectedEntry = existing;
                return;
            }
        }

        SelectedEntry = Games.OfType<LibraryGameEntry>().FirstOrDefault();
    }

    private IEnumerable<Game> ApplySort(IEnumerable<Game> source)
    {
        return sortMode switch
        {
            LibrarySortMode.LastActivity => source.OrderByDescending(game => game.LastActivity ?? System.DateTime.MinValue),
            LibrarySortMode.Added => source.OrderByDescending(game => game.Added ?? System.DateTime.MinValue),
            LibrarySortMode.Playtime => source.OrderByDescending(game => game.Playtime),
            LibrarySortMode.PlayCount => source.OrderByDescending(game => game.PlayCount),
            LibrarySortMode.ReleaseDate => source.OrderByDescending(game => game.ReleaseDate?.Date ?? System.DateTime.MinValue),
            _ => source.OrderBy(game => game.Name)
        };
    }

    private string GetGroupKey(Game game)
    {
        return GroupMode switch
        {
            LibraryGroupMode.Platform => GetFirstMappedName(game.PlatformIds, platformNameById, "Unknown platform"),
            LibraryGroupMode.Genre => GetFirstMappedName(game.GenreIds, genreNameById, "Unknown genre"),
            _ => "All"
        };
    }

    private static string GetFirstMappedName(IReadOnlyList<Guid>? ids, Dictionary<Guid, string> map, string unknownLabel)
    {
        if (ids == null || ids.Count == 0)
        {
            return unknownLabel;
        }

        var names = ids
            .Where(map.ContainsKey)
            .Select(id => map[id])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names.Count > 0 ? names[0] : unknownLabel;
    }

    private void OpenDetails(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        LastAction = SelectedGame == null ? "Open details (no selection)" : $"Open details: {SelectedGame.Name}";
        if (SelectedGame != null)
        {
            navigation?.ShowGameDetails(SelectedGame);
        }
    }

    private void PlayGame(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        if (SelectedGame == null)
        {
            LastAction = "Play (no selection)";
            return;
        }

        LastAction = $"Play: {SelectedGame.Name}";
        TaskUtilities.FireAndForget(AppServices.GameLaunchService.LaunchAsync(SelectedGame));
    }

    private void InstallGame(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        if (SelectedGame == null)
        {
            LastAction = "Install/Uninstall (no selection)";
            return;
        }

        SelectedGame.IsInstalled = !SelectedGame.IsInstalled;
        AppServices.LibraryStore?.TryUpdateGameInstallation(SelectedGame);
        LastAction = $"Install/Uninstall: {SelectedGame.Name}";
        ApplyFilter();
    }

    private void ToggleFavorite(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        if (SelectedGame == null)
        {
            LastAction = "Toggle favorite (no selection)";
            return;
        }

        SelectedGame.Favorite = !SelectedGame.Favorite;
        AppServices.LibraryStore?.TryUpdateGameUserFlags(SelectedGame);
        LastAction = $"Toggle favorite: {SelectedGame.Name}";
        ApplyFilter();
    }

    private void ToggleHidden(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        if (SelectedGame == null)
        {
            LastAction = "Toggle hidden (no selection)";
            return;
        }

        SelectedGame.Hidden = !SelectedGame.Hidden;
        AppServices.LibraryStore?.TryUpdateGameUserFlags(SelectedGame);
        LastAction = $"Toggle hidden: {SelectedGame.Name}";
        ApplyFilter();
    }

    private async System.Threading.Tasks.Task RescanInstallSizeAsync(Game? game)
    {
        if (game != null)
        {
            SelectedGame = game;
        }

        if (SelectedGame == null)
        {
            LastAction = "Rescan install size (no selection)";
            return;
        }

        var installDir = SelectedGame.InstallDirectory ?? string.Empty;
        if (string.IsNullOrWhiteSpace(installDir))
        {
            LastAction = "Rescan install size (missing install directory)";
            return;
        }

        LastAction = $"Scanning install size: {SelectedGame.Name}";

        var scan = await System.Threading.Tasks.Task.Run(() =>
        {
            return InstallSizeScanner.TryGetDirectorySizeBytes(installDir, out var bytes) ? (true, bytes) : (false, 0UL);
        });

        if (!scan.Item1)
        {
            LastAction = "Failed to scan install size";
            return;
        }

        SelectedGame.InstallSize = scan.Item2;
        SelectedGame.LastSizeScanDate = DateTime.UtcNow;
        AppServices.LibraryStore?.TryUpdateGameInstallSize(SelectedGame);

        LastAction = $"Install size updated: {SelectedGame.Name}";
        ApplyFilter();
    }

    private async System.Threading.Tasks.Task RescanAllInstallSizesAsync(bool onlyMissingOrStale = false)
    {
        var games = allGames?.Where(g => g != null && g.IsInstalled).ToList() ?? new System.Collections.Generic.List<Game>();
        if (games.Count == 0)
        {
            LastAction = "Rescan install sizes (no installed games)";
            return;
        }

        var now = DateTime.UtcNow;
        if (onlyMissingOrStale)
        {
            games = games
                .Where(g =>
                    g.InstallSize == null ||
                    g.LastSizeScanDate == null ||
                    (now - g.LastSizeScanDate.Value).TotalDays >= 7)
                .ToList();
        }

        if (games.Count == 0)
        {
            LastAction = "Install size scan skipped (up to date)";
            return;
        }

        var updated = 0;
        foreach (var g in games)
        {
            var installDir = g.InstallDirectory ?? string.Empty;
            if (string.IsNullOrWhiteSpace(installDir))
            {
                continue;
            }

            LastAction = $"Scanning install size ({updated}/{games.Count}): {g.Name}";

            var scan = await System.Threading.Tasks.Task.Run(() =>
            {
                return InstallSizeScanner.TryGetDirectorySizeBytes(installDir, out var bytes) ? (true, bytes) : (false, 0UL);
            });

            if (!scan.Item1)
            {
                continue;
            }

            g.InstallSize = scan.Item2;
            g.LastSizeScanDate = now;
            if (AppServices.LibraryStore?.TryUpdateGameInstallSize(g) == true)
            {
                updated++;
            }
        }

        LastAction = $"Install size scan complete: {updated} updated";
        ApplyFilter();
    }

    private static IEnumerable<LibraryFilterOption> BuildOptions(string allName, IReadOnlyList<LibraryIdName> items)
    {
        yield return new LibraryFilterOption(null, allName);
        foreach (var item in items.OrderBy(a => a.Name))
        {
            yield return new LibraryFilterOption(item.Id, item.Name);
        }
    }
}

public enum LibrarySortMode
{
    Name,
    LastActivity,
    Added,
    Playtime,
    PlayCount,
    ReleaseDate
}

public enum LibraryGroupMode
{
    None,
    Platform,
    Genre
}

public enum LibraryViewMode
{
    List,
    Grid
}

public abstract class LibraryListEntry
{
}

public sealed class LibraryGroupHeaderEntry : LibraryListEntry
{
    public LibraryGroupHeaderEntry(string name, int count)
    {
        Name = name ?? string.Empty;
        Count = count < 0 ? 0 : count;
    }

    public string Name { get; }
    public int Count { get; }
    public override string ToString() => $"{Name} ({Count})";
}

public sealed class LibraryGameEntry : LibraryListEntry
{
    public LibraryGameEntry(Game game)
    {
        Game = game;
    }

    public Game Game { get; }
    public override string ToString() => Game?.Name ?? string.Empty;
}

public sealed class LibraryFilterOption
{
    public LibraryFilterOption(Guid? id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid? Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}
