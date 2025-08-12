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

        internal static byte[] WindowIcon => (byte[])ResourceManager.GetObject("windowIcon", Culture);

        internal static byte[] Logo => (byte[])ResourceManager.GetObject("logo", Culture);

        internal static byte[] Song => (byte[])ResourceManager.GetObject("song", Culture);

        internal static byte[] Advanced => (byte[])ResourceManager.GetObject("advanced", Culture);

        internal static byte[] Borders256 => (byte[])ResourceManager.GetObject("borders256", Culture);

        internal static byte[] Flags => (byte[])ResourceManager.GetObject("flags", Culture);

        internal static byte[] LoadingBarLeft => (byte[])ResourceManager.GetObject("lbar_left", Culture);

        internal static byte[] LoadingBarRight => (byte[])ResourceManager.GetObject("lbar_right", Culture);

        internal static byte[] LoadingBarMid => (byte[])ResourceManager.GetObject("lbar_mid", Culture);

        internal static byte[] LoadingBarRed => (byte[])ResourceManager.GetObject("lbar_red", Culture);

        internal static byte[] LoadingBarYellow => (byte[])ResourceManager.GetObject("lbar_yellow", Culture);

        internal static byte[] LoadingBarGreen => (byte[])ResourceManager.GetObject("lbar_green", Culture);
    }
}
