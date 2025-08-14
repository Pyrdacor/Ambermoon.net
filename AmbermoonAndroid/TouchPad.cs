using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace AmbermoonAndroid;


internal class TouchPad
{
    bool destroyed = false;
    Rect area = new();
    bool active = false;
    int threshold = 0;
    int iconHitRadius = 0;
    int arrowHitRadius = 0;
    bool arrowClicked = false;
    bool resetDirection = false;
    const int DisableOverlayDimension = 128;

    readonly ILayerSprite background;
    readonly ILayerSprite[] arrows = new ILayerSprite[4];
    readonly ILayerSprite activeMarker;
    readonly ILayerSprite[] disableOverlays = new ILayerSprite[3];
    readonly ILayerSprite[] iconBackgrounds = new ILayerSprite[4];
    readonly ILayerSprite[] icons = new ILayerSprite[4];

    static uint GraphicOffset = 0;
    static readonly Rect RelativeMarkerArea = new(368, 368, 282, 282);
    static readonly Rect[] RelativeArrowAreas =
    [
        new(438, 210, 151, 141),
        new(673, 439, 133, 146),
        new(434, 673, 156, 137),
        new(222, 444, 128, 139),
    ];
    static readonly Rect[] RelativeIconAreas =
    [
        new(57, 1048, 270, 276),
        new(57 + 270 + 50, 1048, 270, 276),
        new(57 + 270 + 50 + 270 + 50, 1048, 270, 276),
        new((1024 - 270)/2, -(276 + 24), 270, 276),
    ];
    static readonly Size[] IconSizes =
    [
        new(24, 9), // eye
        new(22, 11), // hand
        new(23, 9), // mouth
        new(13, 11), // options
    ];

    public Direction? Direction { get; private set; } = null;

    public static Dictionary<uint, Graphic> GetGraphics(uint offset)
    {
        try
        {
            GraphicOffset = offset;
            var background = FileProvider.GetTouchPad();
            var marker = FileProvider.GetTouchPadMarker(); // 368,368 (282x282)
            // Top arrow at 438,210 with 151x141
            // Right arrow at 673,439 with 133x146
            // Bottom arrow at 434,673 with 156x137
            // Left arrow at 222,444 with 128x139
            var arrows = FileProvider.GetTouchArrows();
            var iconBackground = FileProvider.GetTouchPadIconBackground();
            var eye = FileProvider.GetTouchPadEye();
            var hand = FileProvider.GetTouchPadHand();
            var mouth = FileProvider.GetTouchPadMouth();
            var options = FileProvider.GetTouchPadOptions();

            byte[] oddRowPixels = [0, 0, 0, 0xff, 0, 0, 0, 0];
            byte[] evenRowPixels = [0, 0, 0, 0, 0, 0, 0, 0xff];
            byte[] oddRow = [.. Enumerable.Repeat(oddRowPixels, DisableOverlayDimension / 2).SelectMany(x => x)];
            byte[] evenRow = [.. Enumerable.Repeat(evenRowPixels, DisableOverlayDimension / 2).SelectMany(x => x)];
            var data = new byte[DisableOverlayDimension * DisableOverlayDimension * 4];
            int index = 0;

            for (int i = 0; i < DisableOverlayDimension / 2; i++)
            {
                Buffer.BlockCopy(oddRow, 0, data, index, oddRow.Length);
                index += oddRow.Length;
                Buffer.BlockCopy(evenRow, 0, data, index, evenRow.Length);
                index += evenRow.Length;
            }

            var disableOverlay = new Graphic
            {
                Width = DisableOverlayDimension,
                Height = DisableOverlayDimension,
                IndexedGraphic = false,
                Data = data
            };

            Graphic[] graphics =
            [
                background,
                marker,
                ..arrows,
                disableOverlay,
                iconBackground,
                eye,
                hand,
                mouth,
                options
            ];

            return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
        }
        catch (Exception ex)
        {
            throw;
        }
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
        try
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

            disableOverlays[0] = CreateSprite(DisableOverlayDimension, DisableOverlayDimension, minDimension, minDimension, 100, 6);
            disableOverlays[0].X = background.X;
            disableOverlays[0].Y = background.Y - background.Height;
            disableOverlays[1] = CreateSprite(DisableOverlayDimension, DisableOverlayDimension, minDimension, minDimension, 100, 6);
            disableOverlays[1].X = background.X;
            disableOverlays[1].Y = background.Y;
            disableOverlays[2] = CreateSprite(DisableOverlayDimension, DisableOverlayDimension, minDimension, minDimension, 100, 6);
            disableOverlays[2].X = background.X;
            disableOverlays[2].Y = background.Y + background.Height;

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

            arrowHitRadius = (5 * arrows.SelectMany<ILayerSprite, int>(arrow => [arrow.Width, arrow.Height]).Max() / 4) / 2;

            for (int i = 0; i < 4; i++)
            {
                relativeArea = RelativeIconAreas[i];
                var iconBackground = iconBackgrounds[i] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scale * relativeArea.Width), Util.Round(scale * relativeArea.Height), 20, 7);

