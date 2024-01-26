using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

#if OS_WINDOWS

#pragma warning disable CA1416

namespace Ambermoon.Data.GameDataRepository.Windows
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WindowsExtensions
    {
        public static Bitmap ToBitmap(this ImageData imageData, Palette palette, bool transparency)
        {
            if (transparency)
                palette = palette.WithTransparency();
            var data = imageData.GetData(palette);
            var bitmap = new Bitmap(imageData.Width, imageData.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageData.Width, imageData.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);

            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        public static ImageData ToImageData(this Bitmap bitmap, Palette palette)
        {
            var data = new byte[bitmap.Width * bitmap.Height * 4];
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);

            bitmap.UnlockBits(bitmapData);

            return new ImageData(bitmap.Width, bitmap.Height, data, palette);
        }
    }
}

#pragma warning restore CA1416

#endif // OS_WINDOWS
