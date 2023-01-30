using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Ambermoon
{
    internal class AdvancedLogo
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

        static readonly Graphic logoAdvancedGraphic;
        readonly Queue<Command> commands;
        Command? currentCommand = null;
        DateTime currentCommandStartTime = DateTime.MaxValue;
        readonly Size frameSize = null;
        IAlphaSprite sprite = null;
        //readonly float oldVolume;
        //readonly Audio.OpenAL.AudioOutput audioOutput;
        //readonly ISong song;
        static TextureAtlasManager textureAtlasManager = null;

        static AdvancedLogo()
        {
            var logoStream = new MemoryStream(Resources.Advanced);
            var deflateStream = new DeflateStream(logoStream, CompressionMode.Decompress);
            var decompressedStream = new MemoryStream();
            deflateStream.CopyTo(decompressedStream);
            deflateStream.Dispose();
            var logoData = new DataReader(decompressedStream.ToArray());
            logoAdvancedGraphic = LoadImage(logoData);
        }

        public AdvancedLogo(/*Audio.OpenAL.AudioOutput audioOutput, ISong song*/)
        {
            //this.audioOutput = audioOutput;
            //this.song = song;
            //oldVolume = audioOutput.Volume;

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

        static Graphic LoadImage(IDataReader dataReader)
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

        public static void Initialize(TextureAtlasManager textureAtlasManager)
        {
            if (AdvancedLogo.textureAtlasManager != textureAtlasManager)
            {
                AdvancedLogo.textureAtlasManager = textureAtlasManager;

                if (!textureAtlasManager.HasLayer(Layer.Images))
                {
                    textureAtlasManager.AddFromGraphics(Layer.Images, new Dictionary<uint, Graphic>
                    {
                        { 0u, logoAdvancedGraphic }
                    });
                }
            }
        }

        /*public void StopMusic()
        {
            if (audioOutput.Enabled)
                song?.Stop();
        }

        public void PlayMusic()
        {
            if (audioOutput.Enabled)
                song?.Play(audioOutput);
        }*/

        public void Cleanup()
        {
            sprite?.Delete();
            //song.Stop();
            //audioOutput.Volume = oldVolume;
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


        void ProcessCurrentCommand(IRenderView renderView, bool commandActivated)
        {
            var command = currentCommand.Value;

            switch (command.Type)
            {
                case CommandType.Wait:
                    if ((DateTime.Now - currentCommandStartTime).TotalMilliseconds >= command.Time)
                        currentCommand = null;
                    break;
                case CommandType.FadeInAdvancedImage:
                    if (commandActivated)
                    {
                        float ratio = (float)logoAdvancedGraphic.Width / logoAdvancedGraphic.Height;
                        int height = renderView.FramebufferSize.Height;
                        int width = Util.Round(ratio * height);
                        sprite = renderView.SpriteFactory.CreateWithAlpha(width, height);
                        sprite.Layer = renderView.GetLayer(Layer.Images);
                        // Important for visibility check, otherwise the virtual screen is used!
                        sprite.ClipArea = new Rect(Position.Zero, renderView.FramebufferSize);
                        sprite.X = (renderView.FramebufferSize.Width - width) / 2;
                        sprite.Y = (renderView.FramebufferSize.Height - height) / 2;
                        sprite.TextureAtlasOffset = new Position(0, 0); // TODO: must be first image!
                        sprite.TextureSize = new Size(logoAdvancedGraphic.Width, logoAdvancedGraphic.Height);
                        sprite.Alpha = 0;
                        sprite.Visible = true;
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);
                        sprite.Alpha = (byte)Util.Round(elapsed * 0xff);

                        if (elapsed >= 1)
                            currentCommand = null;
                    }
                    break;
                case CommandType.FadeOutAdvancedImage:
                    if (commandActivated)
                    {
                        sprite.Visible = true;
                    }
                    else
                    {
                        var elapsed = command.Time == 0 ? 1.0 : Math.Min(1.0, (DateTime.Now - currentCommandStartTime).TotalMilliseconds / command.Time);
                        sprite.Alpha = (byte)Util.Round(0xff - elapsed * 0xff);

                        if (elapsed >= 1)
                            currentCommand = null;
                    }
                    break;
            }
        }
    }
}
