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

        public static Graphic GetTouchPad() => GetGraphic(Resource.Raw.touchpad);

        public static Graphic GetTouchPadIconBackground() => GetGraphic(Resource.Raw.icon_background);

        public static Graphic GetTouchPadEye() => GetSpecialGraphic(Resource.Raw.icon_eye);

        public static Graphic GetTouchPadHand() => GetSpecialGraphic(Resource.Raw.icon_hand);

        public static Graphic GetTouchPadMouth() => GetSpecialGraphic(Resource.Raw.icon_mouth);

        public static Graphic GetTouchPadTransport() => GetSpecialGraphic(Resource.Raw.icon_transport);

        public static Graphic GetTouchPadMap() => GetSpecialGraphic(Resource.Raw.icon_map);

        public static Graphic GetTouchPadCamp() => GetSpecialGraphic(Resource.Raw.icon_camp);

        public static Graphic GetTouchPadMagic() => GetSpecialGraphic(Resource.Raw.icon_magic);

        public static Graphic GetTouchPadWait() => GetSpecialGraphic(Resource.Raw.icon_wait);

        public static Graphic GetTouchPadBattlePositions() => GetSpecialGraphic(Resource.Raw.icon_battle_positions);

        public static Graphic GetTouchPadOptions() => GetSpecialGraphic(Resource.Raw.icon_options);

        public static Graphic GetTouchPadSwitch() => GetSpecialGraphic(Resource.Raw.icon_switch);

        public static Graphic GetTouchPadMarker() => GetGraphic(Resource.Raw.touchpad_marker);

        public static Graphic[] GetTouchArrows() => [ GetGraphic(Resource.Raw.touchpad_top), GetGraphic(Resource.Raw.touchpad_right), GetGraphic(Resource.Raw.touchpad_bottom), GetGraphic(Resource.Raw.touchpad_left) ];

        public static Graphic GetLoadingBarLeft() => GetSpecialGraphic(Resource.Raw.lbar_left);
        public static Graphic GetLoadingBarMid() => GetSpecialGraphic(Resource.Raw.lbar_mid);
        public static Graphic GetLoadingBarRight() => GetSpecialGraphic(Resource.Raw.lbar_right);
        public static Graphic GetLoadingBarFill() => GetSpecialGraphic(Resource.Raw.lbar_green);

        private static Graphic GetGraphic(int id)
		{
			return LoadGraphic(GetBitmap(id));
		}

        private static Bitmap GetBitmap(int id)
		{
			using var stream = LoadStream(id);
			return BitmapFactory.DecodeStream(stream);
		}

        private static Graphic GetSpecialGraphic(int id)
        {
            return LoadSpecialGraphic(LoadData(id));
        }

        private static Graphic LoadSpecialGraphic(byte[] imageData)
		{
            var dataReader = new DataReader(imageData);
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();
            int numColors = dataReader.ReadByte();

            byte[] colors = new byte[numColors * 3];

            for (int i = 0; i < numColors; i++)
            {
                colors[i * 3 + 0] = dataReader.ReadByte();
                colors[i * 3 + 1] = dataReader.ReadByte();
                colors[i * 3 + 2] = dataReader.ReadByte();
            }

            int chunkSize = width * height;
            byte[] data = new byte[chunkSize * 4];

            for (int i = 0; i < chunkSize; ++i)
            {
                int index = dataReader.ReadByte();

                data[i * 4 + 0] = colors[index * 3 + 0];
                data[i * 4 + 1] = colors[index * 3 + 1];
                data[i * 4 + 2] = colors[index * 3 + 2];
                data[i * 4 + 3] = 0xff;
            }

            return new Graphic
            {
                Width = width,
                Height = height,
                Data = data,
                IndexedGraphic = false
            };
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
