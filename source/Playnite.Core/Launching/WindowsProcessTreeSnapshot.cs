using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Playnite.Launching;

[SupportedOSPlatform("windows")]
internal static class WindowsProcessTreeSnapshot
{
    public static List<(int Pid, int ParentPid)> TryCapture()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var handle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (handle == IntPtr.Zero || handle == INVALID_HANDLE_VALUE)
        {
            return null;
        }

        try
        {
            var result = new List<(int Pid, int ParentPid)>();
            var entry = new PROCESSENTRY32();
            entry.dwSize = (uint)Marshal.SizeOf(entry);

            if (!Process32First(handle, ref entry))
            {
                return result;
            }

            do
            {
                result.Add(((int)entry.th32ProcessID, (int)entry.th32ParentProcessID));
            }
            while (Process32Next(handle, ref entry));

            return result;
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                CloseHandle(handle);
            }
            catch
            {
            }
        }
    }

    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
