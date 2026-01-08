using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Playnite.SDK.Models;

namespace Playnite.Launching;

public sealed class GameActionLauncher
{
    public GameLaunchResult Launch(Game game, GameAction action)
    {
        if (game is null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        try
        {
            return action.Type switch
            {
                GameActionType.File => LaunchFile(action),
                GameActionType.URL => LaunchUrl(action),
                GameActionType.Script => LaunchScript(game, action),
                GameActionType.Emulator => GameLaunchResult.Failed("Emulator launch is not implemented yet."),
                _ => GameLaunchResult.Failed($"Unsupported action type: {action.Type}.")
            };
        }
        catch (Exception e)
        {
            return GameLaunchResult.Failed(e.Message);
        }
    }

    private static GameLaunchResult LaunchFile(GameAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Path))
        {
            return GameLaunchResult.Failed("Action path is empty.");
        }

        var filePath = action.Path;
        var isRooted = Path.IsPathRooted(filePath);
        if (!isRooted)
        {
            // Non-rooted paths (e.g. "explorer.exe") are commonly resolvable via system search paths.
            // Only resolve to full path if it actually exists relative to current directory.
            try
            {
                var fullCandidate = Path.GetFullPath(filePath);
                if (File.Exists(fullCandidate))
                {
                    filePath = fullCandidate;
                    isRooted = true;
                }
            }
            catch
            {
            }
        }

        if (isRooted && !File.Exists(filePath))
        {
            return GameLaunchResult.Failed($"Executable not found: {filePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = filePath,
            Arguments = action.Arguments ?? string.Empty,
            WorkingDirectory = ResolveWorkingDirectory(action, filePath),
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            return GameLaunchResult.Failed("Failed to start process.");
        }

        return GameLaunchResult.Success(CreateSession(process, action, filePath));
    }

    private static GameLaunchResult LaunchScript(Game game, GameAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Script))
        {
            return GameLaunchResult.Failed("Script is empty.");
        }

        var playniteDir = AppContext.BaseDirectory ?? string.Empty;
        var expandedScript = LaunchVariableExpander.Expand(action.Script, game, string.Empty, string.Empty, playniteDir);

        var usePowerShell = OperatingSystem.IsWindows() || (!string.IsNullOrWhiteSpace(FindOnPath("pwsh")));
        var scriptExt = usePowerShell ? ".ps1" : ".sh";
        var scriptPath = Path.Combine(Path.GetTempPath(), $"playnite_{game.Id}_script_{Guid.NewGuid():N}{scriptExt}");
        try
        {
            if (usePowerShell)
            {
                File.WriteAllText(scriptPath, expandedScript);
            }
            else
            {
                var content = expandedScript.StartsWith("#!", StringComparison.Ordinal)
                    ? expandedScript
                    : "#!/bin/sh\n" + expandedScript;
                File.WriteAllText(scriptPath, content);
            }

            string exe;
            string args;
            if (usePowerShell)
            {
                exe = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
                args = OperatingSystem.IsWindows()
                    ? $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\""
                    : $"-NoProfile -File \"{scriptPath}\"";
            }
            else
            {
                exe = FindOnPath("sh") ?? "/bin/sh";
                args = $"\"{scriptPath}\"";
            }

            var expandedWorkDir = string.IsNullOrWhiteSpace(action.WorkingDir)
                ? (string.IsNullOrWhiteSpace(game.InstallDirectory) ? string.Empty : game.InstallDirectory)
                : LaunchVariableExpander.Expand(action.WorkingDir, game, string.Empty, string.Empty, playniteDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = expandedWorkDir,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);
            if (process is null)
            {
                return GameLaunchResult.Failed("Failed to start script process.");
            }

            try
            {
                process.EnableRaisingEvents = true;
                process.Exited += (_, _) =>
                {
                    try
                    {
                        File.Delete(scriptPath);
                    }
                    catch
                    {
                    }
                };
            }
            catch
            {
            }

            // Use the same tracking settings as File actions (TrackingPath can point to the real target started by the script).
            return GameLaunchResult.Success(CreateSession(process, action, exe));
        }
        catch (Exception e)
        {
            try
            {
                File.Delete(scriptPath);
            }
            catch
            {
            }

            return GameLaunchResult.Failed(e.Message);
        }
    }

    private static string FindOnPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var part in path.Split(Path.PathSeparator).Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            try
            {
                var candidate = Path.Combine(part.Trim(), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                if (OperatingSystem.IsWindows())
                {
                    var exeCandidate = candidate + ".exe";
                    if (File.Exists(exeCandidate))
                    {
                        return exeCandidate;
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static string ResolveWorkingDirectory(GameAction action, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(action.WorkingDir))
        {
            return action.WorkingDir;
        }

        try
        {
            return Path.GetDirectoryName(filePath) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static GameLaunchResult LaunchUrl(GameAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Path))
        {
            return GameLaunchResult.Failed("URL is empty.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = action.Path,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        return GameLaunchResult.Success();
    }

    private static IGameLaunchSession CreateSession(Process startedProcess, GameAction action, string startedFilePath)
    {
        var trackingMode = action.TrackingMode == TrackingMode.Default
            ? (OperatingSystem.IsWindows() ? TrackingMode.Process : TrackingMode.OriginalProcess)
            : action.TrackingMode;

        var initialDelay = action.InitialTrackingDelay;
        var frequency = action.TrackingFrequency;

        switch (trackingMode)
        {
            case TrackingMode.OriginalProcess:
                return new ProcessGameLaunchSession(startedProcess);
            case TrackingMode.ProcessName:
                return new PollingGameLaunchSession(
                    () => IsAnyProcessWithNameRunning(GetProcessName(action.TrackingPath, startedFilePath)),
                    initialDelay,
                    frequency);
            case TrackingMode.Directory:
                return new PollingGameLaunchSession(
                    () => IsAnyProcessInDirectoryRunning(GetTrackingDirectory(action.TrackingPath, startedFilePath)),
                    initialDelay,
                    frequency);
            case TrackingMode.Process:
                var tracker = new ProcessTreeTracker(startedProcess);
                return new PollingGameLaunchSession(tracker.IsAnyTrackedProcessAlive, initialDelay, frequency);
            default:
                return new ProcessGameLaunchSession(startedProcess);
        }
    }

    private static string GetProcessName(string trackingPath, string startedFilePath)
    {
        var name = trackingPath;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = startedFilePath;
        }

        try
        {
            return Path.GetFileNameWithoutExtension(name);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetTrackingDirectory(string trackingPath, string startedFilePath)
    {
        if (!string.IsNullOrWhiteSpace(trackingPath))
        {
            return trackingPath;
        }

        try
        {
            return Path.GetDirectoryName(startedFilePath) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsAnyProcessWithNameRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        try
        {
            return Process.GetProcessesByName(processName).Any();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAnyProcessInDirectoryRunning(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return false;
        }

        var normalized = NormalizeDirectory(directoryPath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        try
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var modulePath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(modulePath))
                    {
                        continue;
                    }

                    if (modulePath.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeDirectory(string value)
    {
        try
        {
            var full = Path.GetFullPath(value);
            if (!full.EndsWith(Path.DirectorySeparatorChar))
            {
                full += Path.DirectorySeparatorChar;
            }

            return full;
        }
        catch
        {
            return string.Empty;
        }
    }
}
