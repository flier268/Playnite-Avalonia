using System;
using System.IO;

namespace Playnite.Configuration
{
    public static class AppSettingsStoreFactory
    {
        public static AppSettingsStore CreateFromEnvironment()
        {
            var rootPath = Environment.GetEnvironmentVariable("PLAYNITE_USERDATA_PATH");
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                rootPath = UserDataPathResolver.GetDefaultUserDataPath();
            }

            var filePath = Path.Combine(rootPath, "avalonia", "settings.json");
            return new AppSettingsStore(filePath);
        }
    }
}

