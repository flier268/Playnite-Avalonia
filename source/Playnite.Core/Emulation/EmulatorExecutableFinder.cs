using System;
using System.IO;
using System.Text.RegularExpressions;
using Playnite.Common;

namespace Playnite.Emulation;

public static class EmulatorExecutableFinder
{
    public static bool TryFindExecutable(string emulatorInstallDir, string startupExecutablePattern, out string executablePath)
    {
        executablePath = string.Empty;

        if (string.IsNullOrWhiteSpace(emulatorInstallDir) || string.IsNullOrWhiteSpace(startupExecutablePattern))
        {
            return false;
        }

        if (!Directory.Exists(emulatorInstallDir))
        {
            return false;
        }

        Regex regex;
        try
        {
            regex = new Regex(startupExecutablePattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            var directPath = Path.Combine(emulatorInstallDir, startupExecutablePattern);
            if (File.Exists(directPath))
            {
                executablePath = directPath;
                return true;
            }

            return false;
        }

        try
        {
            foreach (var file in new SafeFileEnumerator(emulatorInstallDir, "*.*", SearchOption.AllDirectories))
            {
                if (file.Attributes.HasFlag(FileAttributes.Directory))
                {
                    continue;
                }

                var name = file.Name;
                if (regex.IsMatch(name))
                {
                    executablePath = file.FullName;
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }
}

