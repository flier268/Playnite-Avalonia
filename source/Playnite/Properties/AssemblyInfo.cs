using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// WPF Theme Info - CRITICAL for WPF resource loading
[assembly: System.Windows.ThemeInfo(System.Windows.ResourceDictionaryLocation.None, System.Windows.ResourceDictionaryLocation.SourceAssembly)]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ad271e73-8a13-4c4e-bfdc-3076646b59e3")]

// InternalsVisibleTo for testing
[assembly: InternalsVisibleTo("Playnite.Tests")]

// XAML namespace definitions for WPF
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.Controls")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.Commands")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.Behaviors")]
