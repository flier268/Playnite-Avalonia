using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Playnite.Common
{
    /// <summary>
    /// Cross-platform helper for Windows Shell Links (.lnk files)
    /// Uses COM interop on Windows, gracefully degrades on other platforms
    /// </summary>
    public static class ShellLinkHelper
    {
        public static void CreateShortcut(string executablePath, string arguments, string iconPath, string shortcutPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(".lnk files are Windows-specific");
            }

            var link = (IShellLinkW)new CShellLink();
            link.SetPath(executablePath);
            link.SetWorkingDirectory(Path.GetDirectoryName(executablePath));
            link.SetArguments(arguments);
            var iconLocation = string.IsNullOrEmpty(iconPath) ? $"{executablePath},0" : iconPath;
            ParseIconLocation(iconLocation, out var iconFile, out var iconIndex);
            link.SetIconLocation(iconFile, iconIndex);

            var file = (IPersistFile)link;
            file.Save(shortcutPath, false);
            Marshal.ReleaseComObject(link);
        }

        public static ShortcutData ReadShortcut(string lnkPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(".lnk files are Windows-specific");
            }

            var link = (IShellLinkW)new CShellLink();
            var file = (IPersistFile)link;
            file.Load(lnkPath, 0);

            var data = new ShortcutData();
            var sb = new StringBuilder(260);

            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            data.TargetPath = sb.ToString();

            sb.Clear();
            link.GetWorkingDirectory(sb, sb.Capacity);
            data.WorkingDirectory = sb.ToString();

            sb.Clear();
            link.GetArguments(sb, sb.Capacity);
            data.Arguments = sb.ToString();

            sb.Clear();
            link.GetIconLocation(sb, sb.Capacity, out int iconIndex);
            var iconPath = sb.ToString();
            data.IconLocation = string.IsNullOrEmpty(iconPath) ? string.Empty : $"{iconPath},{iconIndex}";
            data.IconIndex = iconIndex;

            Marshal.ReleaseComObject(link);
            return data;
        }

        private static void ParseIconLocation(string iconLocation, out string iconPath, out int iconIndex)
        {
            iconPath = iconLocation;
            iconIndex = 0;

            if (string.IsNullOrEmpty(iconLocation))
            {
                return;
            }

            var separatorIndex = iconLocation.LastIndexOf(',');
            if (separatorIndex <= 0 || separatorIndex == iconLocation.Length - 1)
            {
                return;
            }

            var indexText = iconLocation.Substring(separatorIndex + 1);
            if (int.TryParse(indexText, out var parsedIndex))
            {
                iconPath = iconLocation.Substring(0, separatorIndex);
                iconIndex = parsedIndex;
            }
        }

        public class ShortcutData
        {
            public string TargetPath { get; set; }
            public string WorkingDirectory { get; set; }
            public string Arguments { get; set; }
            public string IconLocation { get; set; }
            public int IconIndex { get; set; }
        }

        // COM Interop definitions
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class CShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short pwHotkey);
            void GetShowCmd(out uint piShowCmd);
            void SetShowCmd(uint piShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hWnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
    }
}
