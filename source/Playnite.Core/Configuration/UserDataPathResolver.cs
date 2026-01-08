using System;
using System.IO;

namespace Playnite.Configuration
{
    public static class UserDataPathResolver
    {
        public static string GetDefaultUserDataPath()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            return Path.Combine(basePath, "Playnite");
        }
    }
}

