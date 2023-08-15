using Ambermoon.Audio.OpenAL;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Ambermoon.UI;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MousePosition = System.Numerics.Vector2;
using WindowDimension = Silk.NET.Maths.Vector2D<int>;

namespace Ambermoon
{
    class GameWindow : IContextProvider
    {
        string gameVersion = "Ambermoon.net";
        Configuration configuration;
        RenderView renderView;
        IWindow window;
        IKeyboard keyboard = null;
        IMouse mouse = null;
        ICursor cursor = null;
        MainMenu mainMenu = null;
        Func<Game> gameCreator = null;
        MusicManager musicManager = null;
        AudioOutput audioOutput = null;
        IRenderText infoText = null;
        IFontProvider fontProvider = null;
        DateTime? initializeErrorTime = null;
        List<Size> availableFullscreenModes = null;
        DateTime lastRenderTime = DateTime.MinValue;
        TimeSpan lastRenderDuration = TimeSpan.Zero;
        bool trapMouse = false;
        FloatPosition trappedMouseOffset = null;
        FloatPosition trappedMouseLastPosition = null;
        FantasyIntro fantasyIntro = null;
        LogoPyrdacor logoPyrdacor = null;
        AdvancedLogo advancedLogo = null;
        Graphic[] additionalPalettes;
        bool initialized = false;
        Patcher patcher = null;
        bool checkPatcher = true;
        bool initialIntroEndedByClick = false;

        public string Identifier { get; }
        public IGLContext GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }
        VersionSelector versionSelector = null;
        Intro intro = null;
        public Game Game { get; private set; }
        public bool Fullscreen
        {
            get => configuration.Fullscreen;
            set
            {
                configuration.Fullscreen = value;
                window.WindowState = configuration.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;

                if (cursor != null)
                    cursor.CursorMode = CursorMode.Hidden;
            }
        }

        public GameWindow(string id = "MainWindow")
        {
            Identifier = id;
        }

        void ChangeResolution(int? oldWidth) => ChangeResolution(oldWidth, configuration.Fullscreen, true);

        void ChangeResolution(int? oldWidth, bool fullscreen, bool changed)
        {
            if (renderView == null || configuration == null)
                return;

            if (fullscreen)
            {
                var fullscreenSize = renderView.AvailableFullscreenModes.OrderBy(r => r.Width * r.Height).LastOrDefault();

                if (fullscreenSize != null)
                {
                    configuration.FullscreenWidth = fullscreenSize.Width;
                    configuration.FullscreenHeight = fullscreenSize.Height;
                }
            }
            else
            {
                var possibleResolutions = ScreenResolutions.GetPossibleResolutions(renderView.MaxScreenSize);
                int index = oldWidth == null ? 0 : changed
                    ? (possibleResolutions.FindIndex(r => r.Width == oldWidth.Value) + 1) % possibleResolutions.Count
                    : FindNearestResolution(oldWidth.Value);
                var resolution = possibleResolutions[index];
                configuration.Width = resolution.Width;
                configuration.Height = resolution.Height;

                int FindNearestResolution(int width)
                {
                    int index = possibleResolutions.FindIndex(r => r.Width == width);

                    if (index != -1)
                        return index;

                    int minDiffIndex = 0;
                    int minDiff = Math.Abs(possibleResolutions[0].Width - width);

                    for (int i = 1; i < possibleResolutions.Count; ++i)
                    {
                        int diff = Math.Abs(possibleResolutions[i].Width - width);

                        if (diff < minDiff)
                        {
                            minDiffIndex = i;
                            minDiff = diff;
                        }
                    }

                    return minDiffIndex;
                }
            }
        }

