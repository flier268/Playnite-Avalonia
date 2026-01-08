using System;
using System.Collections.Generic;

namespace Playnite.Addons.OutOfProc;

public sealed class OutOfProcAddonStatus
{
    public string AddonId { get; init; } = string.Empty;
    public bool IsRunning { get; init; }
    public DateTime LastStartUtc { get; init; }
    public string LastError { get; init; } = string.Empty;
    public IReadOnlyList<string> StderrTail { get; init; } = Array.Empty<string>();
}

