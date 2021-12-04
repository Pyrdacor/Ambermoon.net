using AmbermoonAndroid;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Ambermoon
{
    internal partial class Resources
    {
        static ResourceManager resourceManager;

        internal Resources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
            => resourceManager ??= new ResourceManager("Ambermoon.Resources", typeof(Resources).Assembly);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture { get; set; }

        internal static byte[] IntroFont => (byte[])ResourceManager.GetObject("IntroFont", Culture);

        internal static byte[] WindowIcon => (byte[])ResourceManager.GetObject("windowIcon", Culture);

        internal static Stream Logo => FileProvider.GetLogoData();

        internal static byte[] Song => FileProvider.GetSongData();

        internal static byte[] Versions => (byte[])ResourceManager.GetObject("versions", Culture);
    }
}
