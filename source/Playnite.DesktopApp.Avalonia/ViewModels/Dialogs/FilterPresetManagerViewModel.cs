using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Playnite.Library;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class FilterPresetManagerViewModel : INotifyPropertyChanged
{
    private readonly ILibraryStore store;
    private readonly Func<string, string, System.Threading.Tasks.Task<bool>> confirmAsync;
    private FilterPresetEditItem? selectedPreset;
    private string status = string.Empty;
    private bool isDirty;

    public FilterPresetManagerViewModel(ILibraryStore store, Func<string, string, System.Threading.Tasks.Task<bool>> confirmAsync)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.confirmAsync = confirmAsync ?? throw new ArgumentNullException(nameof(confirmAsync));

        Presets = new ObservableCollection<FilterPresetEditItem>(
            store.LoadFilterPresets()
                .Where(a => a != null)
                .Select(a => new FilterPresetEditItem(a)));

        foreach (var preset in Presets)
        {
            preset.PropertyChanged += Preset_PropertyChanged;
        }

        PlatformOptions = new ObservableCollection<LibraryIdNameOption>(BuildIdNameOptions("(All platforms)", store.LoadPlatforms()));
        GenreOptions = new ObservableCollection<LibraryIdNameOption>(BuildIdNameOptions("(All genres)", store.LoadGenres()));

        AddCommand = new RelayCommand(Add);
        RemoveCommand = new RelayCommand(() => _ = RemoveSelectedAsync(), () => SelectedPreset != null);
        MoveUpCommand = new RelayCommand(MoveUp, () => CanMoveUp);
        MoveDownCommand = new RelayCommand(MoveDown, () => CanMoveDown);

        SelectedPreset = Presets.FirstOrDefault();
    }

    public ObservableCollection<FilterPresetEditItem> Presets { get; }
    public ObservableCollection<LibraryIdNameOption> PlatformOptions { get; }
    public ObservableCollection<LibraryIdNameOption> GenreOptions { get; }

    public FilterPresetEditItem? SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (ReferenceEquals(selectedPreset, value))
            {
                return;
            }

            selectedPreset = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedPlatformOption));
            OnPropertyChanged(nameof(SelectedGenreOption));
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
            (RemoveCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
            (MoveUpCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
            (MoveDownCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        }
    }

    public LibraryIdNameOption? SelectedPlatformOption
    {
        get
        {
            var id = SelectedPreset?.PlatformId;
            return PlatformOptions.FirstOrDefault(a => a.Id == id) ?? PlatformOptions.FirstOrDefault();
        }
        set
        {
            if (SelectedPreset == null)
            {
                return;
            }

            SelectedPreset.PlatformId = value?.Id;
            OnPropertyChanged();
        }
    }

    public LibraryIdNameOption? SelectedGenreOption
    {
        get
        {
            var id = SelectedPreset?.GenreId;
            return GenreOptions.FirstOrDefault(a => a.Id == id) ?? GenreOptions.FirstOrDefault();
        }
        set
        {
            if (SelectedPreset == null)
            {
                return;
            }

            SelectedPreset.GenreId = value?.Id;
            OnPropertyChanged();
        }
    }

    public bool IsDirty
    {
        get => isDirty;
        private set
        {
            if (isDirty == value)
            {
                return;
            }

            isDirty = value;
            OnPropertyChanged();
        }
    }

    public bool CanMoveUp => SelectedPreset != null && Presets.IndexOf(SelectedPreset) > 0;
    public bool CanMoveDown => SelectedPreset != null && Presets.IndexOf(SelectedPreset) >= 0 && Presets.IndexOf(SelectedPreset) < Presets.Count - 1;

    public string Status
    {
        get => status;
        private set
        {
            if (status == value)
            {
                return;
            }

            status = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool TrySave()
    {
        Status = string.Empty;

        var presets = Presets
            .Where(a => a?.Preset != null)
            .Select(a =>
            {
                if (a.Preset.Id == Guid.Empty)
                {
                    a.Preset.Id = Guid.NewGuid();
                }

                a.Preset.Name = a.Name ?? string.Empty;
                a.Preset.ShowInFullscreeQuickSelection = a.ShowInQuickSelection;
                a.Preset.Settings ??= new FilterPresetSettings();
                return a.Preset;
            })
            .ToList();

        if (!store.TrySaveFilterPresets(presets))
        {
            Status = "Failed to save filter presets.";
            return false;
        }

        foreach (var preset in Presets)
        {
            preset.MarkSaved();
        }

        IsDirty = false;
        return true;
    }

    private void Add()
    {
        var preset = new FilterPreset
        {
            Id = Guid.NewGuid(),
            Name = "New preset",
            Settings = new FilterPresetSettings()
        };

        var item = new FilterPresetEditItem(preset);
        item.PropertyChanged += Preset_PropertyChanged;
        Presets.Add(item);
        SelectedPreset = item;
        IsDirty = true;
    }

    private async System.Threading.Tasks.Task RemoveSelectedAsync()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        var ok = await confirmAsync("Delete preset", $"Delete preset \"{SelectedPreset.Name}\"?");
        if (!ok)
        {
            return;
        }

        var index = Presets.IndexOf(SelectedPreset);
        if (index < 0)
        {
            return;
        }

        Presets.RemoveAt(index);
        SelectedPreset = Presets.ElementAtOrDefault(Math.Min(index, Presets.Count - 1));
        IsDirty = true;
    }

    private void MoveUp()
    {
        if (!CanMoveUp || SelectedPreset == null)
        {
            return;
        }

        var index = Presets.IndexOf(SelectedPreset);
        Presets.Move(index, index - 1);
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
        (MoveUpCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        IsDirty = true;
    }

    private void MoveDown()
    {
        if (!CanMoveDown || SelectedPreset == null)
        {
            return;
        }

        var index = Presets.IndexOf(SelectedPreset);
        Presets.Move(index, index + 1);
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
        (MoveUpCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        IsDirty = true;
    }

    private void Preset_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterPresetEditItem.IsDirty))
        {
            IsDirty = Presets.Any(a => a.IsDirty) || IsDirty;
        }
        else
        {
            IsDirty = true;
        }

        if (ReferenceEquals(sender, SelectedPreset))
        {
            if (e.PropertyName == nameof(FilterPresetEditItem.PlatformId))
            {
                OnPropertyChanged(nameof(SelectedPlatformOption));
            }
            else if (e.PropertyName == nameof(FilterPresetEditItem.GenreId))
            {
                OnPropertyChanged(nameof(SelectedGenreOption));
            }
        }
    }

    private static IEnumerable<LibraryIdNameOption> BuildIdNameOptions(string allLabel, IReadOnlyList<LibraryIdName> items)
    {
        yield return new LibraryIdNameOption(null, allLabel);
        foreach (var item in items.Where(a => a != null).OrderBy(a => a.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            yield return new LibraryIdNameOption(item.Id, item.Name ?? string.Empty);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class FilterPresetEditItem : INotifyPropertyChanged
{
    private string name;
    private bool showInQuickSelection;
    private string searchText;
    private bool installedOnly;
    private bool favoritesOnly;
    private bool includeHidden;
    private Guid? platformId;
    private Guid? genreId;
    private bool isDirty;

    public FilterPresetEditItem(FilterPreset preset)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        name = preset.Name ?? string.Empty;
        showInQuickSelection = preset.ShowInFullscreeQuickSelection;
        Preset.Settings ??= new FilterPresetSettings();
        searchText = Preset.Settings.Name ?? string.Empty;
        installedOnly = Preset.Settings.IsInstalled;
        favoritesOnly = Preset.Settings.Favorite;
        includeHidden = Preset.Settings.Hidden;
        platformId = Preset.Settings.Platform?.Ids?.FirstOrDefault();
        genreId = Preset.Settings.Genre?.Ids?.FirstOrDefault();
    }

    public FilterPreset Preset { get; }

    public Guid Id => Preset.Id;

    public string Name
    {
        get => name;
        set
        {
            if (name == value)
            {
                return;
            }

            name = value ?? string.Empty;
            Preset.Name = name;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public bool ShowInQuickSelection
    {
        get => showInQuickSelection;
        set
        {
            if (showInQuickSelection == value)
            {
                return;
            }

            showInQuickSelection = value;
            Preset.ShowInFullscreeQuickSelection = value;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public string SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
            {
                return;
            }

            searchText = value ?? string.Empty;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.Name = searchText;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public bool InstalledOnly
    {
        get => installedOnly;
        set
        {
            if (installedOnly == value)
            {
                return;
            }

            installedOnly = value;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.IsInstalled = value;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public bool FavoritesOnly
    {
        get => favoritesOnly;
        set
        {
            if (favoritesOnly == value)
            {
                return;
            }

            favoritesOnly = value;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.Favorite = value;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public bool IncludeHidden
    {
        get => includeHidden;
        set
        {
            if (includeHidden == value)
            {
                return;
            }

            includeHidden = value;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.Hidden = value;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public Guid? PlatformId
    {
        get => platformId;
        set
        {
            if (platformId == value)
            {
                return;
            }

            platformId = value;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.Platform = platformId.HasValue ? new IdItemFilterItemProperties(platformId.Value) : null;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public Guid? GenreId
    {
        get => genreId;
        set
        {
            if (genreId == value)
            {
                return;
            }

            genreId = value;
            Preset.Settings ??= new FilterPresetSettings();
            Preset.Settings.Genre = genreId.HasValue ? new IdItemFilterItemProperties(genreId.Value) : null;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    public bool IsDirty
    {
        get => isDirty;
        private set
        {
            if (isDirty == value)
            {
                return;
            }

            isDirty = value;
            OnPropertyChanged();
        }
    }

    public void MarkSaved()
    {
        IsDirty = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MarkDirty()
    {
        IsDirty = true;
    }

    public override string ToString() => Name;
}

public sealed class LibraryIdNameOption
{
    public LibraryIdNameOption(Guid? id, string name)
    {
        Id = id;
        Name = name ?? string.Empty;
    }

    public Guid? Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}
