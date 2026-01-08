namespace Playnite.Addons;

public sealed class AddonInstallResult
{
    public bool Success { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public AddonManifest? Manifest { get; init; }

    public static AddonInstallResult Failed(string message) => new AddonInstallResult { Success = false, ErrorMessage = message ?? string.Empty };

    public static AddonInstallResult Installed(AddonManifest manifest) => new AddonInstallResult { Success = true, Manifest = manifest };
}

