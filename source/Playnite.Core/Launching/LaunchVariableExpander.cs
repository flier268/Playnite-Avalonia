using System;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Playnite.Launching;

public static class LaunchVariableExpander
{
    public static string Expand(string input, Game game, string emulatorDir, string imagePath, string playniteDir)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = input;

        if (game != null)
        {
            if (!string.IsNullOrWhiteSpace(game.InstallDirectory))
            {
                result = result.Replace(ExpandableVariables.InstallationDirectory, game.InstallDirectory, StringComparison.OrdinalIgnoreCase);
                result = result.Replace(ExpandableVariables.InstallationDirName, GetLastDirName(game.InstallDirectory), StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(game.Name))
            {
                result = result.Replace(ExpandableVariables.Name, game.Name, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(game.GameId))
            {
                result = result.Replace(ExpandableVariables.GameId, game.GameId, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(game.Version))
            {
                result = result.Replace(ExpandableVariables.Version, game.Version, StringComparison.OrdinalIgnoreCase);
            }

            result = result.Replace(ExpandableVariables.DatabaseId, game.Id.ToString(), StringComparison.OrdinalIgnoreCase);
            result = result.Replace(ExpandableVariables.PluginId, game.PluginId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            result = result.Replace(ExpandableVariables.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase);
            result = result.Replace(ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(imagePath), StringComparison.OrdinalIgnoreCase);
            result = result.Replace(ExpandableVariables.ImageName, Path.GetFileName(imagePath), StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(emulatorDir))
        {
            result = result.Replace(ExpandableVariables.EmulatorDirectory, emulatorDir, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(playniteDir))
        {
            result = result.Replace(ExpandableVariables.PlayniteDirectory, playniteDir, StringComparison.OrdinalIgnoreCase);
        }

        return FixSeparators(result);
    }

    private static string GetLastDirName(string path)
    {
        try
        {
            return path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FixSeparators(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var separator = Path.DirectorySeparatorChar;
        var other = separator == '/' ? '\\' : '/';
        return value.Replace(other, separator);
    }
}

