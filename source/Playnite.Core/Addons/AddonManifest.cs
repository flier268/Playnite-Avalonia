using System;

namespace Playnite.Addons;

public sealed class AddonManifest
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public Version Version { get; init; } = new Version(0, 0);

    public AddonKind Kind { get; init; }

    public string Type { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;

    public string Mode { get; init; } = string.Empty;
    public string ThemeApiVersion { get; init; } = string.Empty;

    public string InstallDirectory { get; init; } = string.Empty;
    public bool IsUserInstall { get; init; }
}

