using System;
using System.IO;
using NUnit.Framework;
using Playnite.Addons.OutOfProc;

namespace Playnite.Tests.Avalonia;

public class OutOfProcAddonPingTests
{
    [Test]
    public void PingOverStdioWorks()
    {
        var testsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var addonDll = Path.Combine(testsDir, "TestOutOfProcAddon", "bin", "Release", "net10.0", "TestOutOfProcAddon.dll");

        Assert.That(File.Exists(addonDll), Is.True, $"Missing test add-on: {addonDll}");

        var ok = OutOfProcAddonUtilities.TryPing(
            fileName: "dotnet",
            arguments: $"\"{addonDll}\"",
            workingDirectory: testsDir,
            errorMessage: out var error);

        Assert.That(ok, Is.True, error);
    }

    [Test]
    public void GenericGetCommandsWorks()
    {
        var testsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var addonDll = Path.Combine(testsDir, "TestOutOfProcAddon", "bin", "Release", "net10.0", "TestOutOfProcAddon.dll");

        Assert.That(File.Exists(addonDll), Is.True, $"Missing test add-on: {addonDll}");

        var ok = OutOfProcAddonUtilities.TryGetGenericCommandCount(
            fileName: "dotnet",
            arguments: $"\"{addonDll}\"",
            workingDirectory: testsDir,
            commandCount: out var count,
            errorMessage: out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(count, Is.GreaterThanOrEqualTo(1));
    }
}
