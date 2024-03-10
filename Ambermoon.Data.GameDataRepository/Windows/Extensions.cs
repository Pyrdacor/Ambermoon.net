using Ambermoon.Data.GameDataRepository.Data;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

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

		public static Bitmap ToBitmap(this Image image, Palette palette, bool transparency)
		{
			if (transparency)
				palette = palette.WithTransparency();

            var data = new byte[image.Width * image.Height * image.Frames.Count * 4];
            int x = 0;
            int frameRowSize = image.Width * 4;
			int rowSize = image.Width * image.Frames.Count * 4;
			var destination = new Span<byte>(data);

            foreach (var frame in image.Frames)
            {
                ReadOnlySpan<byte> frameData = frame.GetData(palette);

                for (int y = 0; y < image.Height; y++)
                {
					var sourceRow = frameData.Slice(y * frameRowSize, frameRowSize);
					var destRow = destination.Slice(y * rowSize + x, rowSize);
					sourceRow.CopyTo(destRow);
				}

                x += frameRowSize;
            }

			return DataToBitmap(image.Width * image.Frames.Count, image.Height, data);
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
