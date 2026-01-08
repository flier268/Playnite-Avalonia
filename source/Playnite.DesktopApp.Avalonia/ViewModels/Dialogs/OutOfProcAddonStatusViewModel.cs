namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class OutOfProcAddonStatusViewModel
{
    public OutOfProcAddonStatusViewModel(string title, string detailsText)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Out-of-proc add-on status" : title;
        DetailsText = detailsText ?? string.Empty;
    }

    public string Title { get; }
    public string DetailsText { get; }
}

