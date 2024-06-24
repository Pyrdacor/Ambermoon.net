using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace AmbermoonAndroid
{
    internal class TutorialFinger
	{
        bool destroyed = false;
		readonly IAlphaSprite tapFinger;
		readonly IAlphaSprite holdFinger;
		readonly IAlphaSprite clock;
		static uint GraphicOffset = 0;
		const int FingerHeight = 988 - 14 + 160;

		public static Dictionary<uint, Graphic> GetGraphics(uint offset)
		{
			GraphicOffset = offset;
			var finger = FileProvider.GetFinger();
			var tap = FileProvider.GetTapIndicator();
			var hold = FileProvider.GetHoldIndicator();

			var tapFinger = new Graphic
			{
				Width = tap.Width,
				Height = FingerHeight,
				Data = new byte[tap.Width * FingerHeight * 4],
				IndexedGraphic = false
			};

			var holdFinger = new Graphic
			{
				Width = hold.Width,
				Height = FingerHeight,
				Data = new byte[hold.Width * FingerHeight * 4],
				IndexedGraphic = false
			};

			void AddOverlay(Graphic target, Graphic source, int x, int y, byte alpha, bool empty)
			{
				int count = source.Width * 4;
				var sourceLine = new byte[count];
				var targetLine = new byte[count];
				float[] srcColor = new float[4];
				float[] dstColor = new float[4];
				float srcAlpha = alpha / 255.0f;

				for (int sy = 0; sy < source.Height; sy++)
				{
					Buffer.BlockCopy(source.Data, sy * count, sourceLine, 0, count);

					int targetOffset = ((y + sy) * target.Width + x) * 4;

					if (!empty)
					{
						Buffer.BlockCopy(target.Data, targetOffset, targetLine, 0, count);

						for (int i = 0; i < source.Width; i++)
						{
							int index = i * 4;

							for (int c = 0; c < 4; c++)
							{
								srcColor[c] = sourceLine[index] / 255.0f;
								dstColor[c] = targetLine[index++] / 255.0f;
							}

							index -= 4;

							float srcA = srcColor[3] * srcAlpha;
							float dstA = dstColor[3];

							if (srcA < dstA)
								srcA = 0;

							for (int c = 0; c < 3; c++)
								sourceLine[index++] = (byte)Util.Round(255.0f * (srcColor[c] * srcA + dstColor[c] * (1.0f - srcA)));

							sourceLine[index] = (byte)Util.Round(255.0f * Math.Max(srcA, dstA));
						}
					}
					else if (alpha != 255)
					{
						int index = 3;

						for (int i = 0; i < source.Width; i++)
						{
							sourceLine[index] = (byte)Util.Round(srcAlpha * sourceLine[index]);
							index += 4;
						}
					}

					Buffer.BlockCopy(sourceLine, 0, target.Data, targetOffset, count);
				}
			}

			AddOverlay(tapFinger, tap, 0, 0, 224, true);
			AddOverlay(tapFinger, finger, (tap.Width - finger.Width) / 2, 14, 255, false);

			AddOverlay(holdFinger, hold, 0, 0, 128, true);
			AddOverlay(holdFinger, finger, (hold.Width - finger.Width) / 2, 14, 255, false);

			var graphics = new Graphic[]
			{
				tapFinger,
				holdFinger,
				FileProvider.GetClock()
			};
			return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
		}

		public TutorialFinger(IRenderView renderView)
        {
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.MobileOverlays);
			var layer = renderView.GetLayer(Layer.MobileOverlays);

			IAlphaSprite CreateSprite(int texWidth, int texHeight, int width, int height, byte displayLayer, uint textureIndex, byte alpha)
            {
				var sprite = renderView.SpriteFactory.CreateWithAlpha(width, height, displayLayer);
				sprite.Visible = false;
				sprite.TextureSize = new(texWidth, texHeight);
				sprite.Layer = layer;
				sprite.Alpha = alpha;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);

                return sprite;
			}

            tapFinger = CreateSprite(320, FingerHeight, 32, FingerHeight / 10, 70, 0, 255);
			holdFinger = CreateSprite(320, FingerHeight, 32, FingerHeight / 10, 70, 1, 255);
			clock = CreateSprite(320, 320, 32, 32, 70, 2, 224);
		}

		public void DrawFinger(int x, int y, bool longPress)
		{
			if (destroyed)
				return;

			if (x < 0 || y < 0)
			{
				tapFinger.Visible = false;
				holdFinger.Visible = false;
				clock.Visible = false;
			}
			else if (longPress)
			{
				holdFinger.X = x - holdFinger.Width / 2;
				holdFinger.Y = y - 16;
				holdFinger.Visible = true;
				clock.X = holdFinger.X + holdFinger.Width + 8;
				clock.Y = holdFinger.Y;
				clock.Visible = true;
				tapFinger.Visible = false;
			}
			else // tap
			{
				tapFinger.X = x - tapFinger.Width / 2;
				tapFinger.Y = y - 16;
				tapFinger.Visible = true;
				holdFinger.Visible = false;
				clock.Visible = false;
			}
		}

        public void Destroy()
        {
            if (destroyed)
                return;

			tapFinger.Delete();
			holdFinger.Delete();
			clock.Delete();

			destroyed = true;
		}

		public void Clip(Rect area)
		{
			area ??= new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight);
			tapFinger.ClipArea = area;
			holdFinger.ClipArea = area;
			clock.ClipArea = area;
		}
    }
}
