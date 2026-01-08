using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Playnite.Configuration;

namespace Playnite.Addons;

public sealed class AddonsManager
{
    public const string ExtensionManifestFileName = "extension.yaml";
    public const string ThemeManifestFileName = "theme.yaml";
    public const string PackedExtensionFileExtention = ".pext";
    public const string PackedThemeFileExtention = ".pthm";

    private readonly string userDataRootPath;
    private readonly string programRootPath;

    public AddonsManager(string userDataRootPath, string programRootPath)
    {
        this.userDataRootPath = userDataRootPath ?? string.Empty;
        this.programRootPath = programRootPath ?? string.Empty;
    }

    public static AddonsManager CreateDefault()
    {
        var userDataRoot = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");
        if (string.IsNullOrWhiteSpace(userDataRoot))
        {
            userDataRoot = UserDataPathResolver.GetDefaultUserDataPath();
        }

        var programRoot = AppContext.BaseDirectory ?? string.Empty;
        return new AddonsManager(userDataRoot, programRoot);
    }

    public string UserExtensionsPath => Path.Combine(userDataRootPath, "Extensions");
    public string UserThemesPath => Path.Combine(userDataRootPath, "Themes");
    public string ProgramExtensionsPath => Path.Combine(programRootPath, "Extensions");
    public string ProgramThemesPath => Path.Combine(programRootPath, "Themes");

    public IReadOnlyList<AddonManifest> GetInstalledExtensions()
    {
        return EnumerateAddonManifests(AddonKind.Extension);
    }

    public IReadOnlyList<AddonManifest> GetInstalledThemes()
    {
        return EnumerateAddonManifests(AddonKind.Theme);
    }

    public AddonManifest? TryReadPackageManifest(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            return null;
        }

        var kind = DetectPackageKind(packagePath);
        if (kind is null)
        {
            var ext = Path.GetExtension(packagePath);
            if (string.Equals(ext, PackedThemeFileExtention, StringComparison.OrdinalIgnoreCase))
            {
                kind = AddonKind.Theme;
            }
            else if (string.Equals(ext, PackedExtensionFileExtention, StringComparison.OrdinalIgnoreCase))
            {
                kind = AddonKind.Extension;
            }
            else
            {
                return null;
            }
        }

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var manifestEntry = archive.Entries.FirstOrDefault(e =>
                string.Equals(Path.GetFileName(e.FullName), kind == AddonKind.Theme ? ThemeManifestFileName : ExtensionManifestFileName, StringComparison.OrdinalIgnoreCase));
            if (manifestEntry == null)
            {
                return null;
            }

            using var reader = new StreamReader(manifestEntry.Open());
            var yaml = reader.ReadToEnd();
            var parsed = ParseManifest(yaml, kind.Value);
            if (parsed is null)
            {
                return null;
            }

