using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;
using Android.OS;

namespace AmbermoonAndroid;

internal class TouchPad
{
    bool destroyed = false;
    readonly Rect area;
    bool active = false;
    readonly int threshold = 0;
    readonly int arrowHitRadius = 0;
    const int DisableOverlayWidth = 98;
    const int DisableOverlayHeight = 64;
    const int IconDisableOverlayWidth = 19;
    const int IconDisableOverlayHeight = 19;
    bool arrowClicked = false;
    readonly Handler hideTappedArrowHandler = new(Looper.MainLooper);
    const int ShowTappedArrowDuration = 200;
    bool enabled = false;
    int iconPage = -1;
    readonly HashSet<Game.MobileIconAction> iconsActive = [];

    readonly ILayerSprite background;
    readonly ILayerSprite[] arrows = new ILayerSprite[4];
    readonly ILayerSprite activeMarker;
    readonly ILayerSprite disableOverlay;
    readonly Dictionary<int, ILayerSprite> iconDisableOverlays = [];
    readonly Func<int, int, ILayerSprite> iconDisableOverlayFactory;
    readonly ILayerSprite[] icons = new ILayerSprite[11];
    readonly Rect[,] iconAreas = new Rect[2,2];

    static uint GraphicOffset = 0;
    static readonly Rect RelativeMarkerArea = new(634, 362, 254, 254);
    static readonly Rect[] RelativeArrowAreas =
    [
        new(688, 202, 151, 141),
        new(911, 416, 133, 146),
        new(685, 639, 156, 137),
        new(482, 420, 128, 139),
    ];
    const int IconX1 = 136;
    const int IconX2 = 1116;
    const int IconY1 = 186;
    const int IconY2 = 528;
    static readonly Size[] IconSizes =
    [
        new(24, 9), // eye
        new(22, 12), // hand
        new(23, 9), // mouth
        new(26, 11), // transport
        new(22, 11), // map
        new(26, 13), // magic
        new(26, 11), // camp
        new(11, 11), // wait
        new(25, 12), // battle positions
        new(13, 11), // options
        new(22, 8), // switch
    ];
    static readonly Position[] IconLocations =
    [
        // First page
        new(0, 0), // eye
        new(0, 1), // hand
        new(1, 0), // mouth
        // Second page
        new(0, 0), // transport
        new(0, 0), // map (shares same slot with transport)
        new(0, 1), // magic
        new(1, 0), // camp
        // Third page
        new(0, 0), // wait
        new(0, 1), // battle positions
        new(1, 0), // options
        // Switch
        new(1, 1) // switch
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
            var transport = FileProvider.GetTouchPadTransport();
            var map = FileProvider.GetTouchPadMap();
            var magic = FileProvider.GetTouchPadMagic();
            var camp = FileProvider.GetTouchPadCamp();
            var wait = FileProvider.GetTouchPadWait();
            var battlePositions = FileProvider.GetTouchPadBattlePositions();
            var options = FileProvider.GetTouchPadOptions();
            var @switch = FileProvider.GetTouchPadSwitch();
            var iconDisableOverlay = FileProvider.GetTouchPadIconDisableOverlay();

            byte[] oddRowPixels = [0, 0, 0, 0xff, 0, 0, 0, 0];
            byte[] evenRowPixels = [0, 0, 0, 0, 0, 0, 0, 0xff];
            byte[] oddRow = [.. Enumerable.Repeat(oddRowPixels, DisableOverlayWidth / 2).SelectMany(x => x)];
            byte[] evenRow = [.. Enumerable.Repeat(evenRowPixels, DisableOverlayWidth / 2).SelectMany(x => x)];
            var data = new byte[DisableOverlayWidth * DisableOverlayHeight * 4];
            int index = 0;

            for (int i = 0; i < DisableOverlayHeight / 2; i++)
            {
                Buffer.BlockCopy(oddRow, 0, data, index, oddRow.Length);
                index += oddRow.Length;
                Buffer.BlockCopy(evenRow, 0, data, index, evenRow.Length);
                index += evenRow.Length;
            }

            var disableOverlay = new Graphic
            {
                Width = DisableOverlayWidth,
                Height = DisableOverlayHeight,
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
                iconDisableOverlay,
                eye,
                hand,
                mouth,
                transport,
                map,
                magic,
                camp,
                wait,
                battlePositions,
                options,
                @switch
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

            ILayerSprite CreateSprite(int texWidth, int texHeight, int width, int height, byte displayLayer, uint textureIndex, byte? alpha = null)
            {
                var sprite = alpha == null
                    ? renderView.SpriteFactory.Create(width, height, true, displayLayer) as ILayerSprite
                    : renderView.SpriteFactory.CreateWithAlpha(width, height, displayLayer);
                sprite.Visible = false;
                sprite.TextureSize = new(texWidth, texHeight);
                sprite.Layer = layer;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
                // Important for visibility check, otherwise the virtual screen is used!
                sprite.ClipArea = area;

                if (alpha != null && sprite is IAlphaSprite alphaSprite)
                {
                    alphaSprite.Alpha = alpha.Value;
                }

                return sprite;
            }

            double scaleX = area.Width / 1526.0f;
            double scaleY = area.Height / 994.0f;

            background = CreateSprite(1526, 994, area.Width, area.Height, 11, 0);
            background.X = area.X;
            background.Y = area.Y;

            disableOverlay = CreateSprite(DisableOverlayWidth, DisableOverlayHeight, area.Width, area.Height, 17, 6);
            disableOverlay.X = background.X;
            disableOverlay.Y = background.Y;

            var relativeArea = RelativeMarkerArea;
            activeMarker = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 14, 1, 128);
            activeMarker.X = background.X + Util.Round(scaleX * relativeArea.X);
            activeMarker.Y = background.Y + Util.Round(scaleY * relativeArea.Y);

            for (int a = 0; a < 4; a++)
            {
                relativeArea = RelativeArrowAreas[a];
                var arrow = arrows[a] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 14, (uint)(2 + a));

                arrow.X = background.X + Util.Round(scaleX * relativeArea.X);
                arrow.Y = background.Y + Util.Round(scaleY * relativeArea.Y);
            }