                var iconSize = IconSizes[i];
                double iconWidth = i == 3 ? 192.0 : 256.0;
                double iconScale = iconWidth / iconSize.Width;
                var iconHeight = iconScale * iconSize.Height;
                var icon = icons[i] = CreateSprite(iconSize.Width, iconSize.Height, Util.Round(scale * iconWidth), Util.Round(scale * iconHeight), 30, (uint)(8 + i));

                iconBackground.X = background.X + Util.Round(scale * relativeArea.X);
                iconBackground.Y = background.Y + Util.Round(scale * relativeArea.Y);

                icon.X = iconBackground.X + (iconBackground.Width - icon.Width) / 2;
                icon.Y = iconBackground.Y + (iconBackground.Height - icon.Height) / 2;
            }

            iconHitRadius = Math.Max(iconBackgrounds[0].Width, iconBackgrounds[0].Height) / 2 + 1;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void Show(bool show)
    {
        background.Visible = show;

        for (int i = 0; i < 4; i++)
        {
            iconBackgrounds[i].Visible = show;
            icons[i].Visible = show;
        }

        if (!show)
        {
            activeMarker.Visible = false;
            
            foreach (var disableOverlay in disableOverlays)
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
        foreach (var disableOverlay in disableOverlays)
            disableOverlay.Delete();

        foreach (var arrow in arrows)
            arrow.Delete();

        for (int i = 0; i < iconBackgrounds.Length; i++)
        {
            iconBackgrounds[i].Delete();
            icons[i].Delete();
        }

        Direction = null;
        active = false;

        destroyed = true;
    }

    public bool OnTap(Game game, Position position)
    {
        if (!game.InputEnable)
            return false;

        if (area.Contains(position))
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                var arrow = arrows[i];
                var arrowArea = new Rect(arrow.X + arrow.Width / 2 - arrowHitRadius, arrow.Y + arrow.Height / 2 - arrowHitRadius, 2 * arrowHitRadius, 2 * arrowHitRadius);

                if (arrowArea.Contains(position))
                {
                    Direction = (Direction)(i * 2);
                    arrowClicked = true;
                    arrow.Visible = true;
                    activeMarker.Visible = false;
                    active = false;
                    return true;
                }
            }

            for (int i = 0; i < iconBackgrounds.Length; i++)
            {
                var icon = iconBackgrounds[i];
                var iconArea = new Rect(icon.X + icon.Width / 2 - iconHitRadius, icon.Y + icon.Height / 2 - iconHitRadius, 2 * iconHitRadius, 2 * iconHitRadius);

                if (iconArea.Contains(position))
                {
                    activeMarker.Visible = false;
                    active = false;
                    game.TriggerMobileIconAction((Game.MobileIconAction)i);
                    return true;
                }
            }
        }

        return false;
    }

    public bool OnLongPress(Game game, Position position)
    {
        if (!game.InputEnable)
        {
            if (active)
                OnFingerUp(game, position);

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

    public void OnFingerUp(Game game, Position position)
    {
        active = false;
        activeMarker.Visible = false;

        if (arrowClicked)
        {
            arrowClicked = false;
            resetDirection = true;
        }
        else
        {
            foreach (var arrow in arrows)
                arrow.Visible = false;
            Direction = null;
        }
    }

    public bool OnFingerMoveTo(Game game, Position position)
    {
        if (!game.InputEnable)
        {
            if (active)
                OnFingerUp(game, position);

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

        arrows[0].Visible = up;
        arrows[1].Visible = right;
        arrows[2].Visible = down;
        arrows[3].Visible = left;

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
        if (background != null)
        {
            bool showDisableOverlays = background.Visible && !game.InputEnable;

            foreach (var disableOverlay in disableOverlays)
            {
                if (disableOverlay != null)
                    disableOverlay.Visible = showDisableOverlays;
            }
        }

        if (arrowClicked)
        {
            arrowClicked = false;
            resetDirection = true;
        }
        else if (resetDirection)
        {
            resetDirection = false;

            if (!active)
            {
                foreach (var arrow in arrows)
                    arrow.Visible = false;
                Direction = null;
            }
        }
    }
}
