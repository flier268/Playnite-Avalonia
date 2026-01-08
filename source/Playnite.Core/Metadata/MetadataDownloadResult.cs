namespace Playnite.Metadata;

public sealed class MetadataDownloadResult
{
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;

    public static MetadataDownloadResult Failed(string message) => new MetadataDownloadResult { Success = false, ErrorMessage = message ?? string.Empty };
    public static MetadataDownloadResult Ok() => new MetadataDownloadResult { Success = true };
}

