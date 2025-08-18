using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System.IO.Compression;

namespace Ambermoon.Frontend;

public class AdvancedLogo
{
    enum CommandType
    {
        Wait,
        FadeInAdvancedImage,
        FadeOutAdvancedImage
    }

    struct Command
    {
        public CommandType Type;
        public uint Time;
    }

    static int logoWidth;
    static int logoHeight;
    readonly Queue<Command> commands;
    Command? currentCommand = null;
    DateTime currentCommandStartTime = DateTime.MaxValue;
    IAlphaSprite? sprite = null;
    static TextureAtlasManager? textureAtlasManager = null;

    public AdvancedLogo()
    {
        commands = new Queue<Command>(3);

        commands.Enqueue(new Command
        {
            Type = CommandType.FadeInAdvancedImage,
            Time = 2000
        });
        commands.Enqueue(new Command
        {
            Type = CommandType.Wait,
            Time = 3000

        });
        commands.Enqueue(new Command
        {
            Type = CommandType.FadeOutAdvancedImage,
            Time = 2000
        });
    }

    static Graphic LoadImage(DataReader dataReader)
    {
        int width = dataReader.ReadWord();
        int height = dataReader.ReadWord();
        int chunkSize = width * height;
        byte[] data = new byte[chunkSize * 4];

        for (int i = 0; i < chunkSize; ++i)
        {
            data[i * 4] = dataReader.ReadByte(); // R
            data[i * 4 + 3] = 0xff; // A, full opaque
        }

        for (int i = 0; i < chunkSize; ++i)
        {
            data[i * 4 + 1] = dataReader.ReadByte(); // G
        }

        for (int i = 0; i < chunkSize; ++i)
        {
            data[i * 4 + 2] = dataReader.ReadByte(); // B
        }

        return new Graphic
        {
            Width = width,
            Height = height,
            Data = data,
            IndexedGraphic = false
        };
    }

    public static void Initialize(TextureAtlasManager textureAtlasManager, Func<byte[]> logoDataProvider)
    {
        if (AdvancedLogo.textureAtlasManager != textureAtlasManager)
        {
            AdvancedLogo.textureAtlasManager = textureAtlasManager;

            var logoStream = new MemoryStream(logoDataProvider!());
            var deflateStream = new DeflateStream(logoStream, CompressionMode.Decompress);
            var decompressedStream = new MemoryStream();
            deflateStream.CopyTo(decompressedStream);
            deflateStream.Dispose();
            var logoData = new DataReader(decompressedStream.ToArray());
            var logoAdvancedGraphic = LoadImage(logoData);

            if (!textureAtlasManager.HasLayer(Layer.Images))
            {
                textureAtlasManager.AddFromGraphics(Layer.Images, new Dictionary<uint, Graphic>
                {
                    { 0u, logoAdvancedGraphic }
                });
            }

            logoWidth = logoAdvancedGraphic.Width;
            logoHeight = logoAdvancedGraphic.Height;
        }
    }

    public void Cleanup()
    {
        sprite?.Delete();
    }

    public void Update(IGameRenderView renderView, Action finished)
    {
        if (renderView == null)
            return;

        bool commandActivated = false;

        if (currentCommand == null)
        {
            if (commands.Count == 0)
            {
                Cleanup();
                finished?.Invoke();
                return;
            }

            currentCommand = commands.Dequeue();
            currentCommandStartTime = DateTime.Now;
            commandActivated = true;
        }

        ProcessCurrentCommand(renderView, commandActivated);
    }


    void ProcessCurrentCommand(IGameRenderView renderView, bool commandActivated)
    {
        var command = currentCommand;

        switch (command?.Type)
        {
            case CommandType.Wait:
                if ((DateTime.Now - currentCommandStartTime).TotalMilliseconds >= command!.Value.Time)
                    currentCommand = null;
                break;
            case CommandType.FadeInAdvancedImage:
                if (commandActivated)
                {
                    var area = renderView.RenderScreenArea;
                    var textureAtlas = textureAtlasManager!.GetOrCreate(Layer.Images);
                    float ratio = (float)logoWidth / logoHeight;
                    int height = area.Height;
                    int width = Util.Round(ratio * height);
                    sprite = renderView.SpriteFactory.CreateWithAlpha(width, height);
                    sprite.Layer = renderView.GetLayer(Layer.Images);
                    // Important for visibility check, otherwise the virtual screen is used!
                    sprite.ClipArea = new Rect(area);
                    sprite.X = area.X + (area.X * 2 + area.Width - width) / 2;
                    sprite.Y = area.Y + (area.Y * 2 + area.Height - height) / 2;
                    sprite.TextureAtlasOffset = textureAtlas.GetOffset(0);
                    sprite.TextureSize = new Size(logoWidth, logoHeight);
                    sprite.Alpha = 0;
                    sprite.Visible = true;
                }
                else
                {
                    var elapsed = command!.Value.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command!.Value.Time);
                    sprite!.Alpha = (byte)Util.Round(elapsed * 0xff);

                    if (elapsed >= 1)
                        currentCommand = null;
                }
                break;
            case CommandType.FadeOutAdvancedImage:
                if (commandActivated)
                {
                    sprite!.Visible = true;
                }
                else
                {
                    var elapsed = command!.Value.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command!.Value.Time);
                    sprite!.Alpha = (byte)Util.Round(0xff - elapsed * 0xff);

                    if (elapsed >= 1)
                        currentCommand = null;
                }
                break;
        }
    }
}
