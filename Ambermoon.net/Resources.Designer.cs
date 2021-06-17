using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;

namespace Ambermoon
{
    internal class Resources
    {        
        static ResourceManager resourceManager;

        [SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources()
        {
        }
        
        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager => resourceManager ??= new ResourceManager("Ambermoon.Resources", typeof(Resources).Assembly);
        
        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get;
            set;
        }
        
        internal static System.Drawing.Icon App => (System.Drawing.Icon)ResourceManager.GetObject("app", Culture);
        internal static byte[] IntroFont => (byte[])ResourceManager.GetObject("IntroFont", Culture);
        internal static System.Drawing.Bitmap WindowIcon => (System.Drawing.Bitmap)ResourceManager.GetObject("windowIcon", Culture);
    }
}
