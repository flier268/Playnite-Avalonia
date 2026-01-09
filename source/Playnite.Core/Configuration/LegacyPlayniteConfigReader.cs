using System;
using System.IO;
using System.Text.Json;

namespace Playnite.Configuration;

public static class LegacyPlayniteConfigReader
{
    public static bool TryGetDatabasePath(string userDataRoot, out string databasePath)
    {
        databasePath = string.Empty;
        if (string.IsNullOrWhiteSpace(userDataRoot))
        {
            return false;
        }

        var configPath = Path.Combine(userDataRoot, "config.json");
        if (!File.Exists(configPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("DatabasePath", out var prop) || prop.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var raw = prop.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            databasePath = ExpandLegacyDatabasePath(raw, userDataRoot);
            return !string.IsNullOrWhiteSpace(databasePath);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetDatabasePathFromKnownLocations(out string databasePath)
    {
        databasePath = string.Empty;

        var envUserData = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");
        if (!string.IsNullOrWhiteSpace(envUserData) && TryGetDatabasePath(envUserData, out databasePath))
        {
            return true;
        }

        var defaultUserData = UserDataPathResolver.GetDefaultUserDataPath();
        if (!string.IsNullOrWhiteSpace(defaultUserData) && TryGetDatabasePath(defaultUserData, out databasePath))
        {
            return true;
        }

        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDir) && TryGetDatabasePath(baseDir, out databasePath))
        {
            return true;
        }

        return false;
    }

    private static string ExpandLegacyDatabasePath(string value, string playniteDir)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var expanded = value;
        try
        {
            expanded = Environment.ExpandEnvironmentVariables(expanded);
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(playniteDir))
        {
            expanded = expanded.Replace("{PlayniteDir}", playniteDir, StringComparison.OrdinalIgnoreCase);
        }

        return expanded;
    }
}

