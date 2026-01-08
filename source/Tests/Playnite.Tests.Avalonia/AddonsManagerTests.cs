using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Playnite.Addons;

namespace Playnite.Tests.Avalonia;

public sealed class AddonsManagerTests
{
    [Test]
    public void InstallsAndUninstallsExtensionPackage()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "playnite_addons_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var userData = Path.Combine(tempRoot, "userdata");
            Directory.CreateDirectory(userData);

            var manager = new AddonsManager(userData, string.Empty);

            var packagePath = Path.Combine(tempRoot, "test.pext");
            CreateZip(packagePath, new (string Path, string Content)[]
            {
                ("extension.yaml", "Id: Test_Ext\nName: Test Extension\nAuthor: Test\nVersion: 1.2\nModule: Test.dll\nType: GenericPlugin\n"),
                ("Test.dll", "dummy")
            });

            var install = manager.InstallFromPackage(packagePath);
            Assert.That(install.Success, Is.True, install.ErrorMessage);
            Assert.That(install.Manifest, Is.Not.Null);
            Assert.That(install.Manifest!.Id, Is.EqualTo("Test_Ext"));
            Assert.That(Directory.Exists(install.Manifest.InstallDirectory), Is.True);

            var installed = manager.GetInstalledExtensions();
            Assert.That(installed, Has.Some.Matches<AddonManifest>(a => a.Id == "Test_Ext"));

            Assert.That(manager.Uninstall(install.Manifest), Is.True);
            Assert.That(Directory.Exists(install.Manifest.InstallDirectory), Is.False);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch
            {
            }
        }
    }

    [Test]
    public void InstallsThemePackage()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "playnite_addons_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var userData = Path.Combine(tempRoot, "userdata");
            Directory.CreateDirectory(userData);

            var manager = new AddonsManager(userData, string.Empty);

            var packagePath = Path.Combine(tempRoot, "test.pthm");
            CreateZip(packagePath, new (string Path, string Content)[]
            {
                ("theme.yaml", "Id: Test_Theme\nName: Test Theme\nAuthor: Test\nVersion: 1.0\nMode: Desktop\nThemeApiVersion: 2.0.0\n"),
                ("Themes.xaml", "dummy")
            });

            var install = manager.InstallFromPackage(packagePath);
            Assert.That(install.Success, Is.True, install.ErrorMessage);
            Assert.That(install.Manifest, Is.Not.Null);
            Assert.That(install.Manifest!.Kind, Is.EqualTo(AddonKind.Theme));

            var themes = manager.GetInstalledThemes();
            Assert.That(themes, Has.Some.Matches<AddonManifest>(a => a.Id == "Test_Theme"));
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch
            {
            }
        }
    }

    private static void CreateZip(string path, (string Path, string Content)[] entries)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            var e = zip.CreateEntry(entry.Path, CompressionLevel.NoCompression);
            using var writer = new StreamWriter(e.Open());
            writer.Write(entry.Content);
        }
    }
}
