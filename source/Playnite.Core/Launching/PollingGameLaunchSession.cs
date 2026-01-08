using System;
using System.Threading;
using System.Threading.Tasks;

namespace Playnite.Launching;

internal sealed class PollingGameLaunchSession : IGameLaunchSession
{
    private readonly Func<bool> isRunning;
    private readonly int initialDelayMs;
    private readonly int frequencyMs;
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private DateTime startTime;

    public PollingGameLaunchSession(Func<bool> isRunning, int initialDelayMs, int frequencyMs)
    {
        this.isRunning = isRunning ?? throw new ArgumentNullException(nameof(isRunning));
        this.initialDelayMs = Math.Max(0, initialDelayMs);
        this.frequencyMs = Math.Max(250, frequencyMs);
        startTime = DateTime.UtcNow;
    }

    public DateTime StartTime => startTime;

    public bool HasExited
    {
        get
        {
            try
            {
                return !isRunning();
            }
            catch
            {
                return true;
            }
        }
    }

    public async Task WaitForExitAsync()
    {
        if (initialDelayMs > 0)
        {
            await Task.Delay(initialDelayMs, cts.Token).ConfigureAwait(false);
            startTime = DateTime.UtcNow;
        }

        while (!cts.IsCancellationRequested)
        {
            if (HasExited)
            {
                return;
            }

            await Task.Delay(frequencyMs, cts.Token).ConfigureAwait(false);
        }
    }
}

