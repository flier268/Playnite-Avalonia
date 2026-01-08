using System;

namespace Playnite.Library;

public static class LibraryStoreFactory
{
    public static ILibraryStore CreateFromEnvironment()
    {
        return Create(Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH"));
    }

    public static ILibraryStore Create(Playnite.Configuration.AppSettings settings)
    {
        var env = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return Create(env);
        }

        return Create(settings?.LibraryDbPath);
    }

    public static ILibraryStore Create(string rootPath)
    {
        if (!string.IsNullOrWhiteSpace(rootPath) && LiteDbLibraryStore.CanOpen(rootPath))
        {
            return new LiteDbLibraryStore(rootPath);
        }

        return new EmptyLibraryStore();
    }
}
