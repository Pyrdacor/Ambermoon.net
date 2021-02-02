using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;

namespace Ambermoon
{
    internal class Resources
    {        
        static System.Resources.ResourceManager resourceManager;
        
        [SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources()
        {
        }
        
        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceManager, null))
                    resourceManager = new ResourceManager("Ambermoon.Resources", typeof(Resources).Assembly);
                return resourceManager;
            }
        }
        
        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get;
            set;
        }
        
        internal static byte[] IntroFont => (byte[])ResourceManager.GetObject("IntroFont", Culture);
    }
}
