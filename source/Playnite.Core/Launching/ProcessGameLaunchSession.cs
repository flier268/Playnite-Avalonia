using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Playnite.Launching;

internal sealed class ProcessGameLaunchSession : IGameLaunchSession
{
    private readonly Process process;
    private readonly TaskCompletionSource<object> exitTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

    public ProcessGameLaunchSession(Process process)
    {
        this.process = process ?? throw new ArgumentNullException(nameof(process));
        StartTime = DateTime.UtcNow;

        if (process.HasExited)
        {
            exitTcs.TrySetResult(new object());
        }
        else
        {
            process.EnableRaisingEvents = true;
            process.Exited += (_, __) => exitTcs.TrySetResult(new object());
        }
    }

    public DateTime StartTime { get; }

    public bool HasExited
    {
        get
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }

    public Task WaitForExitAsync()
    {
        return exitTcs.Task;
    }
}