            arrowHitRadius = (5 * arrows.SelectMany<ILayerSprite, int>(arrow => [arrow.Width, arrow.Height]).Max() / 4) / 2;

            for (int i = 0; i < 4; i++)
            {
                int iconX = i < 2 ? IconX1 : IconX2;
                int iconY = i % 2 == 0 ? IconY1 : IconY2;
                relativeArea = new(iconX, iconY, 270, 276);

                var iconBackgroundX = background.X + Util.Round(scaleX * relativeArea.X);
                var iconBackgroundY = background.Y + Util.Round(scaleY * relativeArea.Y);
                var iconBackgroundWidth = Util.Round(scaleX * 270);
                var iconBackgroundHeight = Util.Round(scaleY * 276);

                iconAreas[i / 2, i % 2] = new(iconBackgroundX, iconBackgroundY, iconBackgroundWidth, iconBackgroundHeight);
            }

            for (int i = 0; i < IconSizes.Length; i++)
            {
                var iconLocation = IconLocations[i];

                relativeArea = new(iconAreas[iconLocation.X, iconLocation.Y]);

                var iconSize = IconSizes[i];
                var baseSize = 0.9f * relativeArea.Size;
                var maxSize = (float)IconSizes.Max(s => Math.Max(s.Width, s.Height));
                var factorX = iconSize.Width / maxSize;
                var factorY = iconSize.Height / maxSize;
                var iconDisplaySize = baseSize * new FloatSize(factorX, factorY);

                var icon = icons[i] = CreateSprite(iconSize.Width, iconSize.Height, Util.Round(iconDisplaySize.Width), Util.Round(iconDisplaySize.Height), 14, (uint)(9 + i));

                icon.X = relativeArea.X + (relativeArea.Width - icon.Width) / 2;
                icon.Y = relativeArea.Y + (relativeArea.Height - icon.Height) / 2;
            }

            iconDisableOverlayFactory = (int x, int y) =>
            {
                var relativeArea = new Rect(iconAreas[x, y]);

                var iconDisableOverlay = CreateSprite(IconDisableOverlayWidth, IconDisableOverlayHeight, relativeArea.Width, relativeArea.Height, 17, 8);

                iconDisableOverlay.X = relativeArea.X;
                iconDisableOverlay.Y = relativeArea.Y;
                iconDisableOverlay.Visible = true;

                return iconDisableOverlay;
            };

