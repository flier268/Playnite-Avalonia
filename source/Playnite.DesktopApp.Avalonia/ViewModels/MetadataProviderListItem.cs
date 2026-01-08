namespace Playnite.DesktopApp.Avalonia.ViewModels;

public sealed class MetadataProviderListItem
{
    public MetadataProviderListItem(string id, string name, string note = "")
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        Note = note ?? string.Empty;
    }

    public string Id { get; }
    public string Name { get; }
    public string Note { get; }

    public override string ToString() => string.IsNullOrWhiteSpace(Note) ? Name : $"{Name} ({Note})";
}

