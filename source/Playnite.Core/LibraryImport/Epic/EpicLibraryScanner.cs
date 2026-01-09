using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.LibraryImport.Epic;

public sealed class EpicLibraryScanner
{
    public IReadOnlyList<Game> ScanInstalledGames()
    {
        return ScanInstalledGamesDetailed().Games;
    }

    public EpicScanResult ScanInstalledGamesDetailed()
    {
        var manifestDirs = ResolveManifestDirectories();
        if (manifestDirs.Count == 0)
        {
            return new EpicScanResult(false, Array.Empty<Game>(), "Epic manifests not found (set EPIC_MANIFESTS_PATH)");
        }

        var games = new List<Game>();
        var totalManifests = 0;

        foreach (var manifestDir in manifestDirs)
        {
            foreach (var path in Directory.EnumerateFiles(manifestDir, "*.item", SearchOption.TopDirectoryOnly))
            {
                totalManifests++;
                var game = TryReadManifest(path);
                if (game != null)
                {
                    // Check for duplicates (same AppName/CatalogItemId)
                    if (!games.Any(g => g.GameId == game.GameId))
                    {
                        games.Add(game);
                    }
                }
            }
        }

        var sourcesMsg = manifestDirs.Count > 1 ? $" from {manifestDirs.Count} sources" : "";
        return new EpicScanResult(true, games, $"Epic: {games.Count} games{sourcesMsg}; manifests {totalManifests}");
    }

    private static List<string> ResolveManifestDirectories()
    {
        var foundPaths = new List<string>();

        // Priority 1: User settings from AppSettings (supports multiple paths separated by semicolon)
        var settings = Playnite.Configuration.AppSettingsStoreFactory.CreateFromEnvironment().Load();
        if (!string.IsNullOrWhiteSpace(settings.EpicManifestsPath))
        {
            var userPaths = settings.EpicManifestsPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var path in userPaths)
            {
                if (Directory.Exists(path) && !foundPaths.Contains(path))
                {
                    foundPaths.Add(path);
                }
            }
        }

        // Priority 2: Environment variable (supports multiple paths separated by semicolon)
        var env = Environment.GetEnvironmentVariable("EPIC_MANIFESTS_PATH");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var envPaths = env.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var path in envPaths)
            {
                if (Directory.Exists(path) && !foundPaths.Contains(path))
                {
                    foundPaths.Add(path);
                }
            }
        }

        // If user provided custom paths, only use those (don't auto-detect)
        if (foundPaths.Count > 0)
        {
            return foundPaths;
        }

        // Priority 3: Auto-detect common locations
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (!string.IsNullOrWhiteSpace(programData))
                {
                    var manifestPath = Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests");
                    if (Directory.Exists(manifestPath) && !foundPaths.Contains(manifestPath))
                    {
                        foundPaths.Add(manifestPath);
                    }
                }
            }
            catch
            {
            }
        }
        else
        {
            // Linux: Check common Epic/Legendary launcher locations
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var linuxPaths = new[]
                {
                    // Heroic Games Launcher (most common on Linux)
                    Path.Combine(home, ".config", "heroic", "legendaryConfig", "metadata"),
                    // Legendary CLI
                    Path.Combine(home, ".config", "legendary", "metadata"),
                    // Wine/Proton installation
                    Path.Combine(home, ".wine", "drive_c", "ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests"),
                    // Lutris Wine prefix
                    Path.Combine(home, "Games", "epic-games-store", "drive_c", "ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests")
                };

                foreach (var path in linuxPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (Directory.Exists(path) && !foundPaths.Contains(path))
                    {
                        foundPaths.Add(path);
                    }
                }
            }
            catch
            {
            }
        }

        return foundPaths;
    }

    private static Game? TryReadManifest(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var appName = GetString(root, "AppName");
            var displayName = GetString(root, "DisplayName");
            var installLocation = GetString(root, "InstallLocation");

            if (string.IsNullOrWhiteSpace(appName) && string.IsNullOrWhiteSpace(displayName))
            {
                return null;
            }

            var name = !string.IsNullOrWhiteSpace(displayName) ? displayName : appName;
            var gameId = !string.IsNullOrWhiteSpace(appName) ? appName : name;

            var installDir = !string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation)
                ? installLocation
                : string.Empty;

            var actions = new System.Collections.ObjectModel.ObservableCollection<GameAction>();
            if (!string.IsNullOrWhiteSpace(appName))
            {
                actions.Add(new GameAction
                {
                    Name = "Play",
                    Type = GameActionType.URL,
                    Path = $"com.epicgames.launcher://apps/{appName}?action=launch&silent=true",
                    IsPlayAction = true
                });
            }

            return new Game(name)
            {
                PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.EpicLibrary),
                GameId = gameId,
                InstallDirectory = installDir,
                IsInstalled = true,
                GameActions = actions
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetString(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}

public readonly record struct EpicScanResult(bool Success, IReadOnlyList<Game> Games, string Message);
