using System;

namespace Playnite.Addons.OutOfProc;

internal sealed class OutOfProcAddonDescriptor
{
    public OutOfProcAddonDescriptor(AddonManifest manifest, string fileName, string arguments, string workingDirectory)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        FileName = fileName ?? string.Empty;
        Arguments = arguments ?? string.Empty;
        WorkingDirectory = workingDirectory ?? string.Empty;
    }

    public AddonManifest Manifest { get; }
    public string FileName { get; }
    public string Arguments { get; }
    public string WorkingDirectory { get; }
}

