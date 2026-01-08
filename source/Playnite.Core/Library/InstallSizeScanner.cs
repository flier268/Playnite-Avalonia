using System;
using System.Collections.Generic;
using System.IO;

namespace Playnite.Library;

public static class InstallSizeScanner
{
    public static bool TryGetDirectorySizeBytes(string rootPath, out ulong sizeBytes)
    {
        sizeBytes = 0;
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        if (!Directory.Exists(rootPath))
        {
            return false;
        }

        try
        {
            ulong total = 0;
            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                var dir = pending.Pop();
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            var attrs = File.GetAttributes(dir);
                            if ((attrs & FileAttributes.ReparsePoint) != 0)
                            {
                                continue;
                            }
                        }
                        catch
                        {
                        }
                    }

                    IEnumerable<string> subdirs;
                    try
                    {
                        subdirs = Directory.EnumerateDirectories(dir);
                    }
                    catch
                    {
                        subdirs = Array.Empty<string>();
                    }

                    foreach (var subdir in subdirs)
                    {
                        pending.Push(subdir);
                    }

                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(dir);
                    }
                    catch
                    {
                        files = Array.Empty<string>();
                    }

                    foreach (var file in files)
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            if (!info.Exists)
                            {
                                continue;
                            }

                            total += (ulong)Math.Max(0, info.Length);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
            }

            sizeBytes = total;
            return true;
        }
        catch
        {
            return false;
        }
    }
}

