using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace AmbermoonAndroid
{
    internal class MovementIndicator
	{
        bool destroyed = false;
		bool active = false;
		readonly bool[] currentDirections = new bool[4];
		readonly IAlphaSprite background;
		readonly IAlphaSprite activeIndicator;
		readonly IAlphaSprite[] arrows = new IAlphaSprite[4];
		static readonly Position activeIndicatorOffset = new(26, 27);
		static readonly Position[] arrowOffsets = new Position[4]
		{
			new(26, 15),
			new(38, 27),
			new(26, 38),
			new(14, 27),
		};
		Rect Area { get; set; } = new();
		public event Action<int, int> MoveRequested;
		static uint GraphicOffset = 0;

		public static Dictionary<uint, Graphic> GetGraphics(uint offset)
		{
			GraphicOffset = offset;
			var graphics = new Graphic[]
			{
				FileProvider.GetMoveIndicatorArrow(CharacterDirection.Up),
				FileProvider.GetMoveIndicatorArrow(CharacterDirection.Right),
				FileProvider.GetMoveIndicatorArrow(CharacterDirection.Down),
				FileProvider.GetMoveIndicatorArrow(CharacterDirection.Left),
				FileProvider.GetMoveIndicatorBackground(),
				FileProvider.GetMoveIndicatorActive()
			};
			return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
		}

		public MovementIndicator(IRenderView renderView)
        {
			var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.MobileOverlays);
			var layer = renderView.GetLayer(Layer.MobileOverlays);

			IAlphaSprite CreateSprite(int x, int y, int width, int height, byte displayLayer, uint textureIndex, byte alpha)
            {
				var sprite = renderView.SpriteFactory.CreateWithAlpha(width, height, displayLayer);
				sprite.Visible = false;
				sprite.Layer = layer;
                sprite.X = x;
                sprite.Y = y;
				sprite.Alpha = alpha;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);

                return sprite;
			}

            int displayX = Global.Map2DViewX + Global.Map2DViewWidth - 64;
            int displayY = Global.Map2DViewY + Global.Map2DViewHeight - 64;
			background = CreateSprite(displayX, displayY, 64, 64, 50, 4, 160);
            activeIndicator = CreateSprite(displayX + activeIndicatorOffset.X, displayY + activeIndicatorOffset.Y, 12, 10, 55, 5, 224);

            for (uint i = 0; i < 4; i++)
            {
				var offset = arrowOffsets[i];
				arrows[i] = CreateSprite(displayX + offset.X, displayY + offset.Y, 12, 11, 55, i, 224);
			}

			Area = new Rect(displayX, displayY, 64, 64);
		}

		public bool LongPress(Position position)
		{
			if (!Area.Contains(position))
				return false;

			active = true;

			return true;
		}

		public void FingerUp()
		{
			active = false;
		}

		public bool FingerMoveTo(Position position)
		{
			if (!active)
				return false;

			var center = Area.Center;

			const int Threshold = 32;
			int dx = position.X - center.X;
			int dy = position.Y - center.Y;

			if (Math.Abs(dx) < Threshold)
				dx = 0;
			else
				dx = Math.Sign(dx);
			if (Math.Abs(dy) < Threshold)
				dy = 0;
			else
				dy = Math.Sign(dy);

			if (dx != 0 || dy != 0)
				MoveRequested?.Invoke(dx, dy);

			return true;
		}

        public void Update(Rect mapViewArea, bool enabled)
        {
			if (destroyed)
				return;

			if (enabled)
				UpdatePosition(mapViewArea.Right - 64, mapViewArea.Bottom - 64);
			else
				active = false;

			background.Visible = enabled;
			activeIndicator.Visible = enabled && active;

            for (int i = 0; i < 4; i++)
            {
                arrows[i].Visible = enabled && active && currentDirections[i];
            }
		}

		private void UpdatePosition(int x, int y)
		{
			if (background.X == x && background.Y == y)
				return;

			background.X = x;
			background.Y = y;

			activeIndicator.X = x + activeIndicatorOffset.X;
			activeIndicator.Y = y + activeIndicatorOffset.Y;

			for (int i = 0; i < arrowOffsets.Length; i++)
			{
				arrows[i].X = x + arrowOffsets[i].X;
				arrows[i].Y = y + arrowOffsets[i].Y;
			}

			Area = new Ambermoon.Rect(x, y, 64, 64);
		}

        public void Destroy()
        {
            if (destroyed)
                return;

			background.Delete();
            activeIndicator.Delete();

			for (int i = 0; i < 4; i++)
			{
                arrows[i].Delete();
			}

			destroyed = true;
		}
    }
}
