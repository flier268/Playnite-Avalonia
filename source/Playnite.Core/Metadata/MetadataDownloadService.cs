using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.Library;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace Playnite.Metadata;

public sealed class MetadataDownloadService
{
    private readonly IReadOnlyList<IMetadataProvider> providers;
    private readonly ILibraryStore libraryStore;

    public MetadataDownloadService(IReadOnlyList<IMetadataProvider> providers, ILibraryStore libraryStore)
    {
        this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
        this.libraryStore = libraryStore ?? throw new ArgumentNullException(nameof(libraryStore));
    }

    public IReadOnlyList<IMetadataProvider> GetProviders()
    {
        return providers;
    }

    public MetadataDownloadResult Download(Game game, string providerAddonId)
    {
        return Download(game, providerAddonId, overwriteExisting: true);
    }

    public MetadataDownloadResult Download(Game game, string providerAddonId, bool overwriteExisting)
    {
        if (game == null)
        {
            return MetadataDownloadResult.Failed("Game is null.");
        }

        if (libraryStore is EmptyLibraryStore)
        {
            return MetadataDownloadResult.Failed("No library store is active.");
        }

        if (providers.Count == 0)
        {
            return MetadataDownloadResult.Failed("No metadata providers registered.");
        }

        var provider = providers.FirstOrDefault(p => string.Equals(p.Id, providerAddonId, StringComparison.OrdinalIgnoreCase)) ?? providers[0];

        try
        {
            using var onDemand = provider.CreateProvider(game);
            if (onDemand == null)
            {
                return MetadataDownloadResult.Failed("Provider returned no metadata provider instance.");
            }

            var args = new GetMetadataFieldArgs();
            var fields = onDemand.AvailableFields ?? new List<MetadataField>();

            if (fields.Contains(MetadataField.Name))
            {
                var name = onDemand.GetName(args);
                if (!string.IsNullOrWhiteSpace(name) && (overwriteExisting || string.IsNullOrWhiteSpace(game.Name)))
                {
                    game.Name = name;
                }
            }

            if (fields.Contains(MetadataField.Description))
            {
                var desc = onDemand.GetDescription(args);
                if (!string.IsNullOrWhiteSpace(desc) && (overwriteExisting || string.IsNullOrWhiteSpace(game.Description)))
                {
                    game.Description = desc;
                }
            }

            if (fields.Contains(MetadataField.ReleaseDate))
            {
                var date = onDemand.GetReleaseDate(args);
                if (date.HasValue && (overwriteExisting || !game.ReleaseDate.HasValue))
                {
                    game.ReleaseDate = date;
                }
            }

            if (fields.Contains(MetadataField.InstallSize))
            {
                var size = onDemand.GetInstallSize(args);
                if (size.HasValue && (overwriteExisting || !game.InstallSize.HasValue))
                {
                    game.InstallSize = size;
                    game.LastSizeScanDate = DateTime.UtcNow;
                }
            }

            if (fields.Contains(MetadataField.Icon))
            {
                var icon = onDemand.GetIcon(args);
                if ((overwriteExisting || string.IsNullOrWhiteSpace(game.Icon)) && TryWriteMetadataFile(icon, "icon", out var relative))
                {
                    game.Icon = relative;
                }
            }

            if (fields.Contains(MetadataField.CoverImage))
            {
                var cover = onDemand.GetCoverImage(args);
                if ((overwriteExisting || string.IsNullOrWhiteSpace(game.CoverImage)) && TryWriteMetadataFile(cover, "cover", out var relative))
                {
                    game.CoverImage = relative;
                }
            }

            if (fields.Contains(MetadataField.BackgroundImage))
            {
                var bg = onDemand.GetBackgroundImage(args);
                if ((overwriteExisting || string.IsNullOrWhiteSpace(game.BackgroundImage)) && TryWriteMetadataFile(bg, "background", out var relative))
                {
                    game.BackgroundImage = relative;
                }
            }

            if (!libraryStore.TryUpdateGameMetadata(game))
            {
                return MetadataDownloadResult.Failed("Failed to persist metadata to database.");
            }

            return MetadataDownloadResult.Ok();
        }
        catch (Exception e)
        {
            return MetadataDownloadResult.Failed(e.Message);
        }
    }

    private bool TryWriteMetadataFile(MetadataFile file, string kind, out string relativePath)
    {
        relativePath = string.Empty;

        if (file == null)
        {
            return false;
        }

        var root = libraryStore.RootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        if (!file.HasContent)
        {
            // Only support inline content for now (no network fetch).
            return false;
        }

        var fileName = file.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"{Guid.NewGuid():N}.bin";
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".bin";
        }

        var safeExt = ext.Length > 10 ? ".bin" : ext;
        var outName = $"meta_{kind}_{Guid.NewGuid():N}{safeExt}";

        var filesDir = Path.Combine(root, "files");
        Directory.CreateDirectory(filesDir);

        var outPath = Path.Combine(filesDir, outName);
        File.WriteAllBytes(outPath, file.Content);

        relativePath = outName;
        return true;
    }
}
