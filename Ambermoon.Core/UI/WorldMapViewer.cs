using Ambermoon.Data;
using Ambermoon.Render;
using StbImageSharp;
using System;
using System.IO;

namespace Ambermoon.UI;

/// <summary>
/// Shows the painted world map in the whole window. It keeps its aspect ratio, can be
/// scrolled and can be zoomed from "fits into the window" up to a multiple of its native size.
/// </summary>
internal sealed class WorldMapViewer
{
    const float MaxNativeZoom = 2.0f;
    const float WheelZoomFactor = 1.15f;
    readonly IRenderView renderView;
    readonly int imageWidth;
    readonly int imageHeight;
    readonly bool mobile;
    Size windowSize;
    float x = 0.0f; // left of the image relative to the window width
    float y = 0.0f; // top of the image relative to the window height
    float width = 1.0f; // image width relative to the window width
    Position? lastPosition = null;
    bool pressed = false;
    Key? pressedKey = null;

    public event Action? Closed;

    public WorldMapViewer(IRenderView renderView, GameLanguage language, bool mobile)
    {
        this.renderView = renderView;
        this.mobile = mobile;
        var map = LoadImage("WorldMap.jpg");
        var text = LoadImage($"WorldMapText_{GetLanguageCode(language)}.png");
        var data = map.Data;

        // the text is baked into the map as only the matching language is ever displayed
        for (int i = 0; i < data.Length; i += 4)
        {
            int alpha = text.Data[i + 3];

            if (alpha == 0)
                continue;

            for (int c = 0; c < 3; ++c)
                data[i + c] = (byte)((data[i + c] * (255 - alpha) + text.Data[i + c] * alpha) / 255);
        }

        imageWidth = map.Width;
        imageHeight = map.Height;
        windowSize = new Size(renderView.WindowSize);
        renderView.ShowFullscreenImage(imageWidth, imageHeight, data, !mobile);
        width = CoverWidth;
        x = (1.0f - width) * 0.5f;
        y = (1.0f - Height) * 0.5f;
        Apply();
    }

    static string GetLanguageCode(GameLanguage language) => language switch
    {
        GameLanguage.German => "de",
        GameLanguage.French => "fr",
        GameLanguage.Polish => "pl",
        GameLanguage.Czech => "cz",
        _ => "en",
    };

    static ImageResult LoadImage(string name)
    {
        using var stream = typeof(WorldMapViewer).Assembly.GetManifestResourceStream($"Ambermoon.Assets.{name}")
            ?? throw new FileNotFoundException($"Missing embedded resource '{name}'.");

        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    float ImageRatio => (float)imageWidth / imageHeight;
    float WindowRatio => (float)windowSize.Width / windowSize.Height;
    float Height => width * WindowRatio / ImageRatio;
    float CoverWidth => Math.Max(1.0f, ImageRatio / WindowRatio);
    float ContainWidth => Math.Min(1.0f, ImageRatio / WindowRatio);
    float MaxWidth => Math.Max(CoverWidth, MaxNativeZoom * imageWidth / windowSize.Width);

    void Apply()
    {
        width = Math.Clamp(width, ContainWidth, MaxWidth);
        float height = Height;
        x = width < 1.0f ? (1.0f - width) * 0.5f : Math.Clamp(x, 1.0f - width, 0.0f);
        y = height < 1.0f ? (1.0f - height) * 0.5f : Math.Clamp(y, 1.0f - height, 0.0f);
        renderView.SetFullscreenImageArea(x, y, width, height);
    }

    void ZoomAt(float factor, Position position)
    {
        float focusX = (float)position.X / windowSize.Width;
        float focusY = (float)position.Y / windowSize.Height;
        float oldWidth = width;
        float oldHeight = Height;
        width = Math.Clamp(width * factor, ContainWidth, MaxWidth);

        // the map point below the mouse or fingers stays there
        x = focusX - (focusX - x) * width / oldWidth;
        y = focusY - (focusY - y) * Height / oldHeight;
        Apply();
    }

    void MoveTo(Position position)
    {
        if (lastPosition != null)
        {
            x += (float)(position.X - lastPosition.X) / windowSize.Width;
            y += (float)(position.Y - lastPosition.Y) / windowSize.Height;
            Apply();
        }

        lastPosition = new Position(position);
    }

    void Close()
    {
        renderView.HideFullscreenImage();
        Closed?.Invoke();
    }

    public void Update()
    {
        var size = renderView.WindowSize;

        if (size.Width != windowSize.Width || size.Height != windowSize.Height)
        {
            windowSize = new Size(size);
            Apply();
        }
    }

    public void OnMouseWheel(int yScroll, Position position)
    {
        // on mobile devices this is the scroll gesture
        if (!mobile && yScroll != 0)
            ZoomAt(MathF.Pow(WheelZoomFactor, yScroll), position);
    }

    public void OnMouseDown(Position position, MouseButtons buttons)
    {
        pressed = true;
        lastPosition = new Position(position);
    }

    public void OnMouseMove(Position position, MouseButtons buttons)
    {
        if (!mobile && buttons.HasFlag(MouseButtons.Left))
            MoveTo(position);
        else
            lastPosition = new Position(position);
    }

    public void OnMouseUp(MouseButtons buttons)
    {
        // Only a press inside the viewer counts. The release of the
        // click which has opened the viewer must not close it again.
        if (pressed && buttons.HasFlag(MouseButtons.Right))
            Close();

        pressed = false;
    }

    public void OnKeyDown(Key key)
    {
        if (key == Key.Escape || key == Key.Return || key == Key.Space || key == Key.M)
            pressedKey = key;
    }

    public void OnKeyUp(Key key)
    {
        if (key == pressedKey)
            Close();
    }

    public void OnLongPress() => Close();

    public void OnFingerDown(Position position) => lastPosition = new Position(position);

    public void OnFingerUp() => lastPosition = null;

    public void OnFingerMoveTo(Position position) => MoveTo(position);

    public void OnPinchZoom(float factor, Position focus)
    {
        ZoomAt(factor, focus);
        lastPosition = null; // the remaining finger must not move the map by the pinch distance
    }
}
