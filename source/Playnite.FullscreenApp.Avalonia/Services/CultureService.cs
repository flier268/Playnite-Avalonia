using System.Globalization;

namespace Playnite.FullscreenApp.Avalonia.Services;

public static class CultureService
{
    public static void Apply(string cultureName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return;
            }

            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
        }
    }
}

