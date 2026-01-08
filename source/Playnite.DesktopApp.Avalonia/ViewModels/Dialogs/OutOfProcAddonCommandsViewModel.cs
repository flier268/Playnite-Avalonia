using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Playnite.Addons.OutOfProc;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.SDK;
using Playnite.SDK.OutOfProc;

namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class OutOfProcAddonCommandsViewModel : INotifyPropertyChanged
{
    private readonly string addonId;
    private string status = string.Empty;
    private OutOfProcAddonCommandItem? selectedCommand;

    public OutOfProcAddonCommandsViewModel(string title, string addonId)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Out-of-proc commands" : title;
        this.addonId = addonId ?? string.Empty;
        Commands = new ObservableCollection<OutOfProcAddonCommandItem>();

        RefreshCommand = new RelayCommand(Refresh);
        RunSelectedCommand = new RelayCommand(RunSelected);

        Refresh();
    }

    public string Title { get; }
    public ObservableCollection<OutOfProcAddonCommandItem> Commands { get; }

    public OutOfProcAddonCommandItem? SelectedCommand
    {
        get => selectedCommand;
        set
        {
            if (ReferenceEquals(selectedCommand, value))
            {
                return;
            }

            selectedCommand = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
        }
    }

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

    public ICommand RefreshCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public bool CanRun => SelectedCommand != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Refresh()
    {
        Commands.Clear();

        if (string.IsNullOrWhiteSpace(addonId))
        {
            Status = "Missing add-on id.";
            return;
        }

        var host = AppServices.OutOfProcAddonsHost;
        if (host == null)
        {
            Status = "Out-of-proc host unavailable.";
            return;
        }

        if (!host.TryInvoke(addonId, OutOfProcProtocol.Methods.GenericGetCommands, w =>
            {
                w.WriteStartObject();
                w.WriteEndObject();
            }, out var doc, out var error))
        {
            Status = string.IsNullOrWhiteSpace(error) ? "Failed to query commands." : error;
            doc?.Dispose();
            return;
        }

        try
        {
            if (doc == null || !doc.RootElement.TryGetProperty(OutOfProcProtocol.ResponseResultProperty, out var resultEl))
            {
                Status = "Invalid response (missing result).";
                return;
            }

            if (!resultEl.TryGetProperty("commands", out var commandsEl) || commandsEl.ValueKind != JsonValueKind.Array)
            {
                Status = "No commands.";
                return;
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

                Commands.Add(new OutOfProcAddonCommandItem(id, name, desc));
            }

            SelectedCommand = Commands.Count > 0 ? Commands[0] : null;
            Status = $"Commands: {Commands.Count}";
        }
        finally
        {
            doc?.Dispose();
        }
    }

    private void RunSelected()
    {
        if (SelectedCommand == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(addonId))
        {
            return;
        }

        var host = AppServices.OutOfProcAddonsHost;
        if (host == null)
        {
            Status = "Out-of-proc host unavailable.";
            return;
        }

        if (!host.TryInvoke(addonId, OutOfProcProtocol.Methods.GenericRunCommand, w =>
            {
                w.WriteStartObject();
                w.WriteString("id", SelectedCommand.Id);
                w.WriteEndObject();
            }, out var doc, out var error))
        {
            Status = string.IsNullOrWhiteSpace(error) ? "Command failed." : error;
            doc?.Dispose();
            return;
        }

        doc?.Dispose();
        Status = $"Executed: {SelectedCommand.Name}";
    }
}

public sealed class OutOfProcAddonCommandItem
{
    public OutOfProcAddonCommandItem(string id, string name, string description)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public override string ToString() => Name;
}

