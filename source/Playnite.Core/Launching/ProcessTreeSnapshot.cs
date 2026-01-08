using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Playnite.Launching;

internal sealed class ProcessTreeSnapshot
{
    private readonly Dictionary<int, List<int>> childrenByParentPid;
    private readonly HashSet<int> alivePids;

    private ProcessTreeSnapshot(Dictionary<int, List<int>> childrenByParentPid, HashSet<int> alivePids)
    {
        this.childrenByParentPid = childrenByParentPid;
        this.alivePids = alivePids;
    }

    public static ProcessTreeSnapshot TryCapture()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var win = WindowsProcessTreeSnapshot.TryCapture();
                if (win is null)
                {
                    return null;
                }

                return FromPidPairs(win);
            }

            if (OperatingSystem.IsLinux())
            {
                return CaptureLinux();
            }

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                return CaptureUnixViaPs();
            }

            return CaptureManagedBestEffort();
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<int> GetChildren(int parentPid)
    {
        return childrenByParentPid.TryGetValue(parentPid, out var list) ? list : Array.Empty<int>();
    }

    public bool IsAnyAlive(HashSet<int> pids)
    {
        foreach (var pid in pids)
        {
            if (alivePids.Contains(pid))
            {
                return true;
            }
        }

        return false;
    }

    private static ProcessTreeSnapshot FromPidPairs(List<(int Pid, int ParentPid)> pairs)
    {
        var children = new Dictionary<int, List<int>>();
        var alive = new HashSet<int>();

        foreach (var (pid, ppid) in pairs)
        {
            if (pid <= 0)
            {
                continue;
            }

            alive.Add(pid);
            if (ppid > 0)
            {
                if (!children.TryGetValue(ppid, out var list))
                {
                    list = new List<int>();
                    children[ppid] = list;
                }

                list.Add(pid);
            }
        }

        return new ProcessTreeSnapshot(children, alive);
    }

    private static ProcessTreeSnapshot CaptureManagedBestEffort()
    {
        // No PPID information available; treat "alive pids" as known processes and allow tracker fallback
        // to original PID when no snapshot is possible.
        var alive = new HashSet<int>();
        try
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    alive.Add(p.Id);
                }
                catch
                {
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch
        {
        }

        return new ProcessTreeSnapshot(new Dictionary<int, List<int>>(), alive);
    }

    private static ProcessTreeSnapshot CaptureLinux()
    {
        var pairs = new List<(int Pid, int ParentPid)>();

        try
        {
            foreach (var dir in Directory.EnumerateDirectories("/proc"))
            {
                var name = Path.GetFileName(dir);
                if (!int.TryParse(name, out var pid))
                {
                    continue;
                }

                // /proc/<pid>/stat: pid (comm) state ppid ...
                // comm can contain spaces but is wrapped in parentheses. We need ppid which appears after ") ".
                var statPath = Path.Combine(dir, "stat");
                string stat;
                try
                {
                    stat = File.ReadAllText(statPath);
                }
                catch
                {
                    continue;
                }

                var rparen = stat.IndexOf(") ", StringComparison.Ordinal);
                if (rparen < 0)
                {
                    continue;
                }

                // After ") " comes: state (single char) + space + ppid + ...
                var after = stat.Substring(rparen + 2);
                var parts = after.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                if (!int.TryParse(parts[1], out var ppid))
                {
                    continue;
                }

                pairs.Add((pid, ppid));
            }
        }
        catch
        {
        }

        return FromPidPairs(pairs);
    }

    private static ProcessTreeSnapshot CaptureUnixViaPs()
    {
        // Works on macOS/FreeBSD; avoid adding native bindings.
        var pairs = new List<(int Pid, int ParentPid)>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = "-axo pid=,ppid=",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(startInfo);
            if (proc is null)
            {
                return CaptureManagedBestEffort();
            }

            var output = proc.StandardOutput.ReadToEnd();
            try
            {
                proc.WaitForExit(2000);
            }
            catch
            {
            }

            using var reader = new StringReader(output);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                var cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 2)
                {
                    continue;
                }

                if (!int.TryParse(cols[0], out var pid) || !int.TryParse(cols[1], out var ppid))
                {
                    continue;
                }

                pairs.Add((pid, ppid));
            }
        }
        catch
        {
            return CaptureManagedBestEffort();
        }

        return FromPidPairs(pairs);
    }
}
