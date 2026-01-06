using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("19bc9097-5705-4352-90e2-99f0c63230d0")]

// InternalsVisibleTo for testing and application projects
[assembly: InternalsVisibleTo("Playnite.DesktopApp")]
[assembly: InternalsVisibleTo("Playnite.FullscreenApp")]
[assembly: InternalsVisibleTo("Playnite.Tests")]
[assembly: InternalsVisibleTo("Playnite.DesktopApp.Tests")]
[assembly: InternalsVisibleTo("Playnite.FullscreenApp.Tests")]
[assembly: InternalsVisibleTo("Playnite")]

// XAML namespace definitions for WPF
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.SDK.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.SDK.Controls")]
