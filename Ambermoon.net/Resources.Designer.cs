using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Ambermoon
{
    internal class Resources
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

        internal static byte[] IngameFont => (byte[])ResourceManager.GetObject("IngameFont", Culture);

        internal static byte[] IntroFont => (byte[])ResourceManager.GetObject("IntroFont", Culture);

        internal static byte[] WindowIcon => (byte[])ResourceManager.GetObject("windowIcon", Culture);

        internal static byte[] Logo => (byte[])ResourceManager.GetObject("logo", Culture);

        internal static byte[] Song => (byte[])ResourceManager.GetObject("song", Culture);

        internal static byte[] Advanced => (byte[])ResourceManager.GetObject("advanced", Culture);

        internal static byte[] Borders256 => (byte[])ResourceManager.GetObject("borders256", Culture);

        internal static byte[] Flags => (byte[])ResourceManager.GetObject("flags", Culture);
    }
}