            return new AddonManifest
            {
                Id = parsed.Id,
                Name = parsed.Name,
                Author = parsed.Author,
                Version = parsed.Version,
                Kind = kind.Value,
                Type = parsed.Type,
                Module = parsed.Module,
                Mode = parsed.Mode,
                ThemeApiVersion = parsed.ThemeApiVersion,
                InstallDirectory = string.Empty,
                IsUserInstall = true
            };
        }
        catch
        {
            return null;
        }
    }

    public AddonInstallResult InstallFromPackage(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            return AddonInstallResult.Failed("Package path is empty.");
        }

        if (!File.Exists(packagePath))
        {
            return AddonInstallResult.Failed("Package file not found.");
        }

        var ext = Path.GetExtension(packagePath);
        AddonKind kind;
        if (string.Equals(ext, PackedThemeFileExtention, StringComparison.OrdinalIgnoreCase))
        {
            kind = AddonKind.Theme;
        }
        else if (string.Equals(ext, PackedExtensionFileExtention, StringComparison.OrdinalIgnoreCase))
        {
            kind = AddonKind.Extension;
        }
        else
        {
            // Fallback: detect by manifest presence inside zip.
            kind = DetectPackageKind(packagePath) ?? AddonKind.Extension;
        }

        var targetRoot = kind == AddonKind.Theme ? UserThemesPath : UserExtensionsPath;
        Directory.CreateDirectory(targetRoot);

        var tempRoot = Path.Combine(Path.GetTempPath(), "playnite_addons_install", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            using (var archive = ZipFile.OpenRead(packagePath))
            {
                archive.ExtractToDirectory(tempRoot);
            }

            var manifestPath = FindManifestPath(tempRoot, kind);
            if (manifestPath is null)
            {
                return AddonInstallResult.Failed($"Missing {(kind == AddonKind.Theme ? ThemeManifestFileName : ExtensionManifestFileName)} in package.");
            }

            var manifestText = File.ReadAllText(manifestPath);
            var manifest = ParseManifest(manifestText, kind);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
            {
                return AddonInstallResult.Failed("Invalid manifest (missing Id).");
            }

            var extractedRoot = GetAddonRootDirectory(tempRoot, manifestPath);
            if (string.IsNullOrWhiteSpace(extractedRoot) || !Directory.Exists(extractedRoot))
            {
                return AddonInstallResult.Failed("Invalid package layout.");
            }

            var dest = Path.Combine(targetRoot, manifest.Id);
            ReplaceDirectory(dest, extractedRoot);

            var installed = new AddonManifest
            {
                Id = manifest.Id,
                Name = manifest.Name,
                Author = manifest.Author,
                Version = manifest.Version,
                Kind = kind,
                Type = manifest.Type,
                Module = manifest.Module,
                Mode = manifest.Mode,
                ThemeApiVersion = manifest.ThemeApiVersion,
                InstallDirectory = dest,
                IsUserInstall = true
            };

            return AddonInstallResult.Installed(installed);
        }
        catch (Exception e)
        {
            return AddonInstallResult.Failed(e.Message);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch
            {
            }
        }
    }

    public bool Uninstall(AddonManifest addon)
    {
        if (addon is null)
        {
            return false;
        }

        if (!addon.IsUserInstall || string.IsNullOrWhiteSpace(addon.InstallDirectory))
        {
            return false;
        }

        try
        {
            if (!Directory.Exists(addon.InstallDirectory))
            {
                return true;
            }

            Directory.Delete(addon.InstallDirectory, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private List<AddonManifest> EnumerateAddonManifests(AddonKind kind)
    {
        var result = new List<AddonManifest>();
        var manifestFile = kind == AddonKind.Theme ? ThemeManifestFileName : ExtensionManifestFileName;

        foreach (var item in EnumerateAddonRoots(kind))
        {
            try
            {
                var manifestPath = Path.Combine(item.Path, manifestFile);
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                var manifestText = File.ReadAllText(manifestPath);
                var parsed = ParseManifest(manifestText, kind);
                if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id))
                {
                    continue;
                }

                result.Add(new AddonManifest
                {
                    Id = parsed.Id,
                    Name = parsed.Name,
                    Author = parsed.Author,
                    Version = parsed.Version,
                    Kind = kind,
                    Type = parsed.Type,
                    Module = parsed.Module,
                    Mode = parsed.Mode,
                    ThemeApiVersion = parsed.ThemeApiVersion,
                    InstallDirectory = item.Path,
                    IsUserInstall = item.IsUser
                });
            }
            catch
            {
            }
        }

        return result
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<(string Path, bool IsUser)> EnumerateAddonRoots(AddonKind kind)
    {
        var userBase = kind == AddonKind.Theme ? UserThemesPath : UserExtensionsPath;
        var programBase = kind == AddonKind.Theme ? ProgramThemesPath : ProgramExtensionsPath;

        foreach (var (baseDir, isUser) in new[] { (programBase, false), (userBase, true) })
        {
            if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir))
            {
                continue;
            }

            IEnumerable<string> dirs;
            try
            {
                dirs = Directory.EnumerateDirectories(baseDir);
            }
            catch
            {
                continue;
            }

            foreach (var dir in dirs)
            {
                yield return (dir, isUser);
            }
        }
    }

    private sealed class ParsedManifest
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public Version Version { get; init; } = new Version(0, 0);
        public string Type { get; init; } = string.Empty;
        public string Module { get; init; } = string.Empty;
        public string Mode { get; init; } = string.Empty;
        public string ThemeApiVersion { get; init; } = string.Empty;
    }

    private static ParsedManifest? ParseManifest(string yaml, AddonKind kind)
    {
        var map = SimpleAddonYamlParser.ParseKeyValues(yaml);
        if (!map.TryGetValue("Id", out var id) || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        map.TryGetValue("Name", out var name);
        map.TryGetValue("Author", out var author);
        map.TryGetValue("Version", out var versionStr);
        map.TryGetValue("Type", out var type);
        map.TryGetValue("Module", out var module);
        map.TryGetValue("Mode", out var mode);
        map.TryGetValue("ThemeApiVersion", out var themeApi);

        return new ParsedManifest
        {
            Id = id ?? string.Empty,
            Name = name ?? string.Empty,
            Author = author ?? string.Empty,
            Version = SimpleAddonYamlParser.ParseVersion(versionStr),
            Type = kind == AddonKind.Extension ? (type ?? string.Empty) : "Theme",
            Module = module ?? string.Empty,
            Mode = mode ?? string.Empty,
            ThemeApiVersion = themeApi ?? string.Empty
        };
    }

    private static AddonKind? DetectPackageKind(string packagePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            foreach (var entry in archive.Entries)
            {
                var name = Path.GetFileName(entry.FullName);
                if (string.Equals(name, ThemeManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return AddonKind.Theme;
                }

                if (string.Equals(name, ExtensionManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return AddonKind.Extension;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? FindManifestPath(string extractedRoot, AddonKind kind)
    {
        var manifestFile = kind == AddonKind.Theme ? ThemeManifestFileName : ExtensionManifestFileName;

        try
        {
            var direct = Path.Combine(extractedRoot, manifestFile);
            if (File.Exists(direct))
            {
                return direct;
            }

            return Directory.EnumerateFiles(extractedRoot, manifestFile, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static string GetAddonRootDirectory(string extractedRoot, string manifestPath)
    {
        // If the manifest is inside a single top-level folder, install that folder; otherwise install the whole extracted root.
        try
        {
            var manifestDir = Path.GetDirectoryName(manifestPath) ?? extractedRoot;
            if (string.Equals(manifestDir.TrimEnd(Path.DirectorySeparatorChar), extractedRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                return extractedRoot;
            }

            return manifestDir;
        }
        catch
        {
            return extractedRoot;
        }
    }

    private static void ReplaceDirectory(string destination, string source)
    {
        var tempDest = destination + ".new_" + Guid.NewGuid().ToString("N");
        CopyDirectory(source, tempDest);

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, true);
        }

        Directory.Move(tempDest, destination);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? destination);
            File.Copy(file, destFile, true);
        }
    }
}
