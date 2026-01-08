using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playnite.SDK
{
    /// <summary>
    /// Represents SDK version properties.
    /// </summary>
    public static class SdkVersions
    {
        /// <summary>
        /// Gets SDK version.
        /// </summary>
        public static System.Version SDKVersion
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("PLAYNITE_SDK_VERSION");
                return Version.TryParse(env, out var version) ? version : new Version(0, 0);
            }
        }
    }
}
