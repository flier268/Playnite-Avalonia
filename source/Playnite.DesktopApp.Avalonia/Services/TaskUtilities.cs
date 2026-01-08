using System;
using System.Threading.Tasks;

namespace Playnite.DesktopApp.Avalonia.Services;

public static class TaskUtilities
{
    public static void FireAndForget(Task task)
    {
        if (task is null)
        {
            return;
        }

        task.ContinueWith(t =>
        {
            _ = t.Exception;
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}

