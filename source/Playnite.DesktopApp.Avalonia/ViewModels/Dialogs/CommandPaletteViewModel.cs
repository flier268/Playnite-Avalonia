using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Playnite.Configuration;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.DesktopApp.Avalonia.ViewModels;
using Playnite.SDK;
using Playnite.SDK.OutOfProc;

namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class CommandPaletteViewModel : INotifyPropertyChanged
{
    private readonly DesktopShellViewModel shell;
    private string filterText = string.Empty;
    private string statusText = string.Empty;
    private CommandPaletteItem? selectedItem;

    public CommandPaletteViewModel(DesktopShellViewModel shell)
    {
        this.shell = shell ?? throw new ArgumentNullException(nameof(shell));

        Items = new ObservableCollection<CommandPaletteItem>();
        FilteredItems = new ObservableCollection<CommandPaletteItem>();

        RefreshCommand = new RelayCommand(() => TaskUtilities.FireAndForget(RefreshAsync()));
        RunSelectedCommand = new RelayCommand(RunSelected);
        TogglePinSelectedCommand = new RelayCommand(TogglePinSelected);

        TaskUtilities.FireAndForget(RefreshAsync());
    }

    public string Title => "Command Palette";

    public ObservableCollection<CommandPaletteItem> Items { get; }
    public ObservableCollection<CommandPaletteItem> FilteredItems { get; }

    public CommandPaletteItem? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (ReferenceEquals(selectedItem, value))
            {
                return;
            }

            selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanTogglePin));
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

            filterText = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public ICommand TogglePinSelectedCommand { get; }
    public bool CanRun => SelectedItem != null;
    public bool CanTogglePin => SelectedItem != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        Items.Clear();
        FilteredItems.Clear();
        SelectedItem = null;

        AddBuiltInCommands();
        await LoadOutOfProcCommandsAsync();

        ApplyPinnedAndRecentMarkers();
        ApplyFilter();
        StatusText = $"Commands: {FilteredItems.Count}";
    }

    private void AddBuiltInCommands()
    {
        Items.Add(new CommandPaletteItem(
            id: "builtin.app.exit",
            displayName: "App: Exit",
            description: "Exit Playnite.",
            execute: () => AppServices.MainWindow?.ExitApplication()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.app.openLibrary",
            displayName: "App: Open Library",
            description: "Navigate to Library view.",
            execute: () => shell.ShowLibrary()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.app.openAddons",
            displayName: "App: Open Add-ons",
            description: "Navigate to Add-ons view.",
            execute: () => shell.ShowAddons()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.addons.browse",
            displayName: "Add-ons: Browse",
            description: "Open Add-ons section: Browse.",
            execute: () => shell.ShowAddonsSection("Browse")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.addons.installedExtensions",
            displayName: "Add-ons: Installed extensions",
            description: "Open Add-ons section: Installed extensions.",
            execute: () => shell.ShowAddonsSection("Installed extensions")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.addons.installedThemes",
            displayName: "Add-ons: Installed themes",
            description: "Open Add-ons section: Installed themes.",
            execute: () => shell.ShowAddonsSection("Installed themes")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.app.openSettings",
            displayName: "App: Open Settings",
            description: "Navigate to Settings view.",
            execute: () => shell.ShowSettings()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.general",
            displayName: "Settings: General",
            description: "Open Settings section: General.",
            execute: () => shell.ShowSettingsSection("General")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.appearance",
            displayName: "Settings: Appearance",
            description: "Open Settings section: Appearance.",
            execute: () => shell.ShowSettingsSection("Appearance")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.libraries",
            displayName: "Settings: Libraries",
            description: "Open Settings section: Libraries.",
            execute: () => shell.ShowSettingsSection("Libraries")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.updates",
            displayName: "Settings: Updates",
            description: "Open Settings section: Updates.",
            execute: () => shell.ShowSettingsSection("Updates")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.advanced",
            displayName: "Settings: Advanced",
            description: "Open Settings section: Advanced.",
            execute: () => shell.ShowSettingsSection("Advanced")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.settings.about",
            displayName: "Settings: About",
            description: "Open Settings section: About.",
            execute: () => shell.ShowSettingsSection("About")));

        Items.Add(new CommandPaletteItem(
            id: "builtin.library.reload",
            displayName: "Library: Reload",
            description: "Reload library data source and refresh view.",
            execute: () => shell.ReloadLibrary()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.library.rescanAllInstallSizes",
            displayName: "Library: Rescan all install sizes",
            description: "Rescan install sizes for all games.",
            execute: () => shell.RescanAllInstallSizes()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.library.downloadMissingMetadataFiltered",
            displayName: "Library: Download missing metadata (filtered)",
            description: "Download missing metadata for games currently matching filters.",
            execute: () => shell.DownloadMissingMetadataForFiltered()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.play",
            displayName: "Game: Play (current)",
            description: "Play the currently selected game (Library or Details).",
            execute: () => shell.PlayCurrentGame()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.openDetails",
            displayName: "Game: Open details (current)",
            description: "Open details for the currently selected game.",
            execute: () => shell.OpenCurrentGameDetails()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.toggleFavorite",
            displayName: "Game: Toggle favorite (current)",
            description: "Toggle favorite flag for the currently selected game.",
            execute: () => shell.ToggleCurrentGameFavorite()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.toggleHidden",
            displayName: "Game: Toggle hidden (current)",
            description: "Toggle hidden flag for the currently selected game.",
            execute: () => shell.ToggleCurrentGameHidden()));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.downloadMetadata",
            displayName: "Game: Download metadata (current)",
            description: "Download metadata for the currently selected game (overwrite existing).",
            execute: () => shell.DownloadMetadataForCurrentGame(overwriteExisting: true)));

        Items.Add(new CommandPaletteItem(
            id: "builtin.game.downloadMissingMetadata",
            displayName: "Game: Download missing metadata (current)",
            description: "Download only missing metadata for the currently selected game.",
            execute: () => shell.DownloadMetadataForCurrentGame(overwriteExisting: false)));
    }

    private async System.Threading.Tasks.Task LoadOutOfProcCommandsAsync()
    {
        var host = AppServices.OutOfProcAddonsHost;
        if (host == null)
        {
            return;
        }

        var settings = AppServices.LoadSettings();
        var addons = host.GetEnabledOutOfProcExtensions(settings);
        if (addons.Count == 0)
        {
            return;
        }

        foreach (var addon in addons)
        {
            if (addon == null || string.IsNullOrWhiteSpace(addon.Id))
            {
                continue;
            }

            host.TryStart(addon, out _);
            if (!host.TryInvoke(addon.Id, OutOfProcProtocol.Methods.GenericGetCommands, w =>
                {
                    w.WriteStartObject();
                    w.WriteEndObject();
                }, out var doc, out _))
            {
                doc?.Dispose();
                continue;
            }

            try
            {
                if (doc == null || !doc.RootElement.TryGetProperty(OutOfProcProtocol.ResponseResultProperty, out var resultEl) ||
                    resultEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!resultEl.TryGetProperty("commands", out var commandsEl) || commandsEl.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in commandsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var id = item.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : string.Empty;
                    var name = item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String ? nameEl.GetString() : string.Empty;
                    var desc = item.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String ? descEl.GetString() : string.Empty;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    Items.Add(new CommandPaletteItem(
                        id: $"oop:{addon.Id}:{id}",
                        displayName: $"{addon.Name}: {name}",
                        description: desc ?? string.Empty,
                        execute: () =>
                        {
                            var ok = host.TryInvoke(addon.Id, OutOfProcProtocol.Methods.GenericRunCommand, w =>
                                {
                                    w.WriteStartObject();
                                    w.WriteString("id", id);
                                    w.WriteEndObject();
                                }, out var runDoc, out _);
                            runDoc?.Dispose();
                            _ = ok;
                        }));
                }
            }
            finally
            {
                doc?.Dispose();
            }
        }

        await System.Threading.Tasks.Task.CompletedTask;
    }

    private void ApplyPinnedAndRecentMarkers()
    {
        var settings = AppServices.LoadSettings() ?? new AppSettings();
        settings.CommandPalettePinned ??= new List<string>();
        settings.CommandPaletteRecent ??= new List<string>();

        var pinnedSet = settings.CommandPalettePinned.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Items)
        {
            item.IsPinned = pinnedSet.Contains(item.Id);
        }

        var recentOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < settings.CommandPaletteRecent.Count; i++)
        {
            var id = settings.CommandPaletteRecent[i];
            if (!recentOrder.ContainsKey(id))
            {
                recentOrder[id] = i;
            }
        }

        var ordered = Items
            .OrderByDescending(i => i.IsPinned)
            .ThenBy(i => i.IsPinned ? i.DisplayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => recentOrder.TryGetValue(i.Id, out var idx) ? idx : int.MaxValue)
            .ThenBy(i => i.IsPinned ? string.Empty : i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Items.Clear();
        foreach (var item in ordered)
        {
            Items.Add(item);
        }
    }

    private void ApplyFilter()
    {
        var f = FilterText?.Trim() ?? string.Empty;
        FilteredItems.Clear();

        IEnumerable<CommandPaletteItem> source = Items;
        if (!string.IsNullOrWhiteSpace(f))
        {
            source = source.Where(i =>
                (i.DisplayName?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Description?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var item in source)
        {
            FilteredItems.Add(item);
        }

        SelectedItem = FilteredItems.FirstOrDefault();
    }

    private void RunSelected()
    {
        var item = SelectedItem;
        if (item == null)
        {
            return;
        }

        try
        {
            item.Execute?.Invoke();
            RecordRecent(item.Id);
        }
        catch
        {
        }
    }

    private void TogglePinSelected()
    {
        var item = SelectedItem;
        if (item == null)
        {
            return;
        }

        var settings = AppServices.LoadSettings() ?? new AppSettings();
        settings.CommandPalettePinned ??= new List<string>();

        if (settings.CommandPalettePinned.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
        {
            settings.CommandPalettePinned = settings.CommandPalettePinned.Where(a => !string.Equals(a, item.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            settings.CommandPalettePinned.Add(item.Id);
        }

        AppServices.SaveSettings(settings);
        TaskUtilities.FireAndForget(RefreshAsync());
    }

    private void RecordRecent(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var settings = AppServices.LoadSettings() ?? new AppSettings();
        settings.CommandPaletteRecent ??= new List<string>();

        settings.CommandPaletteRecent = settings.CommandPaletteRecent
            .Where(a => !string.Equals(a, id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        settings.CommandPaletteRecent.Insert(0, id);
        if (settings.CommandPaletteRecent.Count > 20)
        {
            settings.CommandPaletteRecent = settings.CommandPaletteRecent.Take(20).ToList();
        }

        AppServices.SaveSettings(settings);
    }
}

public sealed class CommandPaletteItem : INotifyPropertyChanged
{
    private bool isPinned;

    public CommandPaletteItem(string id, string displayName, string description, Action execute)
    {
        Id = id ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Execute = execute;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public Action Execute { get; }

    public bool IsPinned
    {
        get => isPinned;
        set
        {
            if (isPinned == value)
            {
                return;
            }

            isPinned = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayLabel)));
        }
    }

    public string DisplayLabel => IsPinned ? $"★ {DisplayName}" : DisplayName;

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => DisplayName;
}
