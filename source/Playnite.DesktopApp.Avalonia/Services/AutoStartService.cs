using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Playnite.DesktopApp.Avalonia.Services;

public static class AutoStartService
{
    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string WindowsValueName = "Playnite.Avalonia";

    public static bool IsSupported()
    {
        return OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    }

    public static bool TrySetEnabled(bool enabled, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                SetWindows(enabled);
                return true;
            }

            if (OperatingSystem.IsLinux())
            {
                SetLinux(enabled);
                return true;
            }

            if (OperatingSystem.IsMacOS())
            {
                SetMacOs(enabled);
                return true;
            }

            errorMessage = "Unsupported OS.";
            return false;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SetWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(WindowsRunKeyPath);
        if (key is null)
        {
            throw new InvalidOperationException("Failed to open Windows Run registry key.");
        }

        if (!enabled)
        {
            key.DeleteValue(WindowsValueName, false);
            return;
        }

        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            throw new InvalidOperationException("Failed to resolve process path.");
        }

        key.SetValue(WindowsValueName, $"\"{exe}\"");
    }

    private static void SetLinux(bool enabled)
    {
        var autostartDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "autostart");

        var filePath = Path.Combine(autostartDir, "playnite-avalonia.desktop");
        if (!enabled)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return;
        }

        Directory.CreateDirectory(autostartDir);
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            throw new InvalidOperationException("Failed to resolve process path.");
        }

        var desktopEntry = string.Join('\n', new[]
        {
            "[Desktop Entry]",
            "Type=Application",
            "Name=Playnite (Avalonia)",
            $"Exec={EscapeLinuxDesktopExec(exe)}",
            "X-GNOME-Autostart-enabled=true",
            ""
        });

        File.WriteAllText(filePath, desktopEntry);
    }

    private static string EscapeLinuxDesktopExec(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void SetMacOs(bool enabled)
    {
        var agentsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "LaunchAgents");

        var filePath = Path.Combine(agentsDir, "com.playnite.avalonia.plist");
        if (!enabled)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return;
        }

        Directory.CreateDirectory(agentsDir);
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            throw new InvalidOperationException("Failed to resolve process path.");
        }

        var plist = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
  <dict>
    <key>Label</key>
    <string>com.playnite.avalonia</string>
    <key>ProgramArguments</key>
    <array>
      <string>{EscapeXml(exe)}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
  </dict>
</plist>
";

        File.WriteAllText(filePath, plist);
    }

    private static string EscapeXml(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
