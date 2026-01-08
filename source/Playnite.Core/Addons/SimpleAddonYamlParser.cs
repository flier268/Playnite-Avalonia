using System;
using System.Collections.Generic;
using System.Globalization;

namespace Playnite.Addons;

internal static class SimpleAddonYamlParser
{
    public static Dictionary<string, string> ParseKeyValues(string yaml)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return result;
        }

        var lines = yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var rawLine in lines)
        {
            var line = rawLine?.TrimEnd() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var idx = trimmed.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }

            var key = trimmed.Substring(0, idx).Trim();
            var value = trimmed.Substring(idx + 1).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = Unquote(value);
        }

        return result;
    }

    public static Version ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Version(0, 0);
        }

        if (Version.TryParse(value.Trim(), out var v))
        {
            return v;
        }

        try
        {
            var cleaned = value.Trim();
            var parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
            {
                return new Version(major, minor);
            }
        }
        catch
        {
        }

        return new Version(0, 0);
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
}

