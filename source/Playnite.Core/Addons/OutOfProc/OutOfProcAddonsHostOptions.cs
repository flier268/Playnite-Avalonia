namespace Playnite.Addons.OutOfProc;

public sealed class OutOfProcAddonsHostOptions
{
    public int RequestTimeoutMs { get; init; } = 5000;
    public int RestartLimitPerMinute { get; init; } = 3;
    public int StderrTailLines { get; init; } = 50;
}

