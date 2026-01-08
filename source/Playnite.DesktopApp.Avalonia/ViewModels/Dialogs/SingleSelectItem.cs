namespace Playnite.DesktopApp.Avalonia.ViewModels.Dialogs;

public sealed class SingleSelectItem
{
    public SingleSelectItem(string primaryText, string secondaryText, object value)
    {
        PrimaryText = primaryText ?? string.Empty;
        SecondaryText = secondaryText ?? string.Empty;
        Value = value;
    }

    public string PrimaryText { get; }
    public string SecondaryText { get; }
    public object Value { get; }
}

