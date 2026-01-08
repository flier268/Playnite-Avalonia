using System;
using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Playnite.DesktopApp.Avalonia.Services;
using Playnite.Library;
using Playnite.SDK.Models;

namespace Playnite.DesktopApp.Avalonia.Converters;

public sealed class GameCoverImageConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap> bitmapCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Game game)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(game.CoverImage))
        {
            return null;
        }

        var root = AppServices.LibraryStore?.RootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Environment.GetEnvironmentVariable("PLAYNITE_DB_PATH");
        }

        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        if (!LibraryPathResolver.TryResolveDbFilePath(root, game.CoverImage, out var fullPath))
        {
            return null;
        }

        try
        {
            return bitmapCache.GetOrAdd(fullPath, p => new Bitmap(p));
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
