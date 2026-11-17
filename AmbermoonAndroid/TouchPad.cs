using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;
using System.Diagnostics;

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
    int tappedArrowIndex = -1;
    long tappedArrowTimestamp = 0;
    const int ShowTappedArrowDuration = 200;
    bool enabled = false;
    bool shown = false;
    // In the immersive mode only the 4 small circles and the big circle are shown at the lower right.
    bool immersive = false;
    int iconPage = -1;
    readonly HashSet<GameCore.MobileIconAction> iconsActive = [];
    readonly Size screenSize;
    readonly double scaleX;
    readonly double scaleY;
    // screen positions of the texture origin (normal mode: both are the touchpad area position)
    Position smallCirclesOrigin = new();
    Position bigCircleOrigin = new();

    readonly ILayerSprite background;
    // 0 to 3: small circles (index x * 2 + y like the icon slots), 4: big circle
    readonly ILayerSprite[] immersiveCircles = new ILayerSprite[5];
    readonly ILayerSprite[] immersiveDisableOverlays = new ILayerSprite[5];
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
    // Areas of the circles inside the touchpad graphic (with their outline).
    // They are cut out for the immersive mode.
    static readonly Rect[] RelativeSmallCircleAreas =
    [
        new(134, 184, 274, 280), // left top
        new(134, 526, 274, 280), // left bottom
        new(1114, 184, 274, 280), // right top
        new(1114, 526, 274, 280), // right bottom
    ];
    static readonly Rect RelativeBigCircleArea = new(398, 122, 728, 728);
    const int ImmersiveCirclesGraphicIndex = 20; // 4 small circles, then the big circle
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

    public bool ContainsPosition(Position position)
    {
        if (!immersive)
            return area.Contains(position);

        // only the circles are shown, taps between them reach the map
        return immersiveCircles.Any(circle => EllipseContains(new Rect(circle.X, circle.Y, circle.Width, circle.Height), position));
    }

    static bool EllipseContains(Rect rect, Position position)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return false;

        double dx = (position.X + 0.5 - (rect.X + rect.Width * 0.5)) / (rect.Width * 0.5);
        double dy = (position.Y + 0.5 - (rect.Y + rect.Height * 0.5)) / (rect.Height * 0.5);

        return dx * dx + dy * dy <= 1.0;
    }

    /// <summary>
    /// Cuts a circle out of the touchpad graphic. The pixels outside the circle are transparent.
    /// </summary>
    static Graphic CutCircle(Graphic source, Rect area)
    {
        var data = new byte[area.Width * area.Height * 4];
        double radiusX = area.Width * 0.5;
        double radiusY = area.Height * 0.5;

        for (int y = 0; y < area.Height; y++)
        {
            for (int x = 0; x < area.Width; x++)
            {
                int sourceIndex = ((area.Y + y) * source.Width + area.X + x) * 4;
                int targetIndex = (y * area.Width + x) * 4;
                double dx = (x + 0.5 - radiusX) / radiusX;
                double dy = (y + 0.5 - radiusY) / radiusY;
                // smooth edge of about 1 pixel
                double coverage = Math.Clamp((1.0 - Math.Sqrt(dx * dx + dy * dy)) * Math.Min(radiusX, radiusY) + 0.5, 0.0, 1.0);

                data[targetIndex + 0] = source.Data[sourceIndex + 0];
                data[targetIndex + 1] = source.Data[sourceIndex + 1];
                data[targetIndex + 2] = source.Data[sourceIndex + 2];
                data[targetIndex + 3] = (byte)Math.Round(source.Data[sourceIndex + 3] * coverage);
            }
        }

        return new Graphic
        {
            Width = area.Width,
            Height = area.Height,
            IndexedGraphic = false,
            Data = data
        };
    }

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
                @switch,
                // immersive mode (index 20 to 24)
                ..RelativeSmallCircleAreas.Select(circleArea => CutCircle(background, circleArea)),
                CutCircle(background, RelativeBigCircleArea)
            ];

            return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public TouchPad(IGameRenderView renderView, Rect touchPadArea, Size screenSize)
    {
        try
        {
            area = new(touchPadArea);
            this.screenSize = new(screenSize);
            threshold = area.Width / 10; // 10% of the width is the threshold for touchpad movement
            // the sprites are moved in the immersive mode, so the whole screen is used for the visibility check
            var screenArea = new Rect(0, 0, screenSize.Width, screenSize.Height);

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Images);
            var layer = renderView.GetLayer(Layer.Images);

            ILayerSprite CreateSprite(int texWidth, int texHeight, int width, int height, byte displayLayer, uint textureIndex, byte? alpha = null)
            {
                var sprite = alpha == null
                    ? renderView.SpriteFactory.CreateLayered(width, height, displayLayer)
                    : renderView.SpriteFactory.CreateWithAlpha(width, height, displayLayer);
                sprite.Visible = false;
                sprite.TextureSize = new(texWidth, texHeight);
                sprite.Layer = layer;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
                // Important for visibility check, otherwise the virtual screen is used!
                sprite.ClipArea = screenArea;

                if (alpha != null && sprite is IAlphaSprite alphaSprite)
                {
                    alphaSprite.Alpha = alpha.Value;
                }

                return sprite;
            }

            scaleX = area.Width / 1526.0f;
            scaleY = area.Height / 994.0f;

            background = CreateSprite(1526, 994, area.Width, area.Height, 11, 0);
            disableOverlay = CreateSprite(DisableOverlayWidth, DisableOverlayHeight, area.Width, area.Height, 17, 6);

            for (int i = 0; i < immersiveCircles.Length; i++)
            {
                var circleArea = i < 4 ? RelativeSmallCircleAreas[i] : RelativeBigCircleArea;
                int width = Util.Round(scaleX * circleArea.Width);
                int height = Util.Round(scaleY * circleArea.Height);
                immersiveCircles[i] = CreateSprite(circleArea.Width, circleArea.Height, width, height, 11, (uint)(ImmersiveCirclesGraphicIndex + i));
                immersiveDisableOverlays[i] = CreateSprite(IconDisableOverlayWidth, IconDisableOverlayHeight, width, height, 17, 8);
            }

            var relativeArea = RelativeMarkerArea;
            activeMarker = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 14, 1, 128);

            for (int a = 0; a < 4; a++)
            {
                relativeArea = RelativeArrowAreas[a];
                arrows[a] = CreateSprite(relativeArea.Width, relativeArea.Height, Util.Round(scaleX * relativeArea.Width), Util.Round(scaleY * relativeArea.Height), 14, (uint)(2 + a));
            }

            arrowHitRadius = (5 * arrows.SelectMany<ILayerSprite, int>(arrow => [arrow.Width, arrow.Height]).Max() / 4) / 2;

            var iconAreaSize = new Size(Util.Round(scaleX * 270), Util.Round(scaleY * 276));

            for (int i = 0; i < IconSizes.Length; i++)
            {
                var iconSize = IconSizes[i];
                var baseSize = 0.9f * iconAreaSize;
                var maxSize = (float)IconSizes.Max(s => Math.Max(s.Width, s.Height));
                var factorX = iconSize.Width / maxSize;
                var factorY = iconSize.Height / maxSize;
                var iconDisplaySize = baseSize * new FloatSize(factorX, factorY);

                icons[i] = CreateSprite(iconSize.Width, iconSize.Height, Util.Round(iconDisplaySize.Width), Util.Round(iconDisplaySize.Height), 14, (uint)(9 + i));
            }

            Arrange();

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

    Position ToScreen(Position origin, int relativeX, int relativeY) =>
        new(origin.X + Util.Round(scaleX * relativeX), origin.Y + Util.Round(scaleY * relativeY));

    /// <summary>
    /// Positions all elements. In the immersive mode the right circles touch the right
    /// border and the bottom circles (and the big circle) touch the bottom border.
    /// </summary>
    void Arrange()
    {
        if (immersive)
        {
            var rightBottomCircle = RelativeSmallCircleAreas[3];
            smallCirclesOrigin = new(screenSize.Width - Util.Round(scaleX * rightBottomCircle.Right),
                screenSize.Height - Util.Round(scaleY * rightBottomCircle.Bottom));
            bigCircleOrigin = new(smallCirclesOrigin.X, screenSize.Height - Util.Round(scaleY * RelativeBigCircleArea.Bottom));
        }
        else
        {
            smallCirclesOrigin = new(area.X, area.Y);
            bigCircleOrigin = new(area.X, area.Y);
        }

        background.X = area.X;
        background.Y = area.Y;
        disableOverlay.X = area.X;
        disableOverlay.Y = area.Y;

        for (int i = 0; i < immersiveCircles.Length; i++)
        {
            var circleArea = i < 4 ? RelativeSmallCircleAreas[i] : RelativeBigCircleArea;
            var position = ToScreen(i < 4 ? smallCirclesOrigin : bigCircleOrigin, circleArea.X, circleArea.Y);
            immersiveCircles[i].X = immersiveDisableOverlays[i].X = position.X;
            immersiveCircles[i].Y = immersiveDisableOverlays[i].Y = position.Y;
        }

        var markerPosition = ToScreen(bigCircleOrigin, RelativeMarkerArea.X, RelativeMarkerArea.Y);
        activeMarker.X = markerPosition.X;
        activeMarker.Y = markerPosition.Y;

        for (int a = 0; a < 4; a++)
        {
            var arrowPosition = ToScreen(bigCircleOrigin, RelativeArrowAreas[a].X, RelativeArrowAreas[a].Y);
            arrows[a].X = arrowPosition.X;
            arrows[a].Y = arrowPosition.Y;
        }

        for (int i = 0; i < 4; i++)
        {
            int iconX = i < 2 ? IconX1 : IconX2;
            int iconY = i % 2 == 0 ? IconY1 : IconY2;
            var iconAreaPosition = ToScreen(smallCirclesOrigin, iconX, iconY);

            iconAreas[i / 2, i % 2] = new(iconAreaPosition.X, iconAreaPosition.Y, Util.Round(scaleX * 270), Util.Round(scaleY * 276));
        }

        for (int i = 0; i < icons.Length; i++)
        {
            var iconLocation = IconLocations[i];
            var iconArea = iconAreas[iconLocation.X, iconLocation.Y];
            var icon = icons[i];

            icon.X = iconArea.X + (iconArea.Width - icon.Width) / 2;
            icon.Y = iconArea.Y + (iconArea.Height - icon.Height) / 2;
        }

        // the overlays of disabled icons are recreated at the new positions by the next update
        foreach (var iconDisableOverlay in iconDisableOverlays.Values)
            iconDisableOverlay.Delete();

        iconDisableOverlays.Clear();
    }

    Position MovementCenter => immersive
        ? ToScreen(bigCircleOrigin, RelativeBigCircleArea.X + RelativeBigCircleArea.Width / 2, RelativeBigCircleArea.Y + RelativeBigCircleArea.Height / 2)
        : new(background.X + background.Width / 2, background.Y + background.Height / 2);

    void SetImmersive(bool immersive)
    {
        if (this.immersive == immersive)
            return;

        this.immersive = immersive;

        // a running movement is stopped
        active = false;
        arrowClicked = false;
        tappedArrowIndex = -1;
        Direction = null;
        activeMarker.Visible = false;

        foreach (var arrow in arrows)
            arrow.Visible = false;

        Arrange();
        UpdateFrameVisibility();
    }

    void UpdateFrameVisibility()
    {
        background.Visible = shown && !immersive;

        foreach (var circle in immersiveCircles)
            circle.Visible = shown && immersive;
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
                iconsActive.Add(GameCore.MobileIconAction.Eye);
                iconsActive.Add(GameCore.MobileIconAction.Hand);
                iconsActive.Add(GameCore.MobileIconAction.Mouth);
            }
            else if (iconPage == 1)
            {
                iconsActive.Add(GameCore.MobileIconAction.Transport);
                iconsActive.Add(GameCore.MobileIconAction.Map);
                iconsActive.Add(GameCore.MobileIconAction.SpellBook);
                iconsActive.Add(GameCore.MobileIconAction.Camp);
            }
            else if (iconPage == 2)
            {
                iconsActive.Add(GameCore.MobileIconAction.BattlePositions);
                iconsActive.Add(GameCore.MobileIconAction.Wait);
                iconsActive.Add(GameCore.MobileIconAction.Options);
            }

            if (shown)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    icons[i].Visible = iconsActive.Contains((GameCore.MobileIconAction)i);
                }

                icons[^1].Visible = true; // Always show switch icon
            }
        }
    }

    public void Show(bool show)
    {
        shown = show;
        UpdateFrameVisibility();

        for (int i = 0; i < icons.Length; i++)
            icons[i].Visible = show && iconsActive.Contains((GameCore.MobileIconAction)i);

        icons[^1].Visible = show; // Always show switch icon

        if (!show)
        {
            activeMarker.Visible = false;
            disableOverlay.Visible = false;

            foreach (var immersiveDisableOverlay in immersiveDisableOverlays)
                immersiveDisableOverlay.Visible = false;

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

        foreach (var circle in immersiveCircles)
            circle.Delete();

        foreach (var immersiveDisableOverlay in immersiveDisableOverlays)
            immersiveDisableOverlay.Delete();

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

    public bool OnTap(GameCore game, Position position)
    {
        if (!enabled)
            return false;

        if (ContainsPosition(position))
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
                    tappedArrowIndex = i;
                    tappedArrowTimestamp = Stopwatch.GetTimestamp();

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

            // the map below the circles must not receive the tap
            return immersive;
        }

        return false;
    }

    GameCore.MobileIconAction? GetIconActionBySlot(GameCore game, int slot)
    {
        switch (IconPage)
        {
            case 0:
                return slot switch
                {
                    0 => GameCore.MobileIconAction.Eye,
                    1 => GameCore.MobileIconAction.Hand,
                    2 => GameCore.MobileIconAction.Mouth,
                    _ => null
                };
            case 1:
                return slot switch
                {
                    0 => game.Is3D ? GameCore.MobileIconAction.Map : GameCore.MobileIconAction.Transport,
                    1 => GameCore.MobileIconAction.SpellBook,
                    2 => GameCore.MobileIconAction.Camp,
                    _ => null
                };
            case 2:
                return slot switch
                {
                    0 => GameCore.MobileIconAction.Wait,
                    1 => GameCore.MobileIconAction.BattlePositions,
                    2 => GameCore.MobileIconAction.Options,
                    _ => null
                };
        }

        return null;
    }

    public bool OnLongPress(GameCore game, Position position)
    {
        if (!enabled)
        {
            if (active)
                OnFingerUp(game, position);

            return false;
        }

        if (active)
            return true;

        if (ContainsPosition(position))
        {
            active = true;
            arrowClicked = false;
            tappedArrowIndex = -1;

            foreach (var arrow in arrows)
                arrow.Visible = false;

            activeMarker.Visible = true;
            return true;
        }

        return false;
    }

    public void OnFingerUp(GameCore game, Position position)
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

    public bool OnFingerMoveTo(GameCore game, Position position)
    {
        if (!enabled)
        {
            if (active)
                OnFingerUp(game, position);

            return false;
        }

        if (!active)
            return false;

        var center = MovementCenter;
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

    public void Update(GameCore game)
    {
        if (tappedArrowIndex >= 0 && arrowClicked)
        {
            double elapsedMs = Stopwatch.GetElapsedTime(tappedArrowTimestamp).TotalMilliseconds;

            if (elapsedMs >= ShowTappedArrowDuration)
            {
                arrows[tappedArrowIndex].Visible = false;
                tappedArrowIndex = -1;
            }
        }

        SetImmersive(game.ImmersiveActive);

        // popups like the quest log may overlap the touchpad, their buttons must receive the taps
        enabled = shown && game.InputEnable && !game.PopupActive;

        bool inputDisabled = shown && !game.InputEnable && !game.HideMobileTouchpadDisableOverlay;
        disableOverlay.Visible = inputDisabled && !immersive;

        // in the immersive mode only the circles are dimmed
        foreach (var immersiveDisableOverlay in immersiveDisableOverlays)
            immersiveDisableOverlay.Visible = inputDisabled && immersive;

        bool[] iconsDisabled = [false, false, false];

        if (shown && IconPage == 1)
        {
            if (game.Is3D)
            {
                icons[(int)GameCore.MobileIconAction.Transport].Visible = false;
                icons[(int)GameCore.MobileIconAction.Map].Visible = true;
            }
            else
            {
                icons[(int)GameCore.MobileIconAction.Map].Visible = false;
                icons[(int)GameCore.MobileIconAction.Transport].Visible = true;
            }

            if (!inputDisabled)
            {
                if (!game.Is3D && !game.TransportEnabled)
                {
                    var location = IconLocations[(int)GameCore.MobileIconAction.Transport];
                    iconsDisabled[location.Y + location.X * 2] = true;
                }

                if (!game.CampEnabled)
                {
                    var location = IconLocations[(int)GameCore.MobileIconAction.Camp];
                    iconsDisabled[location.Y + location.X * 2] = true;
                }
            }
        }

        if (shown && IconPage == 1 && !inputDisabled && !game.SpellBookEnabled)
        {
            var location = IconLocations[(int)GameCore.MobileIconAction.SpellBook];
            iconsDisabled[location.Y + location.X * 2] = true;
        }

        EnableIconAtLocation(0, 0, !iconsDisabled[0]);
        EnableIconAtLocation(0, 1, !iconsDisabled[1]);
        EnableIconAtLocation(1, 0, !iconsDisabled[2]);
    }
}
