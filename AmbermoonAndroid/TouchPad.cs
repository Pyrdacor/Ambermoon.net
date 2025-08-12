using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace AmbermoonAndroid
{
    internal class TouchPad
	{
        bool destroyed = false;
        Rect area = new();
		bool active = false;
        int threshold = 0;

        readonly ILayerSprite background;
        readonly ILayerSprite[] arrows = new IAlphaSprite[4];
        readonly ILayerSprite activeMarker;
        readonly ILayerSprite disableOverlay;

        static uint GraphicOffset = 0;
		static readonly Rect RelativeMarkerArea = new(368, 368, 282, 282);
		static readonly Rect[] RelativeArrowAreas =
		[
			new(222, 444, 128, 139),
            new(438, 210, 151, 141),
            new(673, 439, 133, 146),
            new(434, 673, 156, 137),
        ];

        public Direction? Direction { get; private set; } = null;


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

            byte[] oddRowPixels = [0, 0, 0, 0xff, 0, 0, 0, 0];
            byte[] evenRowPixels = [0, 0, 0, 0, 0, 0, 0, 0xff];
            byte[] oddRow = [.. Enumerable.Repeat(oddRowPixels, background.Width / 2).SelectMany(x => x)];
            byte[] evenRow = [.. Enumerable.Repeat(oddRowPixels, background.Width / 2).SelectMany(x => x)];
            var data = new byte[background.Width * background.Height * 4];
            int index = 0;

            for (int i = 0; i < background.Height / 2; i++)
            {
                Buffer.BlockCopy(oddRow, 0, data, index, oddRow.Length);
                index += oddRow.Length;
                Buffer.BlockCopy(evenRow, 0, data, index, evenRow.Length);
                index += evenRow.Length;
            }

            var disableOverlay = new Graphic
            {
                Width = background.Width,
                Height = background.Height,
                IndexedGraphic = false,
                Data = data
            };

            Graphic[] graphics =
			[
                background,
				marker,
				..arrows,
                disableOverlay
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
            threshold = area.Width / 10; // 10% of the width is the threshold for touchpad movement
        }

		public TouchPad(IGameRenderView renderView, Size windowSize)
        {
			Resize(windowSize.Width, windowSize.Height);

			var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Images);
			var layer = renderView.GetLayer(Layer.Images);

            ILayerSprite CreateSprite(int texWidth, int texHeight, int width, int height, byte displayLayer, uint textureIndex)
            {
                var sprite = renderView.SpriteFactory.Create(width, height, true, displayLayer) as ILayerSprite;
                sprite.Visible = false;
                sprite.TextureSize = new(texWidth, texHeight);
                sprite.Layer = layer;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
                // Important for visibility check, otherwise the virtual screen is used!
                sprite.ClipArea = area;

                return sprite;
            }

            // Note: This has to be updated when the device resolution changes. But this is not possible so we should be fine!
            int minDimension = Math.Min(area.Width, area.Height);
			int deviceHeight = Math.Min(windowSize.Width, windowSize.Height);

			if (minDimension > 1024 && minDimension > deviceHeight * 3 / 4)
			{
				minDimension = deviceHeight * 3 / 4;
			}

			double scale = minDimension / 1024.0;

			background = CreateSprite(1024, 1024, minDimension, minDimension, 0, 0);
            background.X = area.X + (area.Width - background.Width) / 2;
            background.Y = area.Y + (area.Height - background.Height) / 2;

            disableOverlay = CreateSprite(1024, 1024, minDimension, minDimension, 100, 6);
            disableOverlay.X = background.X;
            disableOverlay.Y = background.Y;

            var relativeArea = RelativeMarkerArea;
			activeMarker = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scale * relativeArea.Width), Util.Round(scale * relativeArea.Height), 10, 1);
            activeMarker.X = background.X + Util.Round(scale * relativeArea.X);
            activeMarker.Y = background.Y + Util.Round(scale * relativeArea.Y);

            for (int a = 0; a < 4; a++)
			{
                relativeArea = RelativeArrowAreas[a];
				var arrow = arrows[a] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scale * relativeArea.Width), Util.Round(scale * relativeArea.Height), 10, (uint)(2 + a));

				arrow.X = background.X + Util.Round(scale * relativeArea.X);
				arrow.Y = background.Y + Util.Round(scale * relativeArea.Y);
			}

			background.Visible = true;
        }

		public void Show(bool show)
		{
            background.Visible = show;

            if (!show)
			{
				activeMarker.Visible = false;
                disableOverlay.Visible = false;

                foreach (var arrow in arrows)
					arrow.Visible = false;

                active = false;
                Direction = null;
            }
		}

        public void Destroy()
        {
            if (destroyed)
                return;

            background.Delete();
            activeMarker.Delete();
            disableOverlay.Delete();
            foreach (var arrow in arrows)
                arrow.Delete();

            Direction = null;
            active = false;

            destroyed = true;
		}

        public bool OnFingerDown(Game game, Position position)
        {
            if (!game.InputEnable)
            {
                if (active)
                    OnFingerUp(position);

                return false;
            }

            if (active)
                return true;

            if (area.Contains(position))
            {
                active = true;
                activeMarker.Visible = true;
                return true;
            }

            return false;
        }

        public void OnFingerUp(Position position)
        {
			active = false;
            Direction = null;
            activeMarker.Visible = false;
            foreach (var arrow in arrows)
                arrow.Visible = false;
        }

        public bool OnFingerMoveTo(Game game, Position position)
        {
            if (!game.InputEnable)
            {
                if (active)
                    OnFingerUp(position);

                return false;
            }

            if (!active)
                return false;

            var center = new Position(background.X + background.Width / 2, background.Y + background.Height / 2);
            int xdist = position.X - center.X;
            int ydist = position.Y - center.Y;

            bool left = xdist < 0 && -xdist >= threshold;
            bool right = xdist > 0 && xdist >= threshold;
            bool up = ydist < 0 && -ydist >= threshold;
            bool down = ydist > 0 && ydist >= threshold;

            arrows[0].Visible = left;
            arrows[1].Visible = up;
            arrows[2].Visible = right;
            arrows[3].Visible = down;

            if (left)
            {
                if (up)
                    Direction = Ambermoon.Direction.UpLeft;
                else if (down)
                    Direction = Ambermoon.Direction.DownLeft;
                else
                    Direction = Ambermoon.Direction.Left;
            }
            else if (right)
            {
                if (up)
                    Direction = Ambermoon.Direction.UpRight;
                else if (down)
                    Direction = Ambermoon.Direction.DownRight;
                else
                    Direction = Ambermoon.Direction.Right;
            }
            else if (up)
                Direction = Ambermoon.Direction.Up;
            else if (down)
                Direction = Ambermoon.Direction.Down;
            else
                Direction = null;

            return true;
        }

        public void Update(Game game)
        {
            if (disableOverlay != null && background != null)
                disableOverlay.Visible = background.Visible && game.InputEnable;
        }
    }
}
