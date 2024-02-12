using Ambermoon.Data.GameDataRepository.Data;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

#if OS_WINDOWS

#pragma warning disable CA1416

namespace Ambermoon.Data.GameDataRepository.Windows
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WindowsExtensions
    {
        private static Bitmap DataToBitmap(int width, int height, byte[] imageData)
        {
            var bitmap = new Bitmap(width, height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);

            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        public static Bitmap ToBitmap(this ImageData imageData, Palette palette, bool transparency)
        {
            if (transparency)
                palette = palette.WithTransparency();
            var data = imageData.GetData(palette);

            return DataToBitmap(imageData.Width, imageData.Height, data);
        }

        public static Bitmap GetBitmap(this GameDataRepository repository, IImageProvidingData imageProvidingData)
        {
            var imageData = repository.GetImage(imageProvidingData, out int width, out int height);

            return DataToBitmap(width, height, imageData);
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

        public static Color ToColor(this Palette palette, uint colorIndex)
        {
            return Color.FromArgb(unchecked((int)palette.GetColor(colorIndex)));
        }

        public static Color[] ToColors(this Palette palette)
        {
            return palette.GetColors().Select(color => Color.FromArgb(unchecked((int)color))).ToArray();
        }
    }
}

#pragma warning restore CA1416

#endif // OS_WINDOWS
