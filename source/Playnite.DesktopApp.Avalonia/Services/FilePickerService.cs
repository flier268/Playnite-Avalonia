using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Playnite.DesktopApp.Avalonia.Services;

public static class FilePickerService
{
    public static async Task<string?> PickPackageFileAsync()
    {
        if (AppServices.MainWindow is null)
        {
            return null;
        }

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var types = new List<FilePickerFileType>
            {
                new FilePickerFileType("Playnite add-on packages")
                {
                    Patterns = new[] { "*.pext", "*.pthm", "*.zip" }
                },
                FilePickerFileTypes.All
            };

            var options = new FilePickerOpenOptions
            {
                Title = "Select add-on package",
                AllowMultiple = false,
                FileTypeFilter = types
            };

            var files = await AppServices.MainWindow.StorageProvider.OpenFilePickerAsync(options);
            var file = files?.FirstOrDefault();
            return file?.TryGetLocalPath();
        });
    }

    public static async Task<string?> PickFolderAsync(string title)
    {
        if (AppServices.MainWindow is null)
        {
            return null;
        }

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var options = new FolderPickerOpenOptions
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Select folder" : title,
                AllowMultiple = false
            };

            var folders = await AppServices.MainWindow.StorageProvider.OpenFolderPickerAsync(options);
            var folder = folders?.FirstOrDefault();
            return folder?.TryGetLocalPath();
        });
    }
}
