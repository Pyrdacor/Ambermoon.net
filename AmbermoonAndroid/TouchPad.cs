using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace AmbermoonAndroid
{
    internal class TouchPad
	{
        bool destroyed = false;
        Rect area = new();

        readonly IAlphaSprite background;
        readonly IAlphaSprite[] arrows = new IAlphaSprite[4];
        readonly IAlphaSprite activeMarker;
		readonly IColoredRect test;

		static uint GraphicOffset = 0;
		static readonly Rect RelativeMarkerArea = new(368, 368, 282, 282);
		static readonly Rect[] RelativeArrowAreas =
		[
			new(222, 444, 128, 139),
            new(438, 210, 151, 141),
            new(673, 439, 133, 146),
            new(434, 673, 156, 137),
        ];

		public static Dictionary<uint, Graphic> GetGraphics(uint offset)
		{
			GraphicOffset = offset;
			var background = FileProvider.GetTouchPad();
			var marker = FileProvider.GetTouchPadMarker(); // 368,368 (282x282)
			// Left arrow at 222,444 with 128x139
			// Top arrow at 438,210 with 151x141
			// Right arrow at 673,439 with 133x146
			// Bottom arrow at 434,673 with 156x137
			var arrows = FileProvider.GetTouchArrows();

            Graphic[] graphics =
			[
                background,
				marker,
				..arrows
			];

			return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
		}

		public void Resize(int width, int height)
		{
			// Game uses 320x200 (16:10 resolution).

			int deviceHeight = Math.Min(width, height);
            int deviceWidth = Math.Max(width, height);

            double factor = deviceHeight / 200.0;
			int usedWidth = (int)Math.Ceiling(320.0 * factor);

			if (usedWidth >= deviceWidth)
			{
				// TODO: what to do here?
				throw new AmbermoonException(ExceptionScope.Application, "TouchPad: Device width is too small for touchpad.");
            }

            area = new Rect(usedWidth, 0, deviceWidth - usedWidth, deviceHeight);
        }

		public TouchPad(IGameRenderView renderView, Size windowSize)
        {
			Resize(windowSize.Width, windowSize.Height);

			var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Images);
			var layer = renderView.GetLayer(Layer.Images);

            IAlphaSprite CreateSprite(int texWidth, int texHeight, int width, int height, byte displayLayer, uint textureIndex, byte alpha)
            {
                var sprite = renderView.SpriteFactory.CreateWithAlpha(width, height, displayLayer);
                sprite.Visible = false;
                sprite.TextureSize = new(texWidth, texHeight);
                sprite.Layer = layer;
                sprite.Alpha = alpha;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
                // Important for visibility check, otherwise the virtual screen is used!
                sprite.ClipArea = area;

                return sprite;
            }

			test = renderView.ColoredRectFactory.Create(200, 200, Color.FireOverlay, 190);
			test.Layer = layer;
			test.ClipArea = area;
			test.X = area.X + 10;
			test.Y = area.Y + 10;
			test.Visible = true;

            // Note: This has to be updated when the device resolution changes. But this is not possible so we should be fine!
            int minDimension = Math.Min(area.Width, area.Height);
			int deviceHeight = Math.Min(windowSize.Width, windowSize.Height);

			if (minDimension > 1024 && minDimension > deviceHeight * 3 / 4)
			{
				minDimension = deviceHeight * 3 / 4;
			}

			double scale = minDimension / 1024.0;

			background = CreateSprite(1024, 1024, minDimension, minDimension, 0, 0, 255);
            background.X = area.X + (area.Width - background.Width) / 2;
            background.Y = area.Y + (area.Height - background.Height) / 2;

            var relativeArea = RelativeMarkerArea;
			activeMarker = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scale * relativeArea.Width), Util.Round(scale * relativeArea.Height), 10, 1, 255);
            activeMarker.X = background.X + Util.Round(scale * relativeArea.X);
            activeMarker.Y = background.Y + Util.Round(scale * relativeArea.Y);

            for (int a = 0; a < 4; a++)
			{
                relativeArea = RelativeArrowAreas[a];
				var arrow = arrows[a] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scale * relativeArea.Width), Util.Round(scale * relativeArea.Height), 10, (uint)(2 + a), 255);

				arrow.X = background.X + Util.Round(scale * relativeArea.X);
				arrow.Y = background.Y + Util.Round(scale * relativeArea.Y);
			}

			//background.Visible = true;
			//activeMarker.Visible = true; // TODO: remove
        }

        public void Destroy()
        {
            if (destroyed)
                return;

            background.Delete();
            activeMarker.Delete();

			destroyed = true;
		}
    }
}
