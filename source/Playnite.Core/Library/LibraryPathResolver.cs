using System.IO;

namespace Playnite.Library;

public static class LibraryPathResolver
{
    public static bool TryResolveDbFilePath(string libraryRootPath, string dbRelativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(libraryRootPath) || string.IsNullOrWhiteSpace(dbRelativePath))
        {
            return false;
        }

        var relativePath = dbRelativePath.Replace('/', Path.DirectorySeparatorChar);
        fullPath = Path.Combine(libraryRootPath, "files", relativePath);
        return File.Exists(fullPath);
    }
}