        static void RunTask(Action task)
        {
            Task.Factory.StartNew(task, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        void ChangeFullscreenMode(bool fullscreen)
        {
            FullscreenChangeRequest(fullscreen);
            Fullscreen = fullscreen;
            UpdateWindow(configuration);
        }

        void FullscreenChangeRequest(bool fullscreen)
        {
            ChangeResolution(configuration.Width, fullscreen, false);
        }

        void SetupInput(IInputContext inputContext)
        {
            keyboard = inputContext.Keyboards.FirstOrDefault(k => k.IsConnected);

            if (keyboard != null)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
                keyboard.KeyUp += Keyboard_KeyUp;
                keyboard.KeyChar += Keyboard_KeyChar;
            }

            mouse = inputContext.Mice.FirstOrDefault(m => m.IsConnected);

            if (mouse != null)
            {
                cursor = mouse.Cursor;
                cursor.CursorMode = CursorMode.Hidden;
                mouse.MouseDown += Mouse_MouseDown;
                mouse.MouseUp += Mouse_MouseUp;
                mouse.MouseMove += Mouse_MouseMove;
                mouse.Scroll += Mouse_Scroll;
            }
        }

        static KeyModifiers GetModifiers(IKeyboard keyboard)
        {
            var modifiers = KeyModifiers.None;

            if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftRight))
                modifiers |= KeyModifiers.Shift;
            if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight))
                modifiers |= KeyModifiers.Control;
            if (keyboard.IsKeyPressed(Silk.NET.Input.Key.AltLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.AltRight))
                modifiers |= KeyModifiers.Alt;

            return modifiers;
        }

        List<Key> QueryPressedKeys()
            => keyboard?.SupportedKeys.Where(key => keyboard.IsKeyPressed(key)).Select(ConvertKey).ToList();

        static Key ConvertKey(Silk.NET.Input.Key key) => key switch
        {
            Silk.NET.Input.Key.Left => Key.Left,
            Silk.NET.Input.Key.Right => Key.Right,
            Silk.NET.Input.Key.Up => Key.Up,
            Silk.NET.Input.Key.Down => Key.Down,
            Silk.NET.Input.Key.Escape => Key.Escape,
            Silk.NET.Input.Key.F1 => Key.F1,
            Silk.NET.Input.Key.F2 => Key.F2,
            Silk.NET.Input.Key.F3 => Key.F3,
            Silk.NET.Input.Key.F4 => Key.F4,
            Silk.NET.Input.Key.F5 => Key.F5,
            Silk.NET.Input.Key.F6 => Key.F6,
            Silk.NET.Input.Key.F7 => Key.F7,
            Silk.NET.Input.Key.F8 => Key.F8,
            Silk.NET.Input.Key.F9 => Key.F9,
            Silk.NET.Input.Key.F10 => Key.F10,
            Silk.NET.Input.Key.F11 => Key.F11,
            Silk.NET.Input.Key.F12 => Key.F12,
            Silk.NET.Input.Key.Enter => Key.Return,
            Silk.NET.Input.Key.KeypadEnter => Key.Return,
            Silk.NET.Input.Key.Delete => Key.Delete,
            Silk.NET.Input.Key.Backspace => Key.Backspace,
            Silk.NET.Input.Key.Tab => Key.Tab,
            Silk.NET.Input.Key.Keypad0 => Key.Num0,
            Silk.NET.Input.Key.Keypad1 => Key.Num1,
            Silk.NET.Input.Key.Keypad2 => Key.Num2,
            Silk.NET.Input.Key.Keypad3 => Key.Num3,
            Silk.NET.Input.Key.Keypad4 => Key.Num4,
            Silk.NET.Input.Key.Keypad5 => Key.Num5,
            Silk.NET.Input.Key.Keypad6 => Key.Num6,
            Silk.NET.Input.Key.Keypad7 => Key.Num7,
            Silk.NET.Input.Key.Keypad8 => Key.Num8,
            Silk.NET.Input.Key.Keypad9 => Key.Num9,
            Silk.NET.Input.Key.PageUp => Key.PageUp,
            Silk.NET.Input.Key.PageDown => Key.PageDown,
            Silk.NET.Input.Key.Home => Key.Home,
            Silk.NET.Input.Key.End => Key.End,
            Silk.NET.Input.Key.Space => Key.Space,
            Silk.NET.Input.Key.W => Key.W,
            Silk.NET.Input.Key.A => Key.A,
            Silk.NET.Input.Key.S => Key.S,
            Silk.NET.Input.Key.D => Key.D,
            Silk.NET.Input.Key.Q => Key.Q,
            Silk.NET.Input.Key.E => Key.E,
            Silk.NET.Input.Key.M => Key.M,
            Silk.NET.Input.Key.Number0 => Key.Number0,
            Silk.NET.Input.Key.Number1 => Key.Number1,
            Silk.NET.Input.Key.Number2 => Key.Number2,
            Silk.NET.Input.Key.Number3 => Key.Number3,
            Silk.NET.Input.Key.Number4 => Key.Number4,
            Silk.NET.Input.Key.Number5 => Key.Number5,
            Silk.NET.Input.Key.Number6 => Key.Number6,
            Silk.NET.Input.Key.Number7 => Key.Number7,
            Silk.NET.Input.Key.Number8 => Key.Number8,
            Silk.NET.Input.Key.Number9 => Key.Number9,
            _ => Key.Invalid,
        };

        void Keyboard_KeyChar(IKeyboard keyboard, char keyChar)
        {
            if (versionSelector != null)
                versionSelector.OnKeyChar(keyChar);
            else if (Game != null)
                Game.OnKeyChar(keyChar);
        }

        void Keyboard_KeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int value)
        {
            if (key == Silk.NET.Input.Key.C && GetModifiers(keyboard) == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                var monitor = Silk.NET.Windowing.Window.GetWindowPlatform(false)?.GetMainMonitor() ?? null;
                window.Center(monitor);
            }
            else if (key == Silk.NET.Input.Key.F11)
            {
                if (Game != null)
                    Game.PreFullscreenChanged();

                // This can happen while a mouse trap is active in-game. Otherwise a fullscreen
                // change can only happen from the options menu where mouse trapping can't be active.
                ChangeFullscreenMode(!Fullscreen);

                if (Game != null)
                    Game.PostFullscreenChanged();
            }
            else if (key == Silk.NET.Input.Key.F7)
            {
                if (!Game.BattleRoundActive)
                {
                    if (GetModifiers(keyboard) == KeyModifiers.None)
                        configuration.BattleSpeed = configuration.BattleSpeed >= 100 ? 0 : configuration.BattleSpeed + 10;
                    else
                        configuration.BattleSpeed = configuration.BattleSpeed <= 0 ? 100 : configuration.BattleSpeed - 10;

                    Game?.ExternalBattleSpeedChanged();
                }
            }
            else if (key == Silk.NET.Input.Key.F8)
            {
                if (GetModifiers(keyboard) == KeyModifiers.None)
                    configuration.GraphicFilter = (GraphicFilter)(((int)configuration.GraphicFilter + 1) % Enum.GetValues<GraphicFilter>().Length);
                else
                    configuration.GraphicFilter = (GraphicFilter)(((int)configuration.GraphicFilter - 1 + Enum.GetValues<GraphicFilter>().Length) % Enum.GetValues<GraphicFilter>().Length);

                if (!renderView.TryUseFrameBuffer())
                    configuration.GraphicFilter = GraphicFilter.None;

                Game?.ExternalGraphicFilterChanged();
            }
            else if (key == Silk.NET.Input.Key.F9)
            {
                if (GetModifiers(keyboard) == KeyModifiers.None)
                    configuration.GraphicFilterOverlay = (GraphicFilterOverlay)(((int)configuration.GraphicFilterOverlay + 1) % Enum.GetValues<GraphicFilterOverlay>().Length);
                else
                    configuration.GraphicFilterOverlay = (GraphicFilterOverlay)(((int)configuration.GraphicFilterOverlay - 1 + Enum.GetValues<GraphicFilterOverlay>().Length) % Enum.GetValues<GraphicFilterOverlay>().Length);

                if (!renderView.TryUseFrameBuffer())
                    configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;

                Game?.ExternalGraphicFilterOverlayChanged();
            }
            else if (key == Silk.NET.Input.Key.F10)
            {
                if (GetModifiers(keyboard) == KeyModifiers.None)
                    configuration.Effects = (Effects)(((int)configuration.Effects + 1) % Enum.GetValues<Effects>().Length);
                else
                    configuration.Effects = (Effects)(((int)configuration.Effects - 1 + Enum.GetValues<Effects>().Length) % Enum.GetValues<Effects>().Length);

                if (!renderView.TryUseEffects())
                    configuration.Effects = Effects.None;

                Game?.ExternalEffectsChanged();
            }
            else if (key == Silk.NET.Input.Key.M && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
                 keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
            {
                configuration.Music = !configuration.Music;
                audioOutput.Enabled = configuration.Music;
                if (audioOutput.Available && audioOutput.Enabled)
                    Game?.ContinueMusic();
                Game?.ExternalMusicChanged();
            }
            else if (key == Silk.NET.Input.Key.Comma && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
                 keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
            {
                if (configuration.Volume >= 10)
                {
                    configuration.Volume -= 10;
                    audioOutput.Volume = configuration.Volume / 100.0f;
                    Game?.ExternalVolumeChanged();
                }
                else if (configuration.Volume > 0)
                {
                    configuration.Volume = 0;
                    audioOutput.Volume = configuration.Volume / 100.0f;
                    Game?.ExternalVolumeChanged();
                }
            }
            else if (key == Silk.NET.Input.Key.Period && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
                 keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
            {
                if (configuration.Volume <= 90)
                {
                    configuration.Volume += 10;
                    audioOutput.Volume = configuration.Volume / 100.0f;
                    Game?.ExternalVolumeChanged();
                }
                else if (configuration.Volume < 100)
                {
                    configuration.Volume = 100;
                    audioOutput.Volume = configuration.Volume / 100.0f;
                    Game?.ExternalVolumeChanged();
                }
            }
            else if (renderView != null && (key == Silk.NET.Input.Key.PrintScreen ||
                (key == Silk.NET.Input.Key.P && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
                 keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))))
            {
                renderView.TakeScreenshot(imageData =>
                {
                    string directory = Path.Combine(Configuration.BundleDirectory, "Screenshots");
                    string path;
                    static string GetFileName() => "Screenshot_" + DateTime.Now.ToString("dd-MM-yyyy.HH-mm-ss");
                    try
                    {
                        Directory.CreateDirectory(directory);
                        path = Path.Combine(directory, GetFileName());
                    }
                    catch
                    {
                        directory = Path.Combine(Configuration.FallbackConfigDirectory, "Screenshots");

                        try
                        {
                            Directory.CreateDirectory(directory);
                            path = Path.Combine(directory, GetFileName());
                        }
                        catch
                        {
                            path = Path.Combine(Path.GetTempPath(), GetFileName());
                        }
                    }
                    try
                    {
                        WritePNG(path, imageData, renderView.FramebufferSize, false, true);
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to create screenshot at '{path}'.");
                    }
                });
            }
            else
            {
                if (logoPyrdacor != null)
                {
                    logoPyrdacor?.Cleanup();
                    logoPyrdacor = null;
                }
                else if (fantasyIntro != null)
                {
                    fantasyIntro.Abort();
                }
                else if (advancedLogo != null)
                {
                    advancedLogo?.Cleanup();
                    advancedLogo = null;
                }
                else if (versionSelector != null)
                    versionSelector.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
                else if (intro != null && key == Silk.NET.Input.Key.Escape)
                    intro.Click();
                else
                    Game?.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
            }
        }

        void Keyboard_KeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int value)
        {
            if (versionSelector != null)
                versionSelector.OnKeyUp(ConvertKey(key), GetModifiers(keyboard));
            else if (Game != null)
                Game.OnKeyUp(ConvertKey(key), GetModifiers(keyboard));
        }

        static MouseButtons GetMouseButtons(IMouse mouse)
        {
            var buttons = MouseButtons.None;

            if (mouse.IsButtonPressed(MouseButton.Left))
                buttons |= MouseButtons.Left;
            if (mouse.IsButtonPressed(MouseButton.Right))
                buttons |= MouseButtons.Right;
            if (mouse.IsButtonPressed(MouseButton.Middle))
                buttons |= MouseButtons.Middle;

            return buttons;
        }

        static MouseButtons ConvertMouseButtons(MouseButton mouseButton)
        {
            return mouseButton switch
            {
                MouseButton.Left => MouseButtons.Left,
                MouseButton.Right => MouseButtons.Right,
                MouseButton.Middle => MouseButtons.Middle,
                _ => MouseButtons.None
            };
        }

        static Position ConvertMousePosition(MousePosition position)
        {
            return new Position(Util.Round(position.X), Util.Round(position.Y));
        }

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            if (patcher != null)
                patcher.OnMouseDown(ConvertMousePosition(position), ConvertMouseButtons(button));
            else if (logoPyrdacor != null)
            {
                logoPyrdacor?.Cleanup();
                logoPyrdacor = null;
            }
            else if (fantasyIntro != null)
            {
                fantasyIntro.Abort();
            }
            else if (advancedLogo != null)
            {
                advancedLogo?.Cleanup();
                advancedLogo = null;
            }
            else if (versionSelector != null)
                versionSelector.OnMouseDown(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (mainMenu != null)
                mainMenu.OnMouseDown(ConvertMousePosition(position), ConvertMouseButtons(button));
            else if (intro != null)
                intro.Click();
            else
                Game?.OnMouseDown(ConvertMousePosition(position), GetMouseButtons(mouse), GetModifiers(keyboard));
        }

        void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            if (patcher != null)
                patcher.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
            else if (versionSelector != null)
                versionSelector.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
            else if (mainMenu != null)
                mainMenu.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
            else if (Game != null)
                Game.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
        }

        void Mouse_MouseMove(IMouse mouse, MousePosition position)
        {
            if (trapMouse && mouse != null)
            {
                mouse.MouseMove -= Mouse_MouseMove;
                trappedMouseOffset.X += position.X - trappedMouseLastPosition.X;
                trappedMouseOffset.Y += position.Y - trappedMouseLastPosition.Y;
                mouse.Position = new MousePosition(window.Size.X / 2, window.Size.Y / 2);
                position = new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y);
                mouse.MouseMove += Mouse_MouseMove;
            }

            if (patcher != null)
                patcher.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (versionSelector != null)
                versionSelector.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (mainMenu != null)
                mainMenu.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (Game != null)
                Game.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
        }

        void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            if (versionSelector != null)
                versionSelector.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), ConvertMousePosition(position));
            else if (Game != null)
                Game.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), ConvertMousePosition(position));
        }

        static void WritePNG(string filename, byte[] rgbData, Size imageSize, bool alpha, bool upsideDown)
        {
            if (File.Exists(filename))
                filename += Guid.NewGuid().ToString();

            filename += ".png";

            int bpp = alpha ? 4 : 3;
            var writer = new DataWriter();

            void WriteChunk(string name, Action<DataWriter> dataWriter)
            {
                var internalDataWriter = new DataWriter();
                dataWriter?.Invoke(internalDataWriter);
                var data = internalDataWriter.ToArray();

                writer.Write((uint)data.Length);
                writer.WriteWithoutLength(name);
                writer.Write(data);
                var crc = new PngCrc();
                uint headerCrc = crc.Calculate(new byte[] { (byte)name[0], (byte)name[1], (byte)name[2], (byte)name[3] });
                writer.Write(crc.Calculate(headerCrc, data));
            }

            // Header
            writer.Write(0x89);
            writer.Write(0x50);
            writer.Write(0x4E);
            writer.Write(0x47);
            writer.Write(0x0D);
            writer.Write(0x0A);
            writer.Write(0x1A);
            writer.Write(0x0A);

            // IHDR chunk
            WriteChunk("IHDR", writer =>
            {
                writer.Write((uint)imageSize.Width);
                writer.Write((uint)imageSize.Height);
                writer.Write(8); // 8 bits per color
                writer.Write((byte)(alpha ? 6 : 2)); // With alpha (RGBA) or color only (RGB)
                writer.Write(0); // Deflate compression
                writer.Write(0); // Default filtering
                writer.Write(0); // No interlace
            });

            WriteChunk("IDAT", writer =>
            {
                byte[] dataWithFilterBytes = new byte[rgbData.Length + imageSize.Height];
                for (int y = 0; y < imageSize.Height; ++y)
                {
                    int i = upsideDown ? imageSize.Height - y - 1 : y;
                    Buffer.BlockCopy(rgbData, y * imageSize.Width * bpp, dataWithFilterBytes, 1 + i + i * imageSize.Width * bpp, imageSize.Width * bpp);
                }
                // Note: Data is initialized with 0 bytes so the filter bytes are already 0.
                using var uncompressedStream = new MemoryStream(dataWithFilterBytes);
                using var compressedStream = new MemoryStream();
                var compressStream = new System.IO.Compression.DeflateStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal, true);
                uncompressedStream.CopyTo(compressStream);
                compressStream.Close();

                // Zlib header
                writer.Write(0x78); // 32k window deflate method
                writer.Write(0xDA); // Best compression, no dict and header is multiple of 31

                uint Adler32()
                {
                    uint s1 = 1;
                    uint s2 = 0;

                    for (int n = 0; n < dataWithFilterBytes.Length; ++n)
                    {
                        s1 = (s1 + dataWithFilterBytes[n]) % 65521;
                        s2 = (s2 + s1) % 65521;
                    }

                    return (s2 << 16) | s1;
                }

                // Compressed data
                writer.Write(compressedStream.ToArray());

                // Checksum
                writer.Write(Adler32());
            });

            // IEND chunk
            WriteChunk("IEND", null);

            using var file = File.Create(filename);
            writer.CopyTo(file);
        }

        void ShowMainMenu(IRenderView renderView, Render.Cursor cursor, bool fromIntro, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            Font introFont, string[] mainMenuTexts, bool canContinue, Action<bool> startGameAction, GameLanguage gameLanguage,
            Action showIntroAction)
        {
            renderView.PaletteFading = null; // Reset palette fading

            void PlayMusic(Data.Enumerations.Song song)
            {
                if (configuration.Music)
                    musicManager.GetSong(song)?.Play(audioOutput);

                if (infoText != null)
                    infoText.Visible = false;
            }

            mainMenu = new MainMenu(renderView, cursor, paletteIndices, introFont, mainMenuTexts, canContinue,
                GetText(gameLanguage, 0), GetText(gameLanguage, 1), PlayMusic, fromIntro);
            mainMenu.Closed += closeAction =>
            {
                switch (closeAction)
                {
                    case MainMenu.CloseAction.NewGame:
                        startGameAction?.Invoke(false);
                        break;
                    case MainMenu.CloseAction.Continue:
                        // Someone who has savegames won't need any introduction, so set this to false.
                        configuration.FirstStart = false;
                        startGameAction?.Invoke(true);
                        break;
                    case MainMenu.CloseAction.Intro:
                        showIntroAction?.Invoke();
                        break;
                    case MainMenu.CloseAction.Exit:
                        mainMenu?.Destroy();
                        mainMenu = null;
                        window.Close();
                        break;
                    default:
                        throw new AmbermoonException(ExceptionScope.Application, "Invalid main menu close action.");
                }
            };
        }

        static readonly Dictionary<GameLanguage, string[]> LoadingTexts = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
                {
                    "Starte Spiel ...",
                    "Bereite neues Spiel vor ..."
                }
            },
            { GameLanguage.English, new string[]
                {
                    "Starting game ...",
                    "Preparing new game ..."
                }
            },
            { GameLanguage.French, new string[]
                {
                    "Démarrage du jeu ...",
                    "Démarrage un nouveau jeu ..."
                }
            },
            { GameLanguage.Polish, new string[]
                {
                    "Rozpoczynanie gry ...",
                    "Przygotowanie nowej gry ..."
                }
            }
        };

        string GetText(GameLanguage gameLanguage, int index) => LoadingTexts[gameLanguage][index];

        void StartGame(IGameData gameData, string savePath, GameLanguage gameLanguage, Features features, BinaryReader advancedDiffsReader)
        {
            // Load fantasy intro data
            var fantasyIntroData = gameData.FantasyIntroData;

            // Load intro data
            var introData = gameData.IntroData;
            var introFont = new Font(introData.Glyphs, 6, 0);
            var introFontLarge = new Font(introData.LargeGlyphs, 10, (uint)introData.Glyphs.Count);

            // Load outro data
            var outroData = gameData.OutroData;
            var outroFont = new Font(outroData.Glyphs, 6, 0);
            var outroFontLarge = new Font(outroData.LargeGlyphs, 10, (uint)outroData.Glyphs.Count);

            // Load game data
            var graphicProvider = gameData.GraphicProvider;

            if (audioOutput == null)
            {
                audioOutput = new AudioOutput();
                audioOutput.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
                audioOutput.Enabled = audioOutput.Available && configuration.Music;
                if (configuration.ShowPyrdacorLogo)
                {
                    logoPyrdacor = new LogoPyrdacor(audioOutput, SongManager.LoadCustomSong(new DataReader(Resources.Song), 0, false, false));
                    additionalPalettes = logoPyrdacor.Palettes;
                }
                else
                {
                    additionalPalettes = new Graphic[1] { new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] } };
                }
            }

            if (gameData.Advanced)
                advancedLogo = new AdvancedLogo(); // TODO: later add it to options

            musicManager = new MusicManager(configuration, gameData);
            fontProvider ??= new IngameFontProvider(new DataReader(Resources.IngameFont), gameData.FontProvider.GetFont());

            // Create render view
            renderView = CreateRenderView(gameData, configuration, graphicProvider, fontProvider, additionalPalettes, () =>
            {
                var textureAtlasManager = TextureAtlasManager.Instance;
                var introGraphics = introData.Graphics.ToDictionary(g => (uint)g.Key, g => g.Value);
                uint twinlakeFrameOffset = (uint)introData.Graphics.Keys.Max();
                foreach (var twinlakeImagePart in introData.TwinlakeImageParts)
                    introGraphics.Add(++twinlakeFrameOffset, twinlakeImagePart.Graphic);
                textureAtlasManager.AddAll(gameData, graphicProvider, fontProvider, introFont.GlyphGraphics,
                    introFontLarge.GlyphGraphics, introGraphics, features);
                logoPyrdacor?.Initialize(textureAtlasManager);
                AdvancedLogo.Initialize(textureAtlasManager);
                return textureAtlasManager;
            });
            renderView.AvailableFullscreenModes = availableFullscreenModes;
            renderView.SetTextureFactor(Layer.Text, 2);

            if (configuration.ShowFantasyIntro)
            {
                fantasyIntro = new FantasyIntro(renderView, fantasyIntroData, () =>
                {
                    fantasyIntro = null;

                    if (configuration.ShowIntro)
                        ShowIntro(byClick => initialIntroEndedByClick = byClick, introData, introFont, introFontLarge);
                });
            }
            else if (configuration.ShowIntro)
            {
                ShowIntro(byClick => initialIntroEndedByClick = byClick, introData, introFont, introFontLarge);
            }

            InitGlyphs(fontProvider);

            var text = renderView.TextProcessor.CreateText("");
            infoText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text), text, Data.Enumerations.Color.White, false,
                Global.GetTextRect(renderView, new Rect(0, Global.VirtualScreenHeight / 2 - 3, Global.VirtualScreenWidth, 6)), TextAlign.Center);
            infoText.DisplayLayer = 254;
            infoText.Visible = false;

            RunTask(() =>
            {
                try
                {
                    var savegameManager = new SavegameManager(savePath);
                    savegameManager.GetSavegameNames(gameData, out int currentSavegame, 10);
                    if (currentSavegame == 0 && configuration.ExtendedSavegameSlots)
                        currentSavegame = configuration.GetOrCreateCurrentAdditionalSavegameSlots(Path.GetFileName(savePath))?.ContinueSavegameSlot ?? 0;
                    bool canContinue = currentSavegame != 0;
                    var cursor = new Render.Cursor(renderView, gameData.CursorHotspots);
                    cursor.UpdatePosition(ConvertMousePosition(mouse.Position), null);
                    cursor.Type = Data.CursorType.None;

                    void SetupGameCreator(bool continueGame)
                    {
                        try
                        {
                            var savegameSerializer = new SavegameSerializer();

                            gameCreator = () =>
                            {
                                var game = new Game(configuration, gameLanguage, renderView, graphicProvider,
                                    savegameManager, savegameSerializer, gameData.Dictionary, cursor, audioOutput,
                                    musicManager, FullscreenChangeRequest, ChangeResolution, QueryPressedKeys,
                                    new OutroFactory(renderView, outroData, outroFont, outroFontLarge), features,
                                    Path.GetFileName(savePath));
                                game.QuitRequested += window.Close;
                                game.MousePositionChanged += position =>
                                {
                                    if (mouse != null)
                                    {
                                        mouse.MouseMove -= Mouse_MouseMove;
                                        mouse.Position = new MousePosition(position.X, position.Y);
                                        mouse.MouseMove += Mouse_MouseMove;
                                    }
                                };
                                game.MouseTrappedChanged += (bool trapped, Position position) =>
                                {
                                    try
                                    {
                                        this.cursor.CursorMode = trapped ? CursorMode.Disabled : CursorMode.Hidden;
                                        trapMouse = false;
                                    }
                                    catch
                                    {
                                        // SDL etc needs special logic as CursorMode.Disabled is not available
                                        trapMouse = trapped;
                                        trappedMouseOffset = trapped ? new FloatPosition(position) : null;
                                        trappedMouseLastPosition = trapped ? new FloatPosition(window.Size.X / 2, window.Size.Y / 2) : null;
                                        this.cursor.CursorMode = CursorMode.Hidden;
                                    }
                                    if (mouse != null)
                                    {
                                        mouse.MouseMove -= Mouse_MouseMove;
                                        mouse.Position = !trapped || !trapMouse ? new MousePosition(position.X, position.Y) :
                                            new MousePosition(window.Size.X / 2, window.Size.Y / 2);
                                        mouse.MouseMove += Mouse_MouseMove;
                                    }
                                };
                                game.ConfigurationChanged += (configuration, windowChange) =>
                                {
                                    if (windowChange)
                                    {
                                        ChangeFullscreenMode(configuration.Fullscreen);
                                    }

                                    if (!renderView.TryUseFrameBuffer())
                                    {
                                        configuration.GraphicFilter = GraphicFilter.None;
                                        configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;
                                    }

                                    if (!renderView.TryUseEffects())
                                        configuration.Effects = Effects.None;

                                    if (configuration.EnableCheats)
                                        PrintCheatConsoleHeader();

                                    if (configuration.EnableCheats && !Console.IsInputRedirected)
                                    {
                                        while (Console.KeyAvailable)
                                            Console.ReadKey(true);
                                    }
                                    else if (!configuration.EnableCheats && !Console.IsInputRedirected)
                                    {
                                        cheatHeaderPrinted = false;
                                        if (!Console.IsOutputRedirected)
                                            Console.Clear();
                                    }
                                };
                                game.DrugTicked += Drug_Ticked;
                                mainMenu.GameDataLoaded = true;

                                AdvancedSavegamePatcher advancedSavegamePatcher = null;

                                // Load advanced diffs (is null for non-advanced versions)
                                if (advancedDiffsReader != null)
                                    advancedSavegamePatcher = new AdvancedSavegamePatcher(advancedDiffsReader);

                                game.RequestAdvancedSavegamePatching += (gameData, saveSlot, sourceEpisode, targetEpisode) =>
                                {
                                    if (advancedSavegamePatcher == null)
                                        throw new AmbermoonException(ExceptionScope.Data, "No diff information for old Ambermoon Advanced savegame found.");

                                    advancedSavegamePatcher.PatchSavegame(gameData, saveSlot, sourceEpisode, targetEpisode);
                                };

                                game.Run(continueGame, ConvertMousePosition(mouse.Position));
                                return game;
                            };
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error while preparing game: " + ex.ToString());
                            gameCreator = () => throw new AmbermoonException(ExceptionScope.Application, "Game preparation failed.");
                        }
                    }

                    while (logoPyrdacor != null)
                        Thread.Sleep(100);

                    while (fantasyIntro != null)
                        Thread.Sleep(100);

                    while (intro != null)
                        Thread.Sleep(100);

                    while (advancedLogo != null)
                        Thread.Sleep(100);

                    void ShowMainMenu(bool fromIntro)
                    {
                        this.ShowMainMenu(renderView, cursor, fromIntro, IntroData.GraphicPalettes, introFontLarge,
                            introData.Texts.Skip(8).Take(4).Select(t => t.Value).ToArray(), canContinue, continueGame =>
                            {
                                cursor.Type = Data.CursorType.None;
                                mainMenu.FadeOutAndDestroy(continueGame, () => RunTask(() => SetupGameCreator(continueGame)));
                            }, gameLanguage, () =>
                            {
                                cursor.Type = Data.CursorType.None;
                                ShowIntro(byClick => ShowMainMenu(!byClick), introData, introFont, introFontLarge);
                            });
                    }

                    ShowMainMenu(configuration.ShowIntro && !initialIntroEndedByClick);
                }
                catch (Exception ex)
                {
                    string error = "Error while loading data: " + ex.Message;
                    Console.WriteLine(error);                   

                    try
                    {
                        error = @"Error loading data   \(o_o\)";
                        if (ex is FileNotFoundException fileNotFoundException &&
                            fileNotFoundException.Source == "Silk.NET.Core")
                        {
                            var missingLibrary = fileNotFoundException?.FileName;

                            if (missingLibrary != null)
                            {
                                error = @$"Error: {Path.GetFileName(missingLibrary)} is missing   (/o_o)/";

                                if (missingLibrary.ToLower().Contains("openal"))
                                    error += $"^Please install OpenAL manually"; // ^ is new line
                            }
                        }

                        int height = 6 + error.Count(ch => ch == '^') * 7;
                        var text = renderView.TextProcessor.CreateText(error, '_');
                        text = renderView.TextProcessor.WrapText(text, new Rect(0, 0, Global.VirtualScreenWidth, height), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
                        infoText.Text = text;
                        infoText.Place(new Rect(Math.Max(0, (Global.VirtualScreenWidth - infoText.Width) / 2), infoText.Y, infoText.Text.MaxLineSize * 6, height));
                        infoText.Visible = true;
                        initializeErrorTime = DateTime.Now;
                    }
                    catch
                    {
                        window.Close();
                    }
                }
            });
        }

        void ShowIntro(Action<bool> showMainMenuAction, IIntroData introData, Font introFont, Font introFontLarge)
        {
            mainMenu?.Destroy();
            mainMenu = null;
            if (configuration.Music)
                musicManager.Stop();

            intro = new Intro(renderView, introData, introFont, introFontLarge, byClick =>
            {
                intro = null;
                showMainMenuAction?.Invoke(byClick);
            }, () =>
            {
                if (configuration.Music)
                    musicManager.GetSong(Data.Enumerations.Song.Intro)?.Play(audioOutput);
            });
        }

        bool ShowVersionSelector(BinaryReader builtinVersionReader, Action<IGameData, string, GameLanguage, Features> selectHandler, out TextureAtlasManager createdTextureAtlasManager)
        {
            createdTextureAtlasManager = null;
            List<BuiltinVersion> versions = null;

            if (builtinVersionReader != null)
            {
                var versionLoader = new BuiltinVersionLoader();
                versions = versionLoader.Load(builtinVersionReader);
            }

            var gameData = new GameData();
            var dataPath = configuration.UseDataPath ? configuration.DataPath : Configuration.BundleDirectory;

            if (versions == null || versions.Count == 0)
            {
                // no versions
                configuration.GameVersionIndex = 0;
                gameData.Load(dataPath);
                selectHandler?.Invoke(gameData, Configuration.GetSavePath(Configuration.ExternalSavegameFolder), gameData.Language.ToGameLanguage(),
                    gameData.Advanced ? Features.AmbermoonAdvanced : Features.None);
                return false;
            }

            GameData LoadBuiltinVersionData(BuiltinVersion builtinVersion, Func<ILegacyGameData> fallbackGameDataProvider)
            {
                var gameData = new GameData();
                builtinVersion.SourceStream.Position = builtinVersion.Offset;
                var buffer = new byte[(int)builtinVersion.Size];
                builtinVersion.SourceStream.Read(buffer, 0, buffer.Length);
                var tempStream = new MemoryStream(buffer);
                gameData.LoadFromMemoryZip(tempStream, fallbackGameDataProvider);
                return gameData;
            }

            GameData LoadGameDataFromDataPath()
            {
                var gameData = new GameData();
                gameData.Load(dataPath);
                return gameData;
            }

            if (configuration.GameVersionIndex < 0 || configuration.GameVersionIndex > versions.Count) // == versions.Count is ok as it could be external data
                configuration.GameVersionIndex = 0;

            GameData.GameDataInfo? additionalVersionInfo = null;

            try
            {
                additionalVersionInfo = GameData.GetInfo(dataPath);
            }
            catch
            {
                if (configuration.GameVersionIndex == versions.Count)
                    configuration.GameVersionIndex = 0;
            }

            // Some versions merge with another one. Here only the basis versions are stored.
            var baseVersionIndices = versions.Select((version, index) => new { version, index }).Where(v => !v.version.MergeWithPrevious).Select(v => v.index).ToList();

            if (configuration.GameVersionIndex < versions.Count)
            {
                Func<ILegacyGameData> fallbackGameDataProvider = baseVersionIndices.Contains(configuration.GameVersionIndex)
                    ? null
                    : () => LoadBuiltinVersionData(versions[baseVersionIndices.Last(idx => idx < configuration.GameVersionIndex)], null);
                gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex], fallbackGameDataProvider);
            }
            else
            {
                try
                {
                    gameData = LoadGameDataFromDataPath();
                }
                catch
                {
                    configuration.GameVersionIndex = 0;
                    gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex], null);
                }
            }

            var builtinVersionDataProviders = new Func<ILegacyGameData>[versions.Count];
            for (int i = 0; i < versions.Count; ++i)
            {
                int index = i;
                if (baseVersionIndices.Contains(i))
                    builtinVersionDataProviders[i] = () => configuration.GameVersionIndex == index ? gameData : LoadBuiltinVersionData(versions[index], null);
                else
                {
                    var lastBaseVersion = baseVersionIndices.Last(idx => idx < index);
                    builtinVersionDataProviders[i] = () => configuration.GameVersionIndex == index ? gameData : LoadBuiltinVersionData(versions[index], builtinVersionDataProviders[lastBaseVersion]);
                }
            }
            var textureAtlasManager = TextureAtlasManager.CreateEmpty();
            createdTextureAtlasManager = textureAtlasManager;

            var flagsData = new DataReader(Resources.Flags);
            var flagsPalette = new Graphic
            {
                Width = 32,
                Height = 1,
                Data = flagsData.ReadBytes(32 * 4),
                IndexedGraphic = false
            };
            var flagsGraphic = new Graphic
            {
                Width = flagsData.ReadWord(),
                Height = flagsData.ReadWord(),
                IndexedGraphic = true
            };
            flagsGraphic.Data = flagsData.ReadBytes(flagsGraphic.Width * flagsGraphic.Height);

            audioOutput = new AudioOutput();
            audioOutput.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
            audioOutput.Enabled = audioOutput.Available && configuration.Music;
            if (configuration.ShowPyrdacorLogo)
            {
                logoPyrdacor = new LogoPyrdacor(audioOutput, SongManager.LoadCustomSong(new DataReader(Resources.Song), 0, false, false));
                additionalPalettes = new Graphic[2] { logoPyrdacor.Palettes[0], flagsPalette };
            }
            else
            {
                additionalPalettes = new Graphic[2] { new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] }, flagsPalette };
            }

            fontProvider ??= new IngameFontProvider(new DataReader(Resources.IngameFont), gameData.FontProvider.GetFont());

            renderView = CreateRenderView(gameData, configuration, gameData.GraphicProvider, fontProvider, additionalPalettes, () =>
            {
                textureAtlasManager.AddUIOnly(gameData.GraphicProvider, fontProvider);
                logoPyrdacor?.Initialize(textureAtlasManager);
                AdvancedLogo.Initialize(textureAtlasManager);
                textureAtlasManager.AddFromGraphics(Layer.Misc, new Dictionary<uint, Graphic>
                {
                    { 1u, flagsGraphic }
                });
                return textureAtlasManager;
            });
            renderView.AvailableFullscreenModes = availableFullscreenModes;
            renderView.SetTextureFactor(Layer.Text, 2);
            InitGlyphs(fontProvider, textureAtlasManager);
            var gameVersions = new List<GameVersion>(5);
            for (int i = 0; i < versions.Count; ++i)
            {
                var builtinVersion = versions[i];
                gameVersions.Add(new GameVersion
                {
                    Version = builtinVersion.Version,
                    Language = builtinVersion.Language.ToGameLanguage(),
                    Info = builtinVersion.Info,
                    DataProvider = builtinVersionDataProviders[i],
                    Features = builtinVersion.Features,
                    MergeWithPrevious = builtinVersion.MergeWithPrevious,
                    ExternalData = false
                });
            }
            if (additionalVersionInfo != null)
            {                
                gameVersions.Add(new GameVersion
                {
                    Version = additionalVersionInfo.Value.Version,
                    Language = additionalVersionInfo.Value.Language.ToGameLanguage(),
                    Info = "From external data",
                    DataProvider = configuration.GameVersionIndex == 4 ? (() => gameData) : LoadGameDataFromDataPath,
                    Features = additionalVersionInfo.Value.Advanced ? Features.AmbermoonAdvanced | Features.WaspTransport | Features.AdvancedSpells : Features.None, // TODO
                    MergeWithPrevious = false,
                    ExternalData = true
                });
            }
            if (configuration.GameVersionIndex < 0 || configuration.GameVersionIndex >= gameVersions.Count)
