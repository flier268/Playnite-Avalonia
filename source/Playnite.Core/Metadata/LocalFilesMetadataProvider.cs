using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Playnite.Metadata;

public sealed class LocalFilesMetadataProvider : IMetadataProvider
{
    public string Id => "builtin.localfiles";
    public string Name => "Local Files";

    public OnDemandMetadataProvider CreateProvider(Game game)
    {
        return new LocalFilesOnDemandMetadataProvider(game);
    }

    private sealed class LocalFilesOnDemandMetadataProvider : OnDemandMetadataProvider
    {
        private const long MaxReadBytes = 20L * 1024L * 1024L;
        private readonly Game game;
        private readonly string iconPath;
        private readonly string coverPath;
        private readonly string backgroundPath;
        private readonly List<MetadataField> availableFields;

        public LocalFilesOnDemandMetadataProvider(Game game)
        {
            this.game = game ?? new Game();

            var searchRoots = GetSearchRoots(this.game).ToList();
            iconPath = FindBestMatch(searchRoots, new[] { "icon" }, new[] { ".png", ".ico", ".jpg", ".jpeg", ".bmp" });
            coverPath = FindBestMatch(searchRoots, new[] { "cover", "box", "boxart", "poster" }, new[] { ".png", ".jpg", ".jpeg", ".bmp" });
            backgroundPath = FindBestMatch(searchRoots, new[] { "background", "fanart", "banner" }, new[] { ".png", ".jpg", ".jpeg", ".bmp" });

            availableFields = new List<MetadataField>();
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                availableFields.Add(MetadataField.Icon);
            }

            if (!string.IsNullOrWhiteSpace(coverPath))
            {
                availableFields.Add(MetadataField.CoverImage);
            }

            if (!string.IsNullOrWhiteSpace(backgroundPath))
            {
                availableFields.Add(MetadataField.BackgroundImage);
            }
        }

        public override List<MetadataField> AvailableFields => availableFields;

        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return TryReadFile(iconPath);
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return TryReadFile(coverPath);
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return TryReadFile(backgroundPath);
        }

        private static IEnumerable<string> GetSearchRoots(Game game)
        {
            if (!string.IsNullOrWhiteSpace(game.InstallDirectory) && Directory.Exists(game.InstallDirectory))
            {
                yield return game.InstallDirectory;
            }

            var playAction = game.GameActions?.FirstOrDefault(a => a.IsPlayAction);
            var actionPath = playAction?.Path;
            if (!string.IsNullOrWhiteSpace(actionPath) && Path.IsPathRooted(actionPath))
            {
                string dir = null;
                try
                {
                    dir = Path.GetDirectoryName(actionPath);
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    yield return dir;
                }
            }
        }

        private static string FindBestMatch(IEnumerable<string> roots, IReadOnlyList<string> keywords, IReadOnlyList<string> extensions)
        {
            foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (var keyword in keywords)
                {
                    foreach (var ext in extensions)
                    {
                        var candidate = Path.Combine(root, keyword + ext);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }

                try
                {
                    var files = Directory.EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f =>
                        {
                            var ext = Path.GetExtension(f);
                            return extensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase));
                        })
                        .Select(f => new { Path = f, Name = Path.GetFileNameWithoutExtension(f) ?? string.Empty })
                        .ToList();

                    foreach (var keyword in keywords)
                    {
                        var hit = files.FirstOrDefault(f => string.Equals(f.Name, keyword, StringComparison.OrdinalIgnoreCase));
                        if (hit != null)
                        {
                            return hit.Path;
                        }

                        hit = files.FirstOrDefault(f => f.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (hit != null)
                        {
                            return hit.Path;
                        }
                    }
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private static MetadataFile TryReadFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var info = new FileInfo(path);
                if (info.Length <= 0 || info.Length > MaxReadBytes)
                {
                    return null;
                }

                var bytes = File.ReadAllBytes(path);
                return new MetadataFile(Path.GetFileName(path), bytes);
            }
            catch
            {
                return null;
            }
        }
    }
}
