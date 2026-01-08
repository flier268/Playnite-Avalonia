using System.Collections.Generic;

namespace Playnite.Configuration
{
    public enum UpdateChannel
    {
        Stable = 0,
        Nightly = 1
    }

    public enum AfterLaunchOption
    {
        None = 0,
        Minimize = 1,
        Close = 2
    }

    public enum AfterGameCloseOption
    {
        None = 0,
        Restore = 1,
        RestoreOnlyFromUI = 2,
        Exit = 3
    }

    public enum AppTheme
    {
        Default = 0,
        Light = 1,
        Dark = 2
    }

    public sealed class AppSettings
    {
        public string LibraryDbPath { get; set; } = string.Empty;

        public AppTheme Theme { get; set; } = AppTheme.Default;

        public string Language { get; set; } = string.Empty;

        public AfterLaunchOption AfterLaunch { get; set; } = AfterLaunchOption.Minimize;
        public AfterGameCloseOption AfterGameClose { get; set; } = AfterGameCloseOption.Restore;

        public bool DownloadMetadataOnImport { get; set; } = true;
        public Playnite.SDK.PlaytimeImportMode PlaytimeImportMode { get; set; } = Playnite.SDK.PlaytimeImportMode.NewImportsOnly;

        public bool ScanLibInstallSizeOnLibUpdate { get; set; } = false;
        public bool FuzzyMatchingInNameFilter { get; set; } = true;

        public bool EnableTray { get; set; } = true;
        public bool MinimizeToTray { get; set; } = false;
        public bool CloseToTray { get; set; } = false;

        public bool StartInFullscreen { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool StartOnBoot { get; set; } = false;

        public bool UpdateLibStartup { get; set; } = true;
        public bool DisableHwAcceleration { get; set; } = false;

        public bool CheckForUpdatesOnStartup { get; set; } = true;
        public bool NotifyOnUpdates { get; set; } = true;
        public bool AutoDownloadUpdates { get; set; } = false;
        public UpdateChannel UpdateChannel { get; set; } = UpdateChannel.Stable;

        public List<string> DisabledAddons { get; set; } = new List<string>();
        public string AddonsBrowsePath { get; set; } = string.Empty;
    }
}
