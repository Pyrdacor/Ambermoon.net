using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Ambermoon.PrepareIcons
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            var bitmap = (Bitmap)Image.FromFile(args[0]);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                int numPixels = bitmap.Width * bitmap.Height;
                var buffer = new byte[numPixels * 4];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                fixed (byte* bptr = &buffer[0])
                {
                    uint* ptr = (uint*)bptr;
                    for (int i = 0; i < numPixels; ++i)
                    {
                        // BGRA -> RGBA
                        // Actually in little endian the result format is 0xAABBGGRR (RGBA).
                        // The source format on the other hand is 0xAARRGGBB (BGRA).
                        uint blue = (*ptr & 0x000000ff) << 16;
                        uint red = (*ptr & 0x00ff0000) >> 16;
                        *ptr = (*ptr & 0xff00ff00) | red | blue;
                        ++ptr;
                    }
                }

                using var output = File.Create(args[1]);

                output.Write(buffer);
            }
            finally
            {
                bitmap.UnlockBits(data);
                bitmap.Dispose();
            }
        }
    }
}
