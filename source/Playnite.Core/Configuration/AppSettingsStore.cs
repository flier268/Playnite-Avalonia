using System;
using System.IO;
using System.Text.Json;

namespace Playnite.Configuration
{
    public sealed class AppSettingsStore
    {
        private readonly object syncLock = new object();
        private readonly string filePath;

        public AppSettingsStore(string filePath)
        {
            this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public string FilePath => filePath;

        public AppSettings Load()
        {
            lock (syncLock)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        return new AppSettings();
                    }

                    var json = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new AppSettings();
                    }

                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            lock (syncLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
            }
        }
    }
}
