using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Playnite.Launching;

internal sealed class ProcessTreeTracker
{
    private readonly int rootPid;
    private readonly HashSet<int> trackedPids = new HashSet<int>();

    public ProcessTreeTracker(Process rootProcess)
    {
        if (rootProcess is null)
        {
            throw new ArgumentNullException(nameof(rootProcess));
        }

        rootPid = rootProcess.Id;
        trackedPids.Add(rootPid);
    }

    public bool IsAnyTrackedProcessAlive()
    {
        var snapshot = ProcessTreeSnapshot.TryCapture();
        if (snapshot is null)
        {
            // Fallback: if we can't snapshot process tree, at least track the original PID.
            return IsProcessAlive(rootPid);
        }

        ExpandTrackedPids(snapshot);
        return snapshot.IsAnyAlive(trackedPids);
    }

    private void ExpandTrackedPids(ProcessTreeSnapshot snapshot)
    {
        // Track newly discovered descendants and keep tracking them even if they become orphaned later.
        var queue = new Queue<int>(trackedPids);
        var visited = new HashSet<int>(trackedPids);

        while (queue.Count > 0)
        {
            var parentPid = queue.Dequeue();
            foreach (var childPid in snapshot.GetChildren(parentPid))
            {
                if (visited.Add(childPid))
                {
                    trackedPids.Add(childPid);
                    queue.Enqueue(childPid);
                }
            }
        }
    }

    private static bool IsProcessAlive(int pid)
    {
        if (pid <= 0)
        {
            return false;
        }

        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
