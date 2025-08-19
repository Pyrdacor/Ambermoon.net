using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon.Frontend;

public class LoadingBar
{
    bool destroyed = false;
    readonly IRenderView renderView;

    static TextureAtlasManager? textureAtlasManager = null;

    readonly ILayerSprite left;
    readonly ILayerSprite right;
    readonly List<ILayerSprite> midParts = [];
    readonly List<ILayerSprite> progressParts = [];

    static uint GraphicOffset;
    static Func<int, Graphic>? graphicsProvider;

    public static void Initialize(TextureAtlasManager textureAtlasManager, Func<int, Graphic>? graphicsProvider)
    {
        LoadingBar.graphicsProvider = graphicsProvider;

        if (LoadingBar.textureAtlasManager != textureAtlasManager)
        {
            LoadingBar.textureAtlasManager = textureAtlasManager;

            textureAtlasManager.AddFromGraphics(Layer.Images, GetGraphics(100));
        }
    }

    private static Dictionary<uint, Graphic> GetGraphics(uint offset)
    {
        GraphicOffset = offset;
        var gfxProvider = graphicsProvider!;

        Graphic[] graphics =
        [
            gfxProvider(0),
            gfxProvider(1),
            gfxProvider(2),
            gfxProvider(3),
        ];

        return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
    }

    public LoadingBar(IRenderView renderView, float sizeFactor, float bottomDistFactor)
    {
        this.renderView = renderView;

        renderView.ShowImageLayerOnly = true;

        var area = new Rect(renderView.RenderScreenArea);
        var textureAtlas = textureAtlasManager!.GetOrCreate(Layer.Images);
        var layer = renderView.GetLayer(Layer.Images);

        ILayerSprite CreateSprite(Size texSize, Size size, byte displayLayer, uint textureIndex)
        {
            var sprite = renderView.SpriteFactory.CreateLayered(size.Width, size.Height, displayLayer);
            sprite.Visible = false;
            sprite.TextureSize = new(texSize.Width, texSize.Height);
            sprite.Layer = layer;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
            // Important for visibility check, otherwise the virtual screen is used!
            sprite.ClipArea = area;

            return sprite;
        }

        float desiredScreenPortion = (area.X * 2 + area.Width) * sizeFactor;
        const int DefaultWidth = 116; // 100 for the percentage and 16 for the borders
        float scale = desiredScreenPortion / DefaultWidth;

        var leftTexSize = new Size(8, 14);
        var rightTexSize = new Size(7, 14);
        var midTexSize = new Size(4, 14);
        var colorTexSize = new Size(4, 6);
        var leftSize = (leftTexSize * scale).ToSize();
        var rightSize = (rightTexSize * scale).ToSize();
        var midSize = (midTexSize * scale).ToSize();
        var colorSize = (colorTexSize * scale).ToSize();
        int scaledWidth = Util.Round(DefaultWidth * scale);

        while ((scaledWidth - leftSize.Width - rightSize.Width) % midSize.Width != 0)
            ++scaledWidth;

        int midPartCount = (scaledWidth - leftSize.Width - rightSize.Width) / midSize.Width;
        int colorYOffset = (midSize.Height - colorSize.Height) / 2;
        int x = area.X + (2 * area.X + area.Width - scaledWidth) / 2;
        int y = area.Height - Util.Round(area.Height * bottomDistFactor);

        left = CreateSprite(leftTexSize, leftSize, 0, 0);
        left.X = x;
        left.Y = y;
        left.Visible = true;

        x += left.Width;

        for (int i = 0; i < midPartCount; i++)
        {
            var midPart = CreateSprite(midTexSize, midSize, 0, 2);
            var color = CreateSprite(colorTexSize, colorSize, 10, 3);

            midPart.X = x;
            midPart.Y = y;
            midPart.Visible = true;
            color.X = x;
            color.Y = y + colorYOffset;

            x += midPart.Width;

            midParts.Add(midPart);
            progressParts.Add(color);
        }

        right = CreateSprite(rightTexSize, rightSize, 0, 1);
        right.X = x;
        right.Y = y;
        right.Visible = true;
    }

    public void SetProgress(float progress)
    {
        progress = Util.Limit(0.0f, progress, 1.0f);

        int width = midParts.Count * midParts[0].Width;
        int barWidth = Util.Round(progress * width);

        int colorWidth = progressParts[0].Width;
        int numVisibleColors = Util.Limit(0, (barWidth + colorWidth / 2) / colorWidth, progressParts.Count);

        for (int i = 0; i < numVisibleColors; i++)
            progressParts[i].Visible = true;
    }

    public void Destroy()
    {
        if (destroyed)
            return;

        left.Delete();
        right.Delete();

        midParts.ForEach(midPart => midPart.Delete());
        midParts.Clear();

        progressParts.ForEach(progressPart => progressPart.Delete());
        progressParts.Clear();

        renderView.ShowImageLayerOnly = false;
        renderView.Close();

        destroyed = true;
    }

    public void Render()
    {
        renderView.Render(null);
    }
}
