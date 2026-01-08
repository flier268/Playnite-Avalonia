using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Models;

namespace Playnite.Emulation;

internal static class SimpleEmulatorYamlParser
{
    public static EmulatorDefinition? TryParseDefinition(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return null;
        }

        var lines = SplitLines(yaml);
        var definition = new EmulatorDefinition
        {
            Profiles = new List<EmulatorDefinitionProfile>()
        };

        EmulatorDefinitionProfile? currentProfile = null;
        var inProfiles = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            line = line.TrimEnd();
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (!inProfiles)
            {
                if (TryReadKeyValue(trimmed, out var key, out var value))
                {
                    if (string.Equals(key, "Profiles", StringComparison.OrdinalIgnoreCase))
                    {
                        inProfiles = true;
                        continue;
                    }

                    ApplyDefinitionValue(definition, key, value);
                }

                continue;
            }

            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                if (TryReadKeyValue(trimmed.TrimStart('-').TrimStart(), out var key, out var value) &&
                    string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    currentProfile = new EmulatorDefinitionProfile
                    {
                        Name = Unquote(value),
                        Platforms = new List<string>(),
                        ImageExtensions = new List<string>(),
                        ProfileFiles = new List<string>()
                    };

                    definition.Profiles.Add(currentProfile);
                }

                continue;
            }

            if (currentProfile == null)
            {
                continue;
            }

            if (!TryReadKeyValue(trimmed, out var profileKey, out var profileValue))
            {
                continue;
            }

            ApplyProfileValue(currentProfile, profileKey, profileValue);
        }

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            return null;
        }

        return definition;
    }

    private static void ApplyDefinitionValue(EmulatorDefinition definition, string key, string value)
    {
        var unquoted = Unquote(value);
        if (string.Equals(key, "Id", StringComparison.OrdinalIgnoreCase))
        {
            definition.Id = unquoted;
        }
        else if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
        {
            definition.Name = unquoted;
        }
        else if (string.Equals(key, "Website", StringComparison.OrdinalIgnoreCase))
        {
            definition.Website = unquoted;
        }
    }

    private static void ApplyProfileValue(EmulatorDefinitionProfile profile, string key, string value)
    {
        if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
        {
            profile.Name = Unquote(value);
        }
        else if (string.Equals(key, "Platforms", StringComparison.OrdinalIgnoreCase))
        {
            profile.Platforms = ParseInlineList(value);
        }
        else if (string.Equals(key, "ImageExtensions", StringComparison.OrdinalIgnoreCase))
        {
            profile.ImageExtensions = ParseInlineList(value);
        }
        else if (string.Equals(key, "ProfileFiles", StringComparison.OrdinalIgnoreCase))
        {
            profile.ProfileFiles = ParseInlineList(value);
        }
        else if (string.Equals(key, "InstallationFile", StringComparison.OrdinalIgnoreCase))
        {
            profile.InstallationFile = Unquote(value);
        }
        else if (string.Equals(key, "StartupArguments", StringComparison.OrdinalIgnoreCase))
        {
            profile.StartupArguments = Unquote(value);
        }
        else if (string.Equals(key, "StartupExecutable", StringComparison.OrdinalIgnoreCase))
        {
            profile.StartupExecutable = Unquote(value);
        }
        else if (string.Equals(key, "ScriptStartup", StringComparison.OrdinalIgnoreCase))
        {
            profile.ScriptStartup = ParseBool(value);
        }
        else if (string.Equals(key, "ScriptGameImport", StringComparison.OrdinalIgnoreCase))
        {
            profile.ScriptGameImport = ParseBool(value);
        }
    }

    private static bool ParseBool(string value)
    {
        var v = Unquote(value).Trim();
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ParseInlineList(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal) || !trimmed.EndsWith("]", StringComparison.Ordinal))
        {
            return new List<string>();
        }

        var inner = trimmed.Substring(1, trimmed.Length - 2);
        return inner.Split(',')
            .Select(a => Unquote(a.Trim()))
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();
    }

    private static bool TryReadKeyValue(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var idx = line.IndexOf(':');
        if (idx <= 0)
        {
            return false;
        }

        key = line.Substring(0, idx).Trim();
        value = line.Substring(idx + 1).Trim();
        return !string.IsNullOrWhiteSpace(key);
    }

    private static string Unquote(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var v = value.Trim();
        if (v.Length >= 2 && ((v[0] == '\'' && v[v.Length - 1] == '\'') || (v[0] == '"' && v[v.Length - 1] == '"')))
        {
            return v.Substring(1, v.Length - 2);
        }

        return v;
    }

    private static IEnumerable<string> SplitLines(string yaml)
    {
        return yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }
}

