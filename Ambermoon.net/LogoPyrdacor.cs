using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Ambermoon
{
    internal class LogoPyrdacor
    {
        enum CommandType
        {
            Wait,
            Blend,
            Replace,
            FadeOut,
            PrintText
        }

        struct Command
        {
            public CommandType Type;
            public uint Time;
            public int ImageIndex;
            public byte[] Parameters;
        }

        public Graphic[] Palettes { get; } = new Graphic[1];
        readonly Graphic logoGraphic;
        readonly Queue<Command> commands;
        Command? currentCommand = null;
        DateTime currentCommandStartTime = DateTime.MaxValue;
        readonly Size frameSize = null;
        IAlphaSprite sprite1 = null;
        IAlphaSprite sprite2 = null;
        readonly float oldVolume;
        readonly Audio.OpenAL.AudioOutput audioOutput;
        readonly ISong song;
        TextureAtlasManager textureAtlasManager = null;
        IRenderText renderText = null;
        IColoredRect textOverlay = null;

        public LogoPyrdacor(Audio.OpenAL.AudioOutput audioOutput, ISong song)
        {
            this.audioOutput = audioOutput;
            this.song = song;
            oldVolume = audioOutput.Volume;
            var logoStream = new MemoryStream(Resources.Logo);
            var deflateStream = new DeflateStream(logoStream, CompressionMode.Decompress);
            var decompressedStream = new MemoryStream();
            deflateStream.CopyTo(decompressedStream);
            deflateStream.Dispose();
            var logoData = new DataReader(decompressedStream.ToArray());

            int commandCount = logoData.ReadByte();
            commands = new Queue<Command>(commandCount);

            for (int i = 0; i < commandCount; ++i)
            {
                uint time = logoData.ReadWord();
                var command = new Command
                {
                    Type = (CommandType)logoData.ReadByte(),
                    Time = time
                };

                if (command.Type == CommandType.Blend)
                    command.Parameters = logoData.ReadBytes(4);

                if (command.Type == CommandType.Blend || command.Type == CommandType.Replace)
                    command.ImageIndex = logoData.ReadByte();

                if (command.Type == CommandType.PrintText)
                {
                    int length = logoData.ReadByte();
                    command.Parameters = logoData.ReadBytes(length);
                }

                commands.Enqueue(command);
            }

            Palettes[0] = new Graphic
            {
                Width = 32,
                Height = 1,
                IndexedGraphic = false,
                Data = logoData.ReadBytes(32 * 4)
            };

            byte frameWidth = logoData.ReadByte();
            byte frameHeight = logoData.ReadByte();
            var dataSize = logoData.Size - logoData.Position;
            int imageWidth = dataSize / frameHeight;
            logoGraphic = new Graphic
            {
                Width = imageWidth,
                Height = frameHeight,
                IndexedGraphic = true,
                Data = logoData.ReadToEnd()
            };
            int frameCount = imageWidth / frameWidth;
            unchecked
            {
                for (int f = 1; f < frameCount; ++f)
                {
                    for (int y = 0; y < frameHeight; ++y)
                    {
                        for (int x = 0; x < frameWidth; ++x)
                        {
                            int index = y * imageWidth + f * frameWidth + x;
                            logoGraphic.Data[index] = (byte)(logoGraphic.Data[index - frameWidth] + (sbyte)logoGraphic.Data[index]);
                        }
                    }
                }
            }
            frameSize = new Size(frameWidth, frameHeight);
        }

        public void Initialize(TextureAtlasManager textureAtlasManager)
        {
            this.textureAtlasManager = textureAtlasManager;
            textureAtlasManager.AddFromGraphics(Layer.Misc, new Dictionary<uint, Graphic>
            {
                { 0u, logoGraphic }
            });
        }

        public void StopMusic()
        {
            if (audioOutput.Enabled)
                song?.Stop();
        }

        public void PlayMusic()
        {
            if (audioOutput.Enabled)
                song?.Play(audioOutput);
        }

        public void Cleanup()
        {
            textOverlay?.Delete();
            renderText?.Delete();
            sprite1?.Delete();
            sprite2?.Delete();
            song.Stop();
            audioOutput.Volume = oldVolume;
        }

        public void Update(IRenderView renderView, Action finished)
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

        void EnsureSprites(IRenderView renderView, bool firstOnly)
        {
            IAlphaSprite EnsureSprite(IAlphaSprite sprite, byte displayLayer)
            {
                sprite ??= renderView.SpriteFactory.CreateWithAlpha(frameSize.Width, frameSize.Height, displayLayer);
                sprite.Layer = renderView.GetLayer(Layer.Misc);
                sprite.X = (Global.VirtualScreenWidth - frameSize.Width) / 2;
                sprite.Y = (Global.VirtualScreenHeight - frameSize.Height) / 2;
                sprite.PaletteIndex = (byte)(renderView.GraphicProvider.FirstFantasyIntroPaletteIndex + 1);
                return sprite;
            }

            sprite1 = EnsureSprite(sprite1, 0);

            if (!firstOnly)
                sprite2 = EnsureSprite(sprite2, 10);
        }

        void EnsureText(IRenderView renderView, string text)
        {
            if (renderText == null)
            {
                var textArea = new Rect(sprite1.X, sprite1.Y + frameSize.Height + 2, frameSize.Width, Global.GlyphLineHeight);
                var emptyText = renderView.TextProcessor.CreateText(text, '?');
                var position = Global.GetTextRect(renderView, textArea).Position;
                renderText = renderView.RenderTextFactory.Create(
                    renderView.GraphicProvider.DefaultTextPaletteIndex, 
                    renderView.GetLayer(Layer.Text), emptyText, Data.Enumerations.Color.White, false);
                renderText.Place(Global.GetTextRect(renderView, textArea), TextAlign.Center);
                textOverlay = renderView.ColoredRectFactory.Create(textArea.Width, textArea.Height + 2, Color.Black, 255);
                textOverlay.X = position.X;
                textOverlay.Y = position.Y - 1;
                textOverlay.Layer = renderView.GetLayer(Layer.Misc);
            }
            else
            {
                renderText.Text = renderView.TextProcessor.CreateText(text, '?');
                textOverlay.Color = Color.Black;
            }

            renderText.Visible = true;
            textOverlay.Visible = true;
        }

        Position GetImageOffset(IRenderView renderView, int index)
        {
            int textureFactor = (int)renderView.GetLayer(Layer.Misc).TextureFactor;
            return (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Misc).GetOffset(0) + new Position(index * frameSize.Width * textureFactor, 0);
        }

        void ProcessCurrentCommand(IRenderView renderView, bool commandActivated)
        {
            var command = currentCommand.Value;

            switch (command.Type)
            {
                case CommandType.Wait:
                    if ((DateTime.Now - currentCommandStartTime).TotalMilliseconds >= command.Time)
                        currentCommand = null;
                    break;
                case CommandType.Blend:
                {
                    var startPosition = new Position(command.Parameters[0], command.Parameters[1]);
                    if (commandActivated)
                    {
                        bool noImage = sprite1 == null;
                        bool noSecondImage = sprite2 == null;
                        EnsureSprites(renderView, noImage);
                        if (noImage)
                        {
                            sprite1.Alpha = 0x00;
                            sprite1.TextureAtlasOffset = GetImageOffset(renderView, command.ImageIndex);
                            sprite1.Visible = false;
                            sprite1.ClipArea = new Rect(sprite1.X + startPosition.X, sprite1.Y + startPosition.Y, 0, 0);
                        }
                        else
                        {
                            if (!noSecondImage && sprite2 != null)
                                sprite1.TextureAtlasOffset = sprite2.TextureAtlasOffset;
                            sprite1.ClipArea = new Rect(sprite1.X, sprite1.Y, sprite1.Width, sprite1.Height);
                            sprite1.Alpha = 0xff;
                            sprite2.Alpha = 0x00;
                            sprite2.TextureAtlasOffset = GetImageOffset(renderView, command.ImageIndex);
                            sprite2.ClipArea = new Rect(sprite2.X + startPosition.X, sprite2.Y + startPosition.Y, 0, 0);
                            sprite1.Visible = true;
                            sprite2.Visible = false;
                        }
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);
                        var endPosition = new Position(command.Parameters[2], command.Parameters[3]);
                        int diffY = Util.Round((endPosition.Y - startPosition.Y) * elapsed);
                        int minY = Math.Min(startPosition.Y, startPosition.Y + diffY);
                        int maxY = Math.Max(startPosition.Y, startPosition.Y + diffY);
                        int startX = Math.Min(startPosition.X, endPosition.X);
                        int endX = Math.Max(startPosition.X, endPosition.X);

                        var sprite = sprite2 ?? sprite1;
                        sprite.Alpha = (byte)Util.Round(elapsed * 0xff);
                        sprite.ClipArea = new Rect(sprite.X + startX, sprite.Y + minY, endX - startX, maxY - minY);
                        sprite.Visible = sprite.Alpha > 0;

                        if (elapsed >= 1)
                            currentCommand = null;
                    }
                    break;
                }
                case CommandType.Replace:
                {
                    if (commandActivated)
                    {
                        bool noImage = sprite1 == null;
                        bool noSecondImage = sprite2 == null;
                        EnsureSprites(renderView, noImage);
                        if (noImage)
                        {                            
                            sprite1.TextureAtlasOffset = GetImageOffset(renderView, command.ImageIndex);
                            sprite1.Alpha = 0x00;
                            sprite1.ClipArea = new Rect(sprite1.X, sprite1.Y, sprite1.Width, sprite1.Height);
                            sprite1.Visible = false;                            
                        }
                        else
                        {
                            if (!noSecondImage && sprite2 != null)
                                sprite1.TextureAtlasOffset = sprite2.TextureAtlasOffset;
                            sprite2.TextureAtlasOffset = GetImageOffset(renderView, command.ImageIndex);
                            sprite1.Alpha = 0xff;
                            sprite2.Alpha = 0x00;                            
                            sprite1.ClipArea = new Rect(sprite1.X, sprite1.Y, sprite1.Width, sprite1.Height);
                            sprite2.ClipArea = new Rect(sprite2.X, sprite2.Y, sprite2.Width, sprite2.Height);
                            sprite1.Visible = true;
                            sprite2.Visible = false;
                        }
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);

                        if (sprite2 == null)
                        {
                            sprite1.Alpha = (byte)Util.Round(elapsed * 0xff);
                            sprite1.Visible = sprite1.Alpha > 0;
                        }
                        else
                        {
                            sprite2.Alpha = (byte)Util.Round(elapsed * 0xff);
                            sprite1.Alpha = (byte)(0xff - sprite2.Alpha);
                            sprite1.Visible = sprite1.Alpha > 0;
                            sprite2.Visible = sprite2.Alpha > 0;
                        }

                        if (elapsed >= 1)
                            currentCommand = null;
                    }
                    break;
                }
                case CommandType.FadeOut:
                    if (commandActivated)
                    {
                        EnsureSprites(renderView, true);

                        if (sprite2 != null)
                        {
                            sprite1.TextureAtlasOffset = sprite2.TextureAtlasOffset;
                            sprite2.Delete();
                            sprite2 = null;
                        }

                        sprite1.Alpha = 0xff;
                        sprite1.Visible = true;

                        if (textOverlay != null)
                        {
                            textOverlay.Color = Color.Transparent;
                            textOverlay.Visible = true;
                        }
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);
                        sprite1.Alpha = (byte)Util.Round(0xff - elapsed * 0xff);
                        sprite1.Visible = sprite1.Alpha > 0;
                        audioOutput.Volume = (float)(1.0 - elapsed) * oldVolume;

                        if (textOverlay != null)
                            textOverlay.Color = new Color((byte)0, (byte)0, (byte)0, (byte)(0xff - sprite1.Alpha));

                        if (elapsed >= 1)
                            currentCommand = null;
                    }
                    break;
                case CommandType.PrintText:
                    if (commandActivated)
                    {
                        string text = System.Text.Encoding.UTF8.GetString(command.Parameters);
                        EnsureText(renderView, text);
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);
                        textOverlay.Color = new Color((byte)0, (byte)0, (byte)0, (byte)Util.Round(0xff - elapsed * 0xff));

                        if (elapsed >= 1)
                        {
                            textOverlay.Visible = false;
                            currentCommand = null;
                        }
                    }
                    break;
            }
        }
    }
}
