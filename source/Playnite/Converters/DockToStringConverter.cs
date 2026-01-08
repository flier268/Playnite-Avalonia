using Playnite;
using Playnite.SDK;
using Playnite.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Playnite.Converters
{
    public class DockToStringConverter : MarkupExtension, IValueConverter
    {
        public static string GetString(Dock value)
        {
            switch (value)
            {
                case Dock.Left:
                    return ResourceProvider.GetString("LOCDockLeft");
                case Dock.Right:
                    return ResourceProvider.GetString("LOCDockRight");
                case Dock.Top:
                    return ResourceProvider.GetString("LOCDockTop");
                case Dock.Bottom:
                    return ResourceProvider.GetString("LOCDockBottom");
            }

            return "<UknownDockMode>";
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Dock dockValue)
                return GetString(dockValue);

            return "<UknownDockMode>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
