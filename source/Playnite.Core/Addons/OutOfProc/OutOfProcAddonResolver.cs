using System;
using System.IO;

namespace Playnite.Addons.OutOfProc;

public static class OutOfProcAddonResolver
{
    public static bool IsOutOfProc(AddonManifest manifest)
    {
        if (manifest is null)
        {
            return false;
        }

        return string.Equals(manifest.Mode, "OutOfProc", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(manifest.Mode, "OutOfProcess", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(manifest.Mode, "ExternalProcess", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool TryResolve(AddonManifest manifest, out OutOfProcAddonDescriptor descriptor, out string errorMessage)
    {
        descriptor = null;
        errorMessage = string.Empty;

        if (manifest is null)
        {
            errorMessage = "Manifest is null.";
            return false;
        }

        if (!IsOutOfProc(manifest))
        {
            errorMessage = "Add-on is not marked as out-of-proc.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest.InstallDirectory) || !Directory.Exists(manifest.InstallDirectory))
        {
            errorMessage = "Missing install directory.";
            return false;
        }

        var module = SelectModuleForCurrentOs(manifest);
        if (string.IsNullOrWhiteSpace(module))
        {
            errorMessage = "Missing entrypoint (Module/ModuleWindows/ModuleLinux/ModuleMacOS).";
            return false;
        }

        var entryPath = ResolvePathInsideInstallDir(manifest.InstallDirectory, module, out var resolvedError);
        if (entryPath is null)
        {
            errorMessage = resolvedError;
            return false;
        }

        var args = manifest.Arguments ?? string.Empty;
        var workingDir = ResolveWorkingDirectory(manifest.InstallDirectory, manifest.WorkingDirectory, out resolvedError);
        if (workingDir is null)
        {
            errorMessage = resolvedError;
            return false;
        }

        string fileName;
        string arguments;
        if (entryPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "dotnet";
            arguments = $"\"{entryPath}\"";
            if (!string.IsNullOrWhiteSpace(args))
            {
                arguments += " " + args;
            }
        }
        else
        {
            fileName = entryPath;
            arguments = args;
        }

        descriptor = new OutOfProcAddonDescriptor(manifest, fileName, arguments, workingDir);
        return true;
    }

    private static string SelectModuleForCurrentOs(AddonManifest manifest)
    {
        if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(manifest.ModuleWindows))
        {
            return manifest.ModuleWindows;
        }

        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(manifest.ModuleLinux))
        {
            return manifest.ModuleLinux;
        }

        if (OperatingSystem.IsMacOS() && !string.IsNullOrWhiteSpace(manifest.ModuleMacOS))
        {
            return manifest.ModuleMacOS;
        }

        return manifest.Module;
    }

    private static string? ResolveWorkingDirectory(string installDir, string workingDirectory, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return installDir;
        }

        var resolved = ResolvePathInsideInstallDir(installDir, workingDirectory, out errorMessage);
        if (resolved is null)
        {
            return null;
        }

        if (!Directory.Exists(resolved))
        {
            errorMessage = "WorkingDirectory does not exist.";
            return null;
        }

        return resolved;
    }

    private static string? ResolvePathInsideInstallDir(string installDir, string relativePathOrFileName, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePathOrFileName))
        {
            errorMessage = "Path is empty.";
            return null;
        }

        var fullInstall = Path.GetFullPath(installDir);
        if (!fullInstall.EndsWith(Path.DirectorySeparatorChar))
        {
            fullInstall += Path.DirectorySeparatorChar;
        }

        string candidate;
        if (Path.IsPathRooted(relativePathOrFileName))
        {
            candidate = Path.GetFullPath(relativePathOrFileName);
        }
        else
        {
            candidate = Path.GetFullPath(Path.Combine(installDir, relativePathOrFileName));
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!candidate.StartsWith(fullInstall, comparison))
        {
            errorMessage = "Entrypoint path must be inside add-on install directory.";
            return null;
        }

        if (!File.Exists(candidate) && !Directory.Exists(candidate))
        {
            errorMessage = "Entrypoint path not found.";
            return null;
        }

        return candidate;
    }
}
