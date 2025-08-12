using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Render;
using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon;

internal class LoadingBar
{
    bool destroyed = false;
    IRenderView renderView;

    static TextureAtlasManager textureAtlasManager = null;

    readonly ILayerSprite left;
    readonly ILayerSprite right;
    readonly List<ILayerSprite> midParts = [];
    readonly List<ILayerSprite> progressParts = [];

    static uint GraphicOffset;

    static Graphic LoadImage(byte[] imageData)
    {
        var dataReader = new DataReader(imageData);
        int width = dataReader.ReadWord();
        int height = dataReader.ReadWord();
        int numColors = dataReader.ReadByte();

        byte[] colors = new byte[numColors * 3];

        for (int i = 0; i < numColors; i++)
        {
            colors[i * 3 + 0] = dataReader.ReadByte();
            colors[i * 3 + 1] = dataReader.ReadByte();
            colors[i * 3 + 2] = dataReader.ReadByte();
        }

        int chunkSize = width * height;
        byte[] data = new byte[chunkSize * 4];

        for (int i = 0; i < chunkSize; ++i)
        {
            int index = dataReader.ReadByte();

            data[i * 4 + 0] = colors[index * 3 + 0];
            data[i * 4 + 1] = colors[index * 3 + 1];
            data[i * 4 + 2] = colors[index * 3 + 2];
            data[i * 4 + 3] = 0xff;
        }

        return new Graphic
        {
            Width = width,
            Height = height,
            Data = data,
            IndexedGraphic = false
        };
    }

    public static void Initialize(TextureAtlasManager textureAtlasManager)
    {
        if (LoadingBar.textureAtlasManager != textureAtlasManager)
        {
            LoadingBar.textureAtlasManager = textureAtlasManager;

            textureAtlasManager.AddFromGraphics(Layer.Images, GetGraphics(100));
        }
    }

    private static Dictionary<uint, Graphic> GetGraphics(uint offset)
    {
        GraphicOffset = offset;

        Graphic[] graphics =
        [
            LoadImage(Resources.LoadingBarLeft),
            LoadImage(Resources.LoadingBarRight),
            LoadImage(Resources.LoadingBarMid),
            LoadImage(Resources.LoadingBarRed),
            LoadImage(Resources.LoadingBarYellow),
            LoadImage(Resources.LoadingBarGreen)
        ];

        return graphics.Select((gfx, index) => new { gfx, index }).ToDictionary(b => offset + (uint)b.index, b => b.gfx);
    }

    public LoadingBar(IRenderView renderView)
    {
        this.renderView = renderView;

        renderView.ShowImageLayerOnly = true;

        var area = new Rect(Position.Zero, renderView.FramebufferSize);
        var textureAtlas = textureAtlasManager.GetOrCreate(Layer.Images);
        var layer = renderView.GetLayer(Layer.Images);

        ILayerSprite CreateSprite(Size texSize, Size size, byte displayLayer, uint textureIndex)
        {
            var sprite = renderView.SpriteFactory.Create(size.Width, size.Height, true, displayLayer) as ILayerSprite;
            sprite.Visible = false;
            sprite.TextureSize = new(texSize.Width, texSize.Height);
            sprite.Layer = layer;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(GraphicOffset + textureIndex);
            // Important for visibility check, otherwise the virtual screen is used!
            sprite.ClipArea = area;

            return sprite;
        }

        float desiredScreenPortion = area.Width / 8;
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
        int x = (area.Width - scaledWidth) / 2;
        int y = area.Height - area.Height / 8;

        left = CreateSprite(leftTexSize, leftSize, 0, 0);
        left.X = x;
        left.Y = y;
        left.Visible = true;

        x += left.Width;

        for (int i = 0; i < midPartCount; i++)
        {
            var midPart = CreateSprite(midTexSize, midSize, 0, 2);
            var color = CreateSprite(colorTexSize, colorSize, 0, 5);

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
        //var textureAtlas = textureAtlasManager.GetOrCreate(Layer.Images);
        progress = Util.Limit(0.0f, progress, 1.0f);

        int width = midParts.Count * midParts[0].Width;
        int barWidth = Util.Round(progress * width);

        int colorWidth = progressParts[0].Width;
        int numVisibleColors = Util.Limit(0, (barWidth + colorWidth / 2) / colorWidth, progressParts.Count);

        /*uint colorIndex = 3;
        if (numVisibleColors >= progressParts.Count * 2 / 3)
            colorIndex = 5;
        else if (numVisibleColors >= progressParts.Count / 3)
            colorIndex = 4;

        var textureOffset = textureAtlas.GetOffset(GraphicOffset + colorIndex);*/

        for (int i = 0; i < numVisibleColors; i++)
        {
            //progressParts[i].TextureAtlasOffset = textureOffset;
            progressParts[i].Visible = true;
        }
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
