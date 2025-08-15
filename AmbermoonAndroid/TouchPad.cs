using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;
using Android.OS;

namespace AmbermoonAndroid;


internal class TouchPad
{
    bool destroyed = false;
    Rect area = new();
    bool active = false;
    int threshold = 0;
    int iconHitRadius = 0;
    int arrowHitRadius = 0;
    const int DisableOverlayDimension = 128;
    bool arrowClicked = false;
    Handler hideTappedArrowHandler = new(Looper.MainLooper);
    const int ShowTappedArrowDuration = 200;
    bool enabled = false;

    readonly ILayerSprite background;
    readonly ILayerSprite[] arrows = new ILayerSprite[4];
    readonly ILayerSprite activeMarker;
    readonly ILayerSprite disableOverlay;
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
        new(22, 12), // hand
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

    public TouchPad(IGameRenderView renderView, Rect touchPadArea)
    {
        try
        {
            area = new(touchPadArea);
            threshold = area.Width / 10; // 10% of the width is the threshold for touchpad movement

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

            double scaleX = area.Width / 1526.0f;
            double scaleY = area.Height / 994.0f;

            //background = CreateSprite(1024, 1024, minDimension, minDimension, 0, 0);
            background = CreateSprite(1526, 994, area.Width, area.Height, 0, 0);
            background.X = area.X;// + (area.Width - background.Width) / 2;
            background.Y = area.Y;// + (area.Height - background.Height) / 2;

            disableOverlay = CreateSprite(DisableOverlayDimension, DisableOverlayDimension, area.Width, area.Height, 100, 6);
            disableOverlay.X = background.X;
            disableOverlay.Y = background.Y;

            var relativeArea = RelativeMarkerArea;
            activeMarker = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 10, 1);
            activeMarker.X = background.X + Util.Round(scaleX * relativeArea.X);
            activeMarker.Y = background.Y + Util.Round(scaleY * relativeArea.Y);

            for (int a = 0; a < 4; a++)
            {
                relativeArea = RelativeArrowAreas[a];
                var arrow = arrows[a] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 10, (uint)(2 + a));

                arrow.X = background.X + Util.Round(scaleX * relativeArea.X);
                arrow.Y = background.Y + Util.Round(scaleY * relativeArea.Y);
            }

            arrowHitRadius = (5 * arrows.SelectMany<ILayerSprite, int>(arrow => [arrow.Width, arrow.Height]).Max() / 4) / 2;

            for (int i = 0; i < 4; i++)
            {
                relativeArea = RelativeIconAreas[i];
                var iconBackground = iconBackgrounds[i] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 20, 7);

                var iconSize = IconSizes[i];
                double iconWidth = i == 3 ? 192.0 : 256.0;
                double iconScale = iconWidth / iconSize.Width;
                var iconHeight = iconScale * iconSize.Height;
                var icon = icons[i] = CreateSprite(iconSize.Width, iconSize.Height, Util.Round(scaleX * iconWidth), Util.Round(scaleY * iconHeight), 30, (uint)(8 + i));

                iconBackground.X = background.X + Util.Round(scaleX * relativeArea.X);
                iconBackground.Y = background.Y + Util.Round(scaleY * relativeArea.Y);

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
        if (!enabled)
            return false;

        if (area.Contains(position))
        {
            Key[] keys = [Key.W, Key.D, Key.S, Key.A];

            for (int i = 0; i < arrows.Length; i++)
            {
                var arrow = arrows[i];
                var arrowArea = new Rect(arrow.X + arrow.Width / 2 - arrowHitRadius, arrow.Y + arrow.Height / 2 - arrowHitRadius, 2 * arrowHitRadius, 2 * arrowHitRadius);

                if (arrowArea.Contains(position))
                {
                    game.OnKeyDown(keys[i], KeyModifiers.None, true);

                    arrow.Visible = true;
                    activeMarker.Visible = false;
                    active = false;
                    arrowClicked = true;

                    hideTappedArrowHandler.PostDelayed(() =>
                    {
                        if (arrowClicked)
                            arrow.Visible = false;
                    }, ShowTappedArrowDuration);

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
        if (!enabled)
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
            arrowClicked = false;
            hideTappedArrowHandler.RemoveCallbacksAndMessages(null);

            foreach (var arrow in arrows)
                arrow.Visible = false;            

            activeMarker.Visible = true;
            return true;
        }

        return false;
    }

    public void OnFingerUp(Game game, Position position)
    {
        active = false;
        activeMarker.Visible = false;

        if (!arrowClicked)
        {
            foreach (var arrow in arrows)
                arrow.Visible = false;
        }

        Direction = null;
    }

    public bool OnFingerMoveTo(Game game, Position position)
    {
        if (!enabled)
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
        enabled = background != null && background.Visible && game.InputEnable;

        if (background != null)
            disableOverlay.Visible = background.Visible && !game.InputEnable;
    }
}
