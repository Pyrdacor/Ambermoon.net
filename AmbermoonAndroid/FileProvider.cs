using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Android.Graphics;

namespace AmbermoonAndroid
{
    internal static class FileProvider
    {
        static Activity activity = null;

        public static void Initialize(Activity activity)
        {
            FileProvider.activity = activity;
        }

        static Stream LoadStream(int id)
        {
            var stream = new MemoryStream();
            using var dataStream = activity.ApplicationContext.Resources.OpenRawResource(id);
            dataStream.CopyTo(stream);
            stream.Position = 0;
            return stream;
        }

        static byte[] LoadData(int id)
        {
            using var stream = LoadStream(id);
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return data;
        }

        public static Stream GetVersions() => LoadStream(Resource.Raw.versions);

        public static Stream GetLogoData() => LoadStream(Resource.Raw.logo);

        public static byte[] GetIntroFontData() => LoadData(Resource.Raw.IntroFont);

		public static byte[] GetIngameFontData() => LoadData(Resource.Raw.IngameFont);

		public static byte[] GetAdvancedLogoData() => LoadData(Resource.Raw.advanced);

		public static byte[] GetBorderData() => LoadData(Resource.Raw.borders256);

		public static byte[] GetFlagsData() => LoadData(Resource.Raw.flags);

		public static Stream GetAdvancedDiffsData() => LoadStream(Resource.Raw.diffs);

		public static byte[] GetWindowIcon() => LoadData(Resource.Raw.windowIcon);

        public static Graphic GetFinger() => GetGraphic(Resource.Raw.finger);

		public static Graphic GetTapIndicator() => GetGraphic(Resource.Raw.tapIndicator);

		public static Graphic GetHoldIndicator() => GetGraphic(Resource.Raw.holdIndicator);

		public static Graphic GetClock() => GetGraphic(Resource.Raw.clock);

		public static Graphic GetDonateButton() => GetGraphic(Resource.Raw.donate);

		public static Stream GetMusic() => LoadStream(Resource.Raw.music);

        public static byte[] GetQuestLogIcon() => LoadData(Resource.Raw.QuestLog);

        public static Graphic GetMidButton()
		{
			var imageData = LoadData(Resource.Raw.mobile_mid_button);
			var graphic = new Graphic()
            {
                Width = 32,
                Height = 13,
                IndexedGraphic = true
            };

            new GraphicReader().ReadGraphic(graphic, new DataReader(imageData), new GraphicInfo
			{
				Width = 32,
				Height = 13,
				PaletteOffset = 24,
				GraphicFormat = GraphicFormat.Palette3Bit,
                Alpha = true,
            });

			return graphic;
		}

        private static Graphic GetGraphic(int id)
		{
			return LoadGraphic(GetBitmap(id));
		}

        private static Bitmap GetBitmap(int id)
		{
			using var stream = LoadStream(id);
			return BitmapFactory.DecodeStream(stream);
		}

		private static Graphic LoadGraphic(Bitmap bitmap)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			int[] intPixels = new int[width * height];

			bitmap.GetPixels(intPixels, 0, width, 0, 0, width, height);

			byte[] rgbaData = new byte[width * height * 4];

			for (int i = 0; i < intPixels.Length; i++)
			{
				int pixel = intPixels[i];
				int index = i * 4;
				rgbaData[index] = (byte)((pixel >> 16) & 0xFF); // R
				rgbaData[index + 1] = (byte)((pixel >> 8) & 0xFF); // G
				rgbaData[index + 2] = (byte)(pixel & 0xFF); // B
				rgbaData[index + 3] = (byte)((pixel >> 24) & 0xFF); // A
			}

			return new Graphic
			{
				Width = width,
				Height = height,
				Data = rgbaData,
				IndexedGraphic = false
			};
		}
	}
}
