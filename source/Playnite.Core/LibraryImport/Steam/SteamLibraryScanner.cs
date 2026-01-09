using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.LibraryImport.Steam;

public sealed class SteamLibraryScanner
{
    public IReadOnlyList<Game> ScanInstalledGames()
    {
        return ScanInstalledGamesDetailed().Games;
    }

    public SteamScanResult ScanInstalledGamesDetailed()
    {
        var steamPaths = ResolveSteamPaths();
        if (steamPaths.Count == 0)
        {
            return new SteamScanResult(false, Array.Empty<Game>(), "Steam not found (set STEAM_PATH)");
        }

        var games = new List<Game>();
        var totalLibraries = 0;
        var totalManifests = 0;
        var scannedSources = new List<string>();

        foreach (var steamPath in steamPaths)
        {
            scannedSources.Add(steamPath);
            var libraryPaths = ResolveLibraryPaths(steamPath);
            if (libraryPaths.Count == 0)
            {
                continue;
            }

            totalLibraries += libraryPaths.Count;

            foreach (var lib in libraryPaths)
            {
                var steamAppsDir = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(steamAppsDir))
                {
                    continue;
                }

                foreach (var manifestPath in Directory.EnumerateFiles(steamAppsDir, "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
                {
                    totalManifests++;
                    var game = TryReadAppManifest(manifestPath, lib);
                    if (game != null)
                    {
                        // Check for duplicates (same AppId)
                        if (!games.Any(g => g.GameId == game.GameId))
                        {
                            games.Add(game);
                        }
                    }
                }
            }
        }

        var sourcesMsg = scannedSources.Count > 1 ? $" from {scannedSources.Count} sources" : "";
        return new SteamScanResult(true, games, $"Steam: {games.Count} games{sourcesMsg}; libraries {totalLibraries}; manifests {totalManifests}");
    }

    private static List<string> ResolveSteamPaths()
    {
        var foundPaths = new List<string>();

        // Priority 1: User settings from AppSettings (supports multiple paths separated by semicolon)
        var settings = Configuration.AppSettingsStoreFactory.CreateFromEnvironment().Load();
        if (!string.IsNullOrWhiteSpace(settings.SteamPath))
        {
            var userPaths = settings.SteamPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var path in userPaths)
            {
                if (Directory.Exists(path) && !foundPaths.Contains(path))
                {
                    foundPaths.Add(path);
                }
            }
        }

        // Priority 2: Environment variable (supports multiple paths separated by semicolon)
        var env = Environment.GetEnvironmentVariable("STEAM_PATH");
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

        // Priority 3: Windows registry
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !foundPaths.Contains(path))
                {
                    foundPaths.Add(path);
                }

                var exe = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", null) as string;
                if (!string.IsNullOrWhiteSpace(exe))
                {
                    exe = exe.Trim('"');
                    var dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) && !foundPaths.Contains(dir))
                    {
                        foundPaths.Add(dir);
                    }
                }
            }
            catch
            {
            }
        }

        // Priority 4: Common installation paths
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var common = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
            }
            : new[]
            {
                // Linux: Flatpak installation
                Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
                // Linux: Snap installation
                Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam"),
                // Linux: Native installation (primary location)
                Path.Combine(home, ".local", "share", "Steam"),
                // Linux: Steam symlink directory
                Path.Combine(home, ".steam", "steam"),
                // Linux: Debian-specific installation
                Path.Combine(home, ".steam", "debian-installation")
            };

        foreach (var path in common.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (Directory.Exists(path) && !foundPaths.Contains(path))
            {
                foundPaths.Add(path);
            }
        }

        return foundPaths;
    }

    private static List<string> ResolveLibraryPaths(string steamRoot)
    {
        var libs = new List<string>();
        if (string.IsNullOrWhiteSpace(steamRoot))
        {
            return libs;
        }

        libs.Add(steamRoot);

        var vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
        {
            return libs;
        }

        try
        {
            var text = File.ReadAllText(vdfPath);
            var root = SteamVdfParser.Parse(text);
            var libObj = GetObject(root, "libraryfolders") ?? GetObject(root, "LibraryFolders");
            if (libObj == null)
            {
                return libs;
            }

            foreach (var kv in libObj)
            {
                if (kv.Value is string pathStr)
                {
                    if (Directory.Exists(pathStr))
                    {
                        libs.Add(pathStr);
                    }
                }
                else if (kv.Value is Dictionary<string, object> nested)
                {
                    var path = GetString(nested, "path");
                    if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    {
                        libs.Add(path);
                    }
                }
            }
        }
        catch
        {
        }

        return libs
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Game? TryReadAppManifest(string manifestPath, string libraryRoot)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var text = File.ReadAllText(manifestPath);
            var root = SteamVdfParser.Parse(text);
            var appState = GetObject(root, "AppState");
            if (appState == null)
            {
                return null;
            }

            var appId = GetString(appState, "appid");
            var name = GetString(appState, "name");
            var installDirName = GetString(appState, "installdir");
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var installDir = string.IsNullOrWhiteSpace(installDirName)
                ? string.Empty
                : Path.Combine(libraryRoot, "steamapps", "common", installDirName);

            var game = new Game(name)
            {
                PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary),
                GameId = appId.Trim(),
                InstallDirectory = Directory.Exists(installDir) ? installDir : string.Empty,
                IsInstalled = true,
                GameActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>
                {
                    new GameAction
                    {
                        Name = "Play",
                        Type = GameActionType.URL,
                        Path = $"steam://run/{appId.Trim()}",
                        IsPlayAction = true
                    }
                }
            };

            return game;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object>? GetObject(Dictionary<string, object> root, string key)
    {
        if (root.TryGetValue(key, out var value) && value is Dictionary<string, object> obj)
        {
            return obj;
        }

        return null;
    }

    private static string GetString(Dictionary<string, object> obj, string key)
    {
        if (obj.TryGetValue(key, out var value) && value is string s)
        {
            return s;
        }

        return string.Empty;
    }
}

public readonly record struct SteamScanResult(bool Success, IReadOnlyList<Game> Games, string Message);
