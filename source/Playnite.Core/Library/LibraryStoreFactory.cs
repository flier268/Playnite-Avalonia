using System;
using System.IO;
using Playnite.Configuration;

namespace Playnite.Library;

public static class LibraryStoreFactory
{
    public static ILibraryStore CreateFromEnvironment()
    {
        var explicitDbPath = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        if (!string.IsNullOrWhiteSpace(explicitDbPath))
        {
            return Create(explicitDbPath);
        }

        return Create(GetDefaultLibraryDbPath());
    }

    public static ILibraryStore Create(Playnite.Configuration.AppSettings settings)
    {
        var explicitDbPath = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        if (!string.IsNullOrWhiteSpace(explicitDbPath))
        {
            return Create(explicitDbPath);
        }

        if (!string.IsNullOrWhiteSpace(settings?.LibraryDbPath))
        {
            return Create(settings.LibraryDbPath);
        }

        return Create(GetDefaultLibraryDbPath());
    }

    public static ILibraryStore Create(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return new EmptyLibraryStore();
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(rootPath))
        {
            try
            {
                Directory.CreateDirectory(rootPath);
            }
            catch
            {
                return new EmptyLibraryStore();
            }
        }

        // Initialize database if it doesn't exist
        if (!LiteDbLibraryStore.CanOpen(rootPath))
        {
            if (!LiteDbLibraryStore.TryInitialize(rootPath))
            {
                return new EmptyLibraryStore();
            }
        }

        return new LiteDbLibraryStore(rootPath);
    }

    private static string GetDefaultLibraryDbPath()
    {
        var userDataRoot = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");
        if (string.IsNullOrWhiteSpace(userDataRoot))
        {
            userDataRoot = UserDataPathResolver.GetDefaultUserDataPath();
        }

        var defaultLibraryPath = Path.Combine(userDataRoot, "library");
        if (LiteDbLibraryStore.CanOpen(defaultLibraryPath))
        {
            return defaultLibraryPath;
        }

        if (LegacyPlayniteConfigReader.TryGetDatabasePathFromKnownLocations(out var legacyDbPath)
            && LiteDbLibraryStore.CanOpen(legacyDbPath))
        {
            return legacyDbPath;
        }

        return defaultLibraryPath;
    }
}