            IconPage = 0;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public int IconPage
    {
        get => iconPage;
        set
        {
            if (iconPage == value)
                return;

            iconPage = value;

            iconsActive.Clear();

            if (iconPage == 0)
            {
                iconsActive.Add(Game.MobileIconAction.Eye);
                iconsActive.Add(Game.MobileIconAction.Hand);
                iconsActive.Add(Game.MobileIconAction.Mouth);
            }
            else if (iconPage == 1)
            {
                iconsActive.Add(Game.MobileIconAction.Transport);
                iconsActive.Add(Game.MobileIconAction.Map);
                iconsActive.Add(Game.MobileIconAction.SpellBook);
                iconsActive.Add(Game.MobileIconAction.Camp);
            }
            else if (iconPage == 2)
            {
                iconsActive.Add(Game.MobileIconAction.BattlePositions);
                iconsActive.Add(Game.MobileIconAction.Wait);
                iconsActive.Add(Game.MobileIconAction.Options);
            }

            if (background?.Visible == true)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    icons[i].Visible = iconsActive.Contains((Game.MobileIconAction)i);
                }

                icons[^1].Visible = true; // Always show switch icon
            }
        }
    }

    public void Show(bool show)
    {
        background.Visible = show;

        for (int i = 0; i < icons.Length; i++)
            icons[i].Visible = show && iconsActive.Contains((Game.MobileIconAction)i);

        icons[^1].Visible = show; // Always show switch icon

        if (!show)
        {
            activeMarker.Visible = false;
            disableOverlay.Visible = false;

            foreach (var iconDisableOverlay in iconDisableOverlays.Values)
                iconDisableOverlay.Visible = false;

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

        foreach (var icon in icons)
            icon.Delete();

        foreach (var iconDisableOverlay in iconDisableOverlays.Values)
            iconDisableOverlay.Delete();

        iconDisableOverlays.Clear();

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

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (iconAreas[x, y].Contains(position))
                    {
                        activeMarker.Visible = false;
                        active = false;

                        if (x == 1 && y == 1) // switch
                            IconPage = (IconPage + 1) % 3;
                        else
                        {
                            var action = GetIconActionBySlot(game, y + x * 2);

                            if (action != null)
                                game.TriggerMobileIconAction(action.Value);
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    Game.MobileIconAction? GetIconActionBySlot(Game game, int slot)
    {
        switch (IconPage)
        {
            case 0:
                return slot switch
                {
                    0 => Game.MobileIconAction.Eye,
                    1 => Game.MobileIconAction.Hand,
                    2 => Game.MobileIconAction.Mouth,
                    _ => null
                };
            case 1:
                return slot switch
                {
                    0 => game.Is3D ? Game.MobileIconAction.Map : Game.MobileIconAction.Transport,
                    1 => Game.MobileIconAction.SpellBook,
                    2 => Game.MobileIconAction.Camp,
                    _ => null
                };
            case 2:
                return slot switch
                {
                    0 => Game.MobileIconAction.Wait,
                    1 => Game.MobileIconAction.BattlePositions,
                    2 => Game.MobileIconAction.Options,
                    _ => null
                };
        }

        return null;
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

    void EnableIconAtLocation(int x, int y, bool enable)
    {
        int index = y + x * 2;

        if (enable)
        {           
            if (iconDisableOverlays.TryGetValue(index, out var overlay))
            {
                overlay.Delete();
                iconDisableOverlays.Remove(index);
            }
        }
        else
        {
            if (iconDisableOverlays.TryGetValue(index, out var overlay))
            {
                overlay.Visible = true;
            }
            else
            {
                iconDisableOverlays.Add(index, iconDisableOverlayFactory(x, y));
            }
        }
    }

    public void Update(Game game)
    {
        enabled = background != null && background.Visible && game.InputEnable;

        if (background != null)
            disableOverlay.Visible = background.Visible && !game.InputEnable;

        bool[] iconsDisabled = [false, false, false];

        if (background?.Visible == true && IconPage == 1)
        {
            if (game.Is3D)
            {
                icons[(int)Game.MobileIconAction.Transport].Visible = false;
                icons[(int)Game.MobileIconAction.Map].Visible = true;
            }
            else
            {
                icons[(int)Game.MobileIconAction.Map].Visible = false;
                icons[(int)Game.MobileIconAction.Transport].Visible = true;
            }

            if (!disableOverlay.Visible)
            {
                if (!game.Is3D && !game.TransportEnabled)
                {
                    var location = IconLocations[(int)Game.MobileIconAction.Transport];
                    iconsDisabled[location.Y + location.X * 2] = true;
                }

                if (!game.CampEnabled)
                {
                    var location = IconLocations[(int)Game.MobileIconAction.Camp];
                    iconsDisabled[location.Y + location.X * 2] = true;
                }
            }
        }

        if (background?.Visible == true && IconPage == 2 && !disableOverlay.Visible && !game.SpellBookEnabled)
        {
            var location = IconLocations[(int)Game.MobileIconAction.SpellBook];
            iconsDisabled[location.Y + location.X * 2] = true;
        }

        EnableIconAtLocation(0, 0, !iconsDisabled[0]);
        EnableIconAtLocation(0, 1, !iconsDisabled[1]);
        EnableIconAtLocation(1, 0, !iconsDisabled[2]);
    }
}