#if DEBUG
                configuration.GameVersionIndex = additionalVersionInfo != null ? gameVersions.Count - 1 : 0;
#else
                configuration.GameVersionIndex = 0;
#endif
            var cursor = new Render.Cursor(renderView, gameData.CursorHotspots, textureAtlasManager);

            RunTask(() =>
            {
                while (checkPatcher || patcher != null || logoPyrdacor != null)
                    Thread.Sleep(100);

                versionSelector = new VersionSelector(gameVersion, renderView, textureAtlasManager,
                    gameVersions, cursor, configuration.GameVersionIndex, configuration.SaveOption, configuration);
                versionSelector.Closed += (gameVersionIndex, gameData, saveInDataPath) =>
                {
                    var gameVersion = gameVersions[gameVersionIndex];
                    configuration.SaveOption = saveInDataPath ? SaveOption.DataFolder : SaveOption.ProgramFolder;
                    configuration.GameVersionIndex = gameVersionIndex;
                    selectHandler?.Invoke(gameData, saveInDataPath ? dataPath : Configuration.GetSavePath(Configuration.GetVersionSavegameFolder(gameVersion)),
                        gameVersion.Language, gameVersion.Features);
                };
            });

            return true;
        }

        void InitGlyphs(IFontProvider fontProvider, TextureAtlasManager textureAtlasManager = null)
        {
            int glyphCount = fontProvider.GetFont().GlyphCount;
            var textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Text);
            renderView.RenderTextFactory.GlyphTextureMapping = Enumerable.Range(0, glyphCount).ToDictionary(x => (byte)x, x => textureAtlas.GetOffset((uint)x));
            var digitTextureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.SmallDigits);
            renderView.RenderTextFactory.DigitGlyphTextureMapping = Enumerable.Range(0, 10).ToDictionary(x => (byte)(ExecutableData.DigitGlyphOffset + x), x => digitTextureAtlas.GetOffset((uint)x));
        }

        RenderView CreateRenderView(IGameData gameData, IConfiguration configuration, IGraphicProvider graphicProvider,
            IFontProvider fontProvider, Graphic[] additionalPalettes = null, Func<TextureAtlasManager> textureAtlasManagerProvider = null)
        {
            bool AnyIntroActive() => fantasyIntro != null || logoPyrdacor != null || advancedLogo != null;
            var useFrameBuffer = true;
            var useEffects = configuration.Effects != Effects.None;
            var renderView = new RenderView(this, gameData, graphicProvider, fontProvider,
                new TextProcessor(fontProvider.GetFont().GlyphCount), textureAtlasManagerProvider, window.FramebufferSize.X, window.FramebufferSize.Y,
                new Size(window.Size.X, window.Size.Y), ref useFrameBuffer, ref useEffects,
                () => KeyValuePair.Create(AnyIntroActive() ? 0 : (int)configuration.GraphicFilter, AnyIntroActive() ? 0 : (int)configuration.GraphicFilterOverlay),
                () => AnyIntroActive() ? 0 : (int)configuration.Effects,
                additionalPalettes);
            if (!useFrameBuffer)
            {
                configuration.GraphicFilter = GraphicFilter.None;
                configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;
            }
            if (!useEffects)
                configuration.Effects = Effects.None;
            return renderView;
        }

        bool PositionInsideWindow(MousePosition position)
        {
            return position.X >= 0 && position.X < window.Size.X &&
                position.Y >= 0 && position.Y < window.Size.Y;
        }

        void Drug_Ticked()
        {
            if (mouse != null && PositionInsideWindow(mouse.Position))
            {
                mouse.Position = new MousePosition(mouse.Position.X + Game.RandomInt(-16, 16),
                    mouse.Position.Y + Game.RandomInt(-16, 16));
                if (Fullscreen) // This needs a little help
                    Game.OnMouseMove(ConvertMousePosition(mouse.Position), GetMouseButtons(mouse));
            }
        }

        void Window_Load()
        {
            if (window.Native.Glfw is null)
            {
                Console.WriteLine("WARNING: The current window is not a GLFW window." + Environment.NewLine +
                                  "         Other window systems may be not fully supported!");
            }

            var windowIcon = new Silk.NET.Core.RawImage(16, 16, new Memory<byte>(Resources.WindowIcon));
            window.SetWindowIcon(ref windowIcon);

            window.MakeCurrent();

            // Setup input
            SetupInput(window.CreateInput());

            availableFullscreenModes = window.Monitor.GetAllVideoModes().Select(mode =>
                new Size(mode.Resolution.Value.X, mode.Resolution.Value.Y)).Distinct().ToList();

            var fullscreenSize = availableFullscreenModes.OrderBy(r => r.Width * r.Height).LastOrDefault();

            if (fullscreenSize != null)
            {
                configuration.FullscreenWidth = fullscreenSize.Width;
                configuration.FullscreenHeight = fullscreenSize.Height;
            }

            var platform = Silk.NET.Windowing.Window.GetWindowPlatform(false);
            var monitors = platform.GetMonitors().ToArray();

            if (configuration.MonitorIndex != null && configuration.MonitorIndex >= 0 && configuration.MonitorIndex < monitors.Length)
                window.Monitor = monitors[configuration.MonitorIndex.Value];
            else
                window.Monitor = platform.GetMainMonitor();

            if (configuration.Fullscreen)
            {
                ChangeFullscreenMode(true); // This will adjust the window
            }

            if (configuration.Width == null || configuration.Height == null)
            {
                var monitorSize = window.Monitor.Bounds.Size;
                var size = ScreenResolutions.GetPossibleResolutions(new Size(monitorSize.X, monitorSize.Y))[2];
                configuration.Width = Width = size.Width;
                configuration.Height = Height = size.Height;
                if (!configuration.Fullscreen)
                    window.Size = new WindowDimension(Width, Height);
            }

            if (!configuration.Fullscreen)
            {
                if (configuration.WindowX == null || configuration.WindowY == null)
                {
                    window.Center();
                    configuration.WindowX = window.Position.X;
                    configuration.WindowY = window.Position.Y;
                    configuration.MonitorIndex = window.Monitor.Index;
                }
                else
                {
                    window.Position = new WindowDimension(configuration.WindowX.Value, configuration.WindowY.Value);
                    EnsureWindowOnMonitor();
                    configuration.WindowX = window.Position.X;
                    configuration.WindowY = window.Position.Y;
                    configuration.MonitorIndex = window.Monitor.Index;
                }

                var monitorSize = window.Monitor.Bounds.Size;
                var realBorderSize = window.BorderSize.Origin + window.BorderSize.Size;
                var realWindowSize = window.Size + realBorderSize;

                if (realWindowSize.X > monitorSize.X || realWindowSize.Y > monitorSize.Y)
                {
                    var size = ScreenResolutions.GetPossibleResolutions(new Size(monitorSize.X, monitorSize.Y))[2];
                    configuration.Width = Width = size.Width;
                    configuration.Height = Height = size.Height;
                    window.Size = new WindowDimension(Width, Height);
                }
            }

            initialized = true;

            static BinaryReader LoadAdvancedDiffs()
            {
                static bool TryLoad(string path, out BinaryReader reader)
                {
                    reader = File.Exists(path) ? new BinaryReader(File.OpenRead(path)) : null;
                    return reader != null;
                }

                if (TryLoad("diffs.dat", out var reader))
                    return reader;

                if (TryLoad(Path.Combine(Configuration.ReadonlyBundleDirectory, "diffs.dat"), out reader))
                    return reader;

                if (OperatingSystem.IsMacOS() &&
                    Configuration.ReadonlyBundleDirectory != Configuration.ExecutableDirectoryPath &&
                    !Directory.Exists(Configuration.ReadonlyBundleDirectory) &&
                    TryLoad(Path.Combine(Configuration.ExecutableDirectoryPath, "diffs.dat"), out reader))
                {
                    return reader;
                }

                return null;
            }

            static BinaryReader LoadVersionData()
            {
                static bool TryLoad(string path, out BinaryReader reader)
                {
                    reader = File.Exists(path) ? new BinaryReader(File.OpenRead(path)) : null;
                    return reader != null;
                }

                if (TryLoad("versions.dat", out var reader))
                    return reader;

                if (TryLoad(Path.Combine(Configuration.ReadonlyBundleDirectory, "versions.dat"), out reader))
                    return reader;

                if (OperatingSystem.IsMacOS() &&
                    Configuration.ReadonlyBundleDirectory != Configuration.ExecutableDirectoryPath &&
                    !Directory.Exists(Configuration.ReadonlyBundleDirectory) &&
                    TryLoad(Path.Combine(Configuration.ExecutableDirectoryPath, "versions.dat"), out reader))
                {
                    return reader;
                }

                return null;
            }

            var additionalData = OperatingSystem.IsMacOS() ? null : AdditionalData.Loader.Load();
            var builtinVersionReader = additionalData == null
                ? LoadVersionData()
                : additionalData.TryGetValue("versions", out var reader) ? reader : LoadVersionData();

            if (ShowVersionSelector(builtinVersionReader, (gameData, savePath, gameLanguage, features) =>
            {
                try
                {
                    builtinVersionReader?.Dispose();
                    renderView?.Dispose();
                }
                catch
                {
                    // ignore
                }

                try
                {
                    var advancedDiffsReader = gameData.Advanced ? (additionalData == null
                        ? LoadAdvancedDiffs()
                        : additionalData.TryGetValue("diffs", out var reader) ? reader : LoadAdvancedDiffs()) : null;
                    StartGame(gameData as GameData, savePath, gameLanguage, features, advancedDiffsReader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error starting game: " + ex.ToString());
                    window.Close();
                    return;
                }
                WindowMoved();
                versionSelector = null;
            }, out var textureAtlasManager))
            {
                WindowMoved();
            }

            string patcherPath = Path.Combine(Configuration.ExecutableDirectoryPath, "AmbermoonPatcher");

            if (OperatingSystem.IsWindows())
                patcherPath += ".exe";

            bool hasPatcher = File.Exists(patcherPath);

            if (hasPatcher && configuration.UsePatcher != false)
            {
                bool firstTime = configuration.UsePatcher == null;
                patcher = new Patcher(renderView, patcherPath, textureAtlasManager ?? TextureAtlasManager.Instance);
                checkPatcher = false;

                if (firstTime)
                {
                    patcher.AskToUsePatcher(() =>
                    {
                        configuration.UsePatcher = true;
                        CheckPatches();
                    }, () =>
                    {
                        configuration.UsePatcher ??= false;
                        patcher?.CleanUp(true);
                        patcher = null;
                        logoPyrdacor?.PlayMusic();
                    });
                }
                else
                {
                    CheckPatches();
                }

                void CheckPatches()
                {
                    configuration.PatcherTimeout ??= 1250;                    
                    int timeout = configuration.PatcherTimeout.Value;
                    patcher.CheckPatches(ok =>
                    {
                        if (ok)
                            window.Close();
                        else
                            NotPatched();
                    }, NotPatched, ref timeout);
                    configuration.PatcherTimeout = timeout;
                    void NotPatched()
                    {
                        patcher?.CleanUp(true);
                        patcher = null;
                        logoPyrdacor?.PlayMusic();
                    }
                }
            }
            else
            {
                checkPatcher = false;
                logoPyrdacor?.PlayMusic();
            }
        }

        void Window_Render(double delta)
        {
            if (window.WindowState != WindowState.Minimized)
            {
                int refreshRate = Util.Limit(1, window.Monitor.VideoMode.RefreshRate ?? 60, 250);
                var timePerFrame = 1000.0 / refreshRate;
                bool vsync = lastRenderDuration.TotalMilliseconds <= timePerFrame;

                if (vsync != window.VSync)
                    window.VSync = vsync;
                else if (vsync)
                {
                    var renderDuration = DateTime.Now - lastRenderTime;
                    if (lastRenderDuration.TotalMilliseconds < 10 &&
                        renderDuration.TotalMilliseconds < timePerFrame - 4.0 * delta * 1000.0 - lastRenderDuration.TotalMilliseconds)
                        return;
                }

                var startRenderTime = DateTime.Now;
            
                if (patcher != null)
                    patcher.Render();
                else if (versionSelector != null)
                    versionSelector.Render();
                if (mainMenu != null)
                    mainMenu.Render();
                else if (Game != null)
                    renderView.Render(Game.ViewportOffset);
                else if (renderView != null)
                    renderView.Render(null);

                window.SwapBuffers();

                lastRenderTime = DateTime.Now;
                lastRenderDuration = lastRenderTime - startRenderTime;
            }
        }

        void Window_Update(double delta)
        {
            if (initializeErrorTime != null)
            {
                if ((DateTime.Now - initializeErrorTime.Value).TotalSeconds > 5)
                    window.Close();
                return;
            }

            if (patcher == null && logoPyrdacor != null)
                logoPyrdacor.Update(renderView, () => logoPyrdacor = null);

            if (patcher != null)
                patcher.Update(delta);
            else if (versionSelector != null)
                versionSelector.Update(delta);
            else if (logoPyrdacor == null && fantasyIntro != null)
                fantasyIntro.Update(delta);
            else if (logoPyrdacor == null && advancedLogo != null)
                advancedLogo.Update(renderView, () => advancedLogo = null);
            else if (intro != null)
                intro.Update(delta);
            else if (mainMenu != null)
            {
                mainMenu.Update();

                if (gameCreator != null)
                {
                    // Create game
                    try
                    {
                        Game = gameCreator();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error creating game: " + ex.Message);
                        window.Close();
                        return;
                    }
                    mainMenu?.Destroy();
                    mainMenu = null;
                    gameCreator = null;

                    // Show cheat info
                    if (configuration.EnableCheats)
                    {
                        PrintCheatConsoleHeader();
                    }
                }
            }
            else if (Game != null)
            {
                bool cheatsEnabled = configuration.EnableCheats;

                Game.Update(delta);

                if (cheatsEnabled && configuration.EnableCheats)
                {
                    if (!Console.IsInputRedirected)
                    {
                        if (Console.KeyAvailable)
                            Cheats.ProcessInput(Console.ReadKey(true), Game);
                    }
                    else
                    {
                        if (!cheatTaskStarted)
                        {
                            cheatTaskStarted = true;
                            Task.Run(async () =>
                            {
                                while (!window.IsClosing && !cheatTaskCancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    string input = await Console.In.ReadLineAsync();

                                    if (cheatsEnabled && configuration.EnableCheats)
                                    {
                                        Cheats.ProcessInput(input, Game);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Cheats are disabled so your input has no effect.");
                                    }
                                }
                            }, cheatTaskCancellationTokenSource.Token);
                        }
                    }
                }
            }
        }

        static bool cheatHeaderPrinted = false;
        static bool cheatTaskStarted = false;        
        static CancellationTokenSource cheatTaskCancellationTokenSource = new CancellationTokenSource();

        static void PrintCheatConsoleHeader()
        {
            if (!cheatHeaderPrinted)
            {
                cheatHeaderPrinted = true;
                Console.WriteLine("***** Ambermoon Cheat Console *****");
                Console.WriteLine("Type 'help' for more information.");
                Console.WriteLine();
            }
        }

        void Window_Resize(WindowDimension size)
        {
            if (!Fullscreen && (size.X != Width || size.Y != Height))
            {
                // This seems to happen when changing the screen resolution.
                window.Size = new WindowDimension(Width, Height);
                EnsureWindowOnMonitor();
            }

            if (renderView != null)
                renderView.Resize(window.FramebufferSize.X, window.FramebufferSize.Y, size.X, size.Y);
        }

        void Window_FramebufferResize(WindowDimension size)
        {
            if (renderView != null)
                renderView.Resize(size.X, size.Y);
        }

        void Window_StateChanged(WindowState state)
        {
            if (state == WindowState.Minimized)
                Game?.PauseGame();
            else
                Game?.ResumeGame();
        }

        void Window_Move(WindowDimension position)
        {
            if (initialized)
            {
                configuration.WindowX = window.Position.X;
                configuration.WindowY = window.Position.Y;
                configuration.MonitorIndex = window.Monitor?.Index ?? 0;
            }

            WindowMoved();
        }

        void WindowMoved()
        {
            if (renderView != null)
            {
                var monitorSize = window.Monitor?.Bounds.Size ?? new WindowDimension(800, 500);
                renderView.MaxScreenSize = new Size(monitorSize.X, monitorSize.Y);
                renderView.Resize(window.FramebufferSize.X, window.FramebufferSize.Y);
            }
        }

        void UpdateWindow(IConfiguration configuration)
        {
            if (!Fullscreen)
            {
                var size = configuration.GetScreenSize();
                this.configuration.Width = Width = size.Width;
                this.configuration.Height = Height = size.Height;
                window.Size = new WindowDimension(size.Width, size.Height);
                EnsureWindowOnMonitor();
            }

            renderView?.Resize(window.FramebufferSize.X, window.FramebufferSize.Y, window.Size.X, window.Size.Y);
        }

        void EnsureWindowOnMonitor()
        {
            var bounds = window.Monitor?.Bounds;
            WindowDimension upperLeft = bounds?.Origin ?? new WindowDimension(0, 0);
            int? newX = null;
            int? newY = null;

            if (window.Position.X - window.BorderSize.Origin.X < upperLeft.X)
            {
                newX = upperLeft.X + window.BorderSize.Origin.X;
            }
            else if (bounds != null && window.Position.X >= upperLeft.X + bounds.Value.Size.X)
            {
                newX = Math.Max(upperLeft.X + window.BorderSize.Origin.X, upperLeft.X + bounds.Value.Size.X - window.Size.X - window.BorderSize.Origin.X - window.BorderSize.Size.X);
            }

            if (window.Position.Y - window.BorderSize.Origin.Y < upperLeft.Y)
            {
                newY = upperLeft.Y + window.BorderSize.Origin.Y;
            }
            else if (bounds != null && window.Position.Y >= upperLeft.Y + bounds.Value.Size.Y)
            {
                newY = Math.Max(upperLeft.Y + window.BorderSize.Origin.Y, upperLeft.Y + bounds.Value.Size.Y - window.Size.Y - window.BorderSize.Origin.Y - window.BorderSize.Size.Y);
            }

            if (newX != null || newY != null)
            {
                window.Position = new WindowDimension(newX ?? window.Position.X, newY ?? window.Position.Y);
            }
        }

        public void Run(Configuration configuration)
        {
            this.configuration = configuration;
            var screenSize = configuration.GetScreenSize();
            Width = screenSize.Width;
            Height = screenSize.Height;

#if GLES
            var api = new GraphicsAPI
                (ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(2, 0));
#else
            var api = GraphicsAPI.Default;
#endif
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            gameVersion = $"Ambermoon.net v{version.Major}.{version.Minor}.{version.Build}";
            var videoMode = new VideoMode(60);
            var options = new WindowOptions(true, new WindowDimension(100, 100),
                new WindowDimension(Width, Height), 60.0, 60.0, api, gameVersion,
                WindowState.Normal, WindowBorder.Fixed, true, false, videoMode, 24);
            options.WindowClass = "Ambermoon.net";

            try
            {
                Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
                window = (IWindow)Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
                window.Title = options.Title;
                window.Size = options.Size;
                window.WindowBorder = options.WindowBorder;
                window.Load += Window_Load;
                window.Render += Window_Render;
                window.Update += Window_Update;
                window.Resize += Window_Resize;
                window.FramebufferResize += Window_FramebufferResize;
                window.Move += Window_Move;
                window.StateChanged += Window_StateChanged;
                window.Closing += () =>
                {
                    audioOutput.Stop();
                    audioOutput.Dispose();
                    cheatTaskCancellationTokenSource.Cancel();
                };
                window.Run();
            }
            catch (Exception ex)
            {
                if (Game != null)
                {
                    try
                    {
                        Game.SaveCrashedGame();
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (renderView != null && infoText != null)
                {
                    Util.SafeCall(() => Game?.Destroy());
                    Util.SafeCall(() => window.DoRender());
                    Util.SafeCall(() =>
                    {
                        var screenArea = new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight);
                        var text = renderView.TextProcessor.CreateText(ex.Message
                            .Replace("\r\n", "^")                            
                            .Replace("\n", "^")
                            .Replace("\r", "^"),
                            ' ');
                        text = renderView.TextProcessor.WrapText(text, screenArea, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
                        int height = text.LineCount * Global.GlyphLineHeight - 1;
                        infoText.Text = text;
                        screenArea.Position.Y = Math.Max(0, (Global.VirtualScreenHeight - height) / 2);
                        infoText.Place(screenArea, TextAlign.Center);
                        infoText.Visible = true;

                        for (int i = 0; i < 5; ++i)
                        {
                            window.DoRender();
                            Thread.Sleep(1000);
                        }
                    });
                }

                throw;
            }
            finally
            {
                Util.SafeCall(() =>
                {
                    infoText?.Delete();
                    infoText = null;
                });
                Util.SafeCall(() => window?.Dispose());
            }
        }
    }
}
