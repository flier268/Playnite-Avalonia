using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Playnite.Launching;
using Playnite.SDK.Models;

namespace Playnite.Tests.Avalonia;

public sealed class ProcessTrackingTests
{
    [Test]
    public async Task ProcessTrackingTracksChildAfterParentExits()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("This test relies on Windows PowerShell availability.");
        }

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "ProcessTrackingTracksChildAfterParentExits"
        };

        var action = new GameAction
        {
            Type = GameActionType.File,
            Path = "powershell.exe",
            Arguments = "-NoProfile -Command \"Start-Process powershell -WindowStyle Hidden -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 2'; Start-Sleep -Milliseconds 500\"",
            TrackingMode = TrackingMode.Process,
            InitialTrackingDelay = 0,
            TrackingFrequency = 250
        };

        var launcher = new GameActionLauncher();
        var result = launcher.Launch(game, action);

        Assert.That(result.Started, Is.True, result.ErrorMessage);
        Assert.That(result.Session, Is.Not.Null);

        var sw = Stopwatch.StartNew();
        await result.Session.WaitForExitAsync();
        sw.Stop();

        Assert.That(sw.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(1.0)));
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20.0)));
    }
}

