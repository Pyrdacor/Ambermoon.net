using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Ambermoon.UI;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Windowing;
using MousePosition = System.Numerics.Vector2;
using WindowDimension = Silk.NET.Maths.Vector2D<int>;
using Key = Ambermoon.Key;
using Data = Ambermoon.Data;
using Render = Ambermoon.Render;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Input.Sdl;
using System.Globalization;

namespace AmbermoonAndroid;

class GameWindow : IContextProvider
{
    public enum ActivityState
    {
        Active, // visible and active
        Paused, // visible but not active
        Stopped // invisible and not active
    }

    string gameVersion = "Ambermoon.net";
    Configuration configuration;
    GameRenderView renderView;
    IView window;
    IKeyboard keyboard = null;
    IMouse mouse = null;
    ICursor cursor = null;
    MainMenu mainMenu = null;
    Func<Game> gameCreator = null;
    Func<MusicManager> musicManagerFactory = null;
    MusicManager musicManager = null;
    bool musicInitialized = false;
    IRenderText infoText = null;
    IFontProvider fontProvider = null;
    DateTime? initializeErrorTime = null;
    bool trapMouse = false;
    FloatPosition trappedMouseOffset = null;
    FloatPosition trappedMouseLastPosition = null;
    FantasyIntro fantasyIntro = null;
    LogoPyrdacor logoPyrdacor = null;
    AdvancedLogo advancedLogo = null;
    LoadingBar loadingBar = null;
    Action preloadAction = null;
    Action switchRenderViewAction = null;
    bool preloading = true;
    Graphic[] additionalPalettes;
    bool initialIntroEndedByClick = false;
    readonly List<Action> touchActions = new();
    readonly Action<bool, string> keyboardRequest;
    TutorialFinger tutorialFinger;
    TouchPad touchPad;
    ISprite donateButton;
    ActivityState state = ActivityState.Active;
    readonly Action<Action> runOnUiThread;

    public ActivityState State
    {
        get => state;
        set
        {
            if (state == value)
                return;

            state = value;
            OnStateChanged();
        }
    }
    public string Identifier { get; }
    public IGLContext GLContext => window?.GLContext;
    public int Width { get; private set; }
    public int Height { get; private set; }
    VersionSelector versionSelector = null;
    Intro intro = null;
    public Game Game { get; private set; }
    public event Action OpenDonationLink;
    public event Action Closed;

    public GameWindow(Action<Action> runOnUiThread, string gameVersion, Action<bool, string> keyboardRequest, string id = "MainWindow")
    {
        this.runOnUiThread = runOnUiThread;
        this.gameVersion = gameVersion;
        this.keyboardRequest = keyboardRequest;
        Identifier = id;
    }

    void DrawTouchFinger(int x, int y, bool longPress, Rect clipArea, bool behindPopup)
    {
        tutorialFinger?.Clip(clipArea);
        tutorialFinger?.DrawFinger(x, y, longPress, behindPopup);
    }

    async void WaitForTask(Action waitTask, Action task)
    {
        await Task.Run(waitTask);
        runOnUiThread(task);
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
            /*mouse.MouseDown += Mouse_MouseDown;
            mouse.MouseUp += Mouse_MouseUp;
            mouse.MouseMove += Mouse_MouseMove;
            mouse.Scroll += Mouse_Scroll;*/
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
        if (loadingBar != null)
            return;
        else if (versionSelector != null)
            versionSelector.OnKeyChar(keyChar);
        else if (Game != null)
            Game.OnKeyChar(keyChar);
    }

    void Keyboard_KeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int value)
    {
        if (loadingBar != null)
            return;

        if (key == Silk.NET.Input.Key.F7)
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
                configuration.GraphicFilter = (GraphicFilter)(((int)configuration.GraphicFilter + 1) % EnumHelper.GetValues<GraphicFilter>().Length);
            else
                configuration.GraphicFilter = (GraphicFilter)(((int)configuration.GraphicFilter - 1 + EnumHelper.GetValues<GraphicFilter>().Length) % EnumHelper.GetValues<GraphicFilter>().Length);

            if (!renderView.TryUseFrameBuffer())
                configuration.GraphicFilter = GraphicFilter.None;

            Game?.ExternalGraphicFilterChanged();
        }
        else if (key == Silk.NET.Input.Key.F9)
        {
            if (GetModifiers(keyboard) == KeyModifiers.None)
                configuration.GraphicFilterOverlay = (GraphicFilterOverlay)(((int)configuration.GraphicFilterOverlay + 1) % EnumHelper.GetValues<GraphicFilterOverlay>().Length);
            else
                configuration.GraphicFilterOverlay = (GraphicFilterOverlay)(((int)configuration.GraphicFilterOverlay - 1 + EnumHelper.GetValues<GraphicFilterOverlay>().Length) % EnumHelper.GetValues<GraphicFilterOverlay>().Length);

            if (!renderView.TryUseFrameBuffer())
                configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;

            Game?.ExternalGraphicFilterOverlayChanged();
        }
        else if (key == Silk.NET.Input.Key.F10)
        {
            if (GetModifiers(keyboard) == KeyModifiers.None)
                configuration.Effects = (Effects)(((int)configuration.Effects + 1) % EnumHelper.GetValues<Effects>().Length);
            else
                configuration.Effects = (Effects)(((int)configuration.Effects - 1 + EnumHelper.GetValues<Effects>().Length) % EnumHelper.GetValues<Effects>().Length);

            if (!renderView.TryUseEffects())
                configuration.Effects = Effects.None;

            Game?.ExternalEffectsChanged();
        }
        else if (key == Silk.NET.Input.Key.M && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
             keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
        {
            configuration.Music = !configuration.Music;
            musicManager.Enabled = configuration.Music;
            if (musicManager.Available && musicManager.Enabled)
                Game?.ContinueMusic();
            Game?.ExternalMusicChanged();
        }
        else if (key == Silk.NET.Input.Key.Comma && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
             keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
        {
            if (configuration.Volume >= 10)
            {
                configuration.Volume -= 10;
                musicManager.Volume = configuration.Volume / 100.0f;
                Game?.ExternalVolumeChanged();
            }
            else if (configuration.Volume > 0)
            {
                configuration.Volume = 0;
                musicManager.Volume = configuration.Volume / 100.0f;
                Game?.ExternalVolumeChanged();
            }
        }
        else if (key == Silk.NET.Input.Key.Period && (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ||
             keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight)))
        {
            if (configuration.Volume <= 90)
            {
                configuration.Volume += 10;
                musicManager.Volume = configuration.Volume / 100.0f;
                Game?.ExternalVolumeChanged();
            }
            else if (configuration.Volume < 100)
            {
                configuration.Volume = 100;
                musicManager.Volume = configuration.Volume / 100.0f;
                Game?.ExternalVolumeChanged();
            }
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
                renderView.ShowImageLayerOnly = false;
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
        if (loadingBar != null)
            return;
        else if (versionSelector != null)
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

    /*void Mouse_MouseDown(IMouse mouse, MouseButton button)
    {
        var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

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
            renderView.ShowImageLayerOnly = false;
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

        if (versionSelector != null)
            versionSelector.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
        else if (mainMenu != null)
            mainMenu.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
        else if (Game != null)
            Game.OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
    }*/

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

        if (loadingBar != null)
            return;
        else if (versionSelector != null)
            versionSelector.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
        else if (mainMenu != null)
            mainMenu.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
        else if (Game != null)
            Game.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
    }

    /*void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
    {
        var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

        if (versionSelector != null)
            versionSelector.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), ConvertMousePosition(position));
        else if (Game != null)
            Game.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), ConvertMousePosition(position));
    }*/

    internal void OnLongPress(Position position)
    {
        if (Game == null)
        {
            OnMouseDown(position, MouseButtons.Right);
            OnMouseUp(position, MouseButtons.Right);
        }
        else
        {
            lock (touchActions)
            {
                touchActions.Add(() =>
                {
                    if (Game != null && touchPad?.OnLongPress(Game, position) == true)
                        return;

                    Game?.OnLongPress(position);
                });
            }
        }
    }

    private Position ConvertPositionToGame(Position position) => renderView?.ScreenToGame(position) ?? position;

    internal void OnMouseDown(Position position, MouseButtons buttons)
    {
        lock (touchActions)
        {
            touchActions.Add(() =>
            {
                if (loadingBar != null)
                    return;
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
                {
                    if (!TestDonateButtonClick(position))
                        versionSelector.OnMouseDown(position, buttons);
                }
                else if (mainMenu != null)
                    mainMenu.OnMouseDown(position, buttons);
                else if (intro != null)
                    intro.Click();
                else if (Game != null)
                    Game.OnMouseDown(position, buttons);
            });
        }
    }

    internal void OnMouseUp(Position position, MouseButtons buttons)
    {
        lock (touchActions)
        {
            touchActions.Add(() =>
            {
                if (loadingBar != null)
                    return;
                else if (versionSelector != null)
                    versionSelector.OnMouseUp(position, buttons);
                else if (mainMenu != null)
                    mainMenu.OnMouseUp(position, buttons);
                else if (Game != null)
                    Game.OnMouseUp(position, buttons);
            });
        }
    }

    internal void OnMouseScroll(Position position, int deltaX, int deltaY)
    {
        lock (touchActions)
        {
            touchActions.Add(() =>
            {
                // Note: x and y are swapped on purpose here as the screen is rotated
                if (loadingBar != null)
                    return;
                else if (versionSelector != null)
                    versionSelector.OnMouseWheel(deltaY, deltaX, position);
                else if (Game != null)
                    Game.OnMouseWheel(deltaY, deltaX, position);
            });
        }
    }

    internal bool OnTap(Position position)
    {
        return touchPad?.OnTap(Game, position) ?? false;
    }

    internal void OnFingerDown(Position position)
    {
        Game?.OnFingerDown(position);
    }

    internal void OnFingerUp(Position position)
    {
        if (Game == null)
            return;

        touchPad?.OnFingerUp(Game, position);
        Game.OnFingerUp(position);
    }

    internal void OnFingerMoveTo(Position position)
    {
        if (Game != null && touchPad?.OnFingerMoveTo(Game, position) == true)
            return;

        Game?.OnFingerMoveTo(position);
    }

    internal void OnKeyChar(char ch)
    {
        if (ch == '\n')
        {
            OnKeyDown(Key.Return);
            return;
        }

        Keyboard_KeyChar(null, ch);
    }

    internal void OnKeyDown(Key key)
    {
        Game?.OnKeyDown(key, KeyModifiers.None);
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

    void ShowMainMenu(IGameRenderView renderView, Render.Cursor cursor, bool fromIntro, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
        Font introFont, string[] mainMenuTexts, bool canContinue, Action<bool> startGameAction, GameLanguage gameLanguage,
        Action showIntroAction)
    {
        renderView.PaletteFading = null; // Reset palette fading

        void PlayMusic(Song song)
        {
            if (configuration.Music)
                musicManager.GetSong(song)?.Play(musicManager);

            if (infoText != null)
                infoText.Visible = false;
        }

        // Fast clicking might create the main menu while the intro is also created and so both would be active.
        // This will ensure that the intro is destroyed if the main menu opens.
        intro?.Destroy();
        intro = null;
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
                    Quit();
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
        },
        { GameLanguage.Czech, new string[]
            {
                "Zahájení hry ...",
                "Příprava nové hry ..."
            }
        }
        };

    static string GetText(GameLanguage gameLanguage, int index) => LoadingTexts[gameLanguage][index];

    void SetMobileDeviceView(bool set)
    {
        // In game we want to move everything to the left and make room for the touch pad area.
        // Before we just display as on desktop (centered on screen). Now switch to mobile device view.
        renderView.SetDeviceType(set ? DeviceType.MobileLandscape : DeviceType.Desktop, window.FramebufferSize.X, window.FramebufferSize.Y, window.Size.X, window.Size.Y);
    }

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

        if (!musicInitialized)
        {
            musicInitialized = true;
            musicManager.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
            musicManager.Enabled = musicManager.Available && configuration.Music;
            if (configuration.ShowPyrdacorLogo)
            {
                logoPyrdacor = new LogoPyrdacor(musicManager, musicManager.GetPyrdacorSong());
                logoPyrdacor?.PlayMusic();
                additionalPalettes = logoPyrdacor.Palettes;
            }
            else
            {
                additionalPalettes = [new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] }];
            }
        }

        if (gameData.Advanced && configuration.ShowAdvancedLogo)
            advancedLogo = new AdvancedLogo();

        fontProvider ??= new IngameFontProvider(new DataReader(FileProvider.GetIngameFontData()), gameData.FontProvider.GetFont());

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
            var graphics = TutorialFinger.GetGraphics(1u); // Donate button is 0
            graphics.Add(0u, FileProvider.GetDonateButton());
            textureAtlasManager.AddFromGraphics(Layer.MobileOverlays, graphics);
            graphics = TouchPad.GetGraphics(1u); // Advanced logo is 0
            textureAtlasManager.AddFromGraphics(Layer.Images, graphics);
            return textureAtlasManager;
        });
        renderView.AvailableFullscreenModes = new();
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
        infoText = renderView.RenderTextFactory.Create(
            (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
            renderView.GetLayer(Layer.Text), text, Data.Enumerations.Color.White, false,
            Global.GetTextRect(renderView, new Rect(0, Global.VirtualScreenHeight / 2 - 3, Global.VirtualScreenWidth, 6)), TextAlign.Center);
        infoText.DisplayLayer = 254;
        infoText.Visible = false;

        var cursor = new InvisibleCursor(renderView, gameData.CursorHotspots);
        cursor.UpdatePosition(ConvertMousePosition(mouse.Position), null);
        cursor.Type = Data.CursorType.None;

        WaitForTask(() =>
        {
            while (logoPyrdacor != null)
                Thread.Sleep(100);

            while (fantasyIntro != null)
                Thread.Sleep(100);

            while (intro != null)
                Thread.Sleep(100);

            while (advancedLogo != null)
            {
                renderView.ShowImageLayerOnly = true;
                Thread.Sleep(100);
            }
        }, () =>
        {
            try
            {
                var savegameManager = new RemakeSavegameManager(savePath, configuration);
                savegameManager.GetSavegameNames(gameData, out int currentSavegame, Game.NumBaseSavegameSlots);
                if (currentSavegame == 0 && configuration.ExtendedSavegameSlots)
                    currentSavegame = savegameManager.ContinueSavegameSlot;
                bool canContinue = currentSavegame != 0;

                void SetupGameCreator(bool continueGame)
                {
                    try
                    {
                        var savegameSerializer = new SavegameSerializer();

                        gameCreator = () =>
                        {
                            var game = new Game(configuration, gameLanguage, renderView, graphicProvider,
                                savegameManager, savegameSerializer, gameData.Dictionary, cursor, musicManager,
                                musicManager, (_) => { }, (_) => { }, QueryPressedKeys,
                                new OutroFactory(renderView, outroData, outroFont, outroFontLarge), features,
                                Path.GetFileName(savePath), gameVersion, keyboardRequest, savegameManager,
                                DrawTouchFinger, show => touchPad.Show(show), SetMobileDeviceView);
                            game.QuitRequested += Quit;
                            /*game.MousePositionChanged += position =>
                            {
                                if (mouse != null)
                                {
                                    mouse.MouseMove -= Mouse_MouseMove;
                                    mouse.Position = new MousePosition(position.X, position.Y);
                                    mouse.MouseMove += Mouse_MouseMove;
                                }
                            };*/
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
                                /*if (mouse != null)
                                {
                                    mouse.MouseMove -= Mouse_MouseMove;
                                    mouse.Position = !trapped || !trapMouse ? new MousePosition(position.X, position.Y) :
                                        new MousePosition(window.Size.X / 2, window.Size.Y / 2);
                                    mouse.MouseMove += Mouse_MouseMove;
                                }*/
                            };
                            game.ConfigurationChanged += (configuration, windowChange) =>
                            {
                                if (!renderView.TryUseFrameBuffer())
                                {
                                    configuration.GraphicFilter = GraphicFilter.None;
                                    configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;
                                }

                                if (!renderView.TryUseEffects())
                                    configuration.Effects = Effects.None;
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

                            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.MobileOverlays);

                            tutorialFinger = new TutorialFinger(renderView);
                            touchPad = new TouchPad(renderView, new(window.Size.X, window.Size.Y));
                            touchPad.Show(false);

                            game.Run(continueGame, ConvertMousePosition(mouse.Position));

                            return game;
                        };
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("Ambermoon", "Error while preparing game: " + ex.ToString());
                        gameCreator = () => throw new AmbermoonException(ExceptionScope.Application, "Game preparation failed.");
                    }
                }

                void ShowMainMenu(bool fromIntro)
                {
                    // When starting for the first time the intro is played automatically.
                    // But then is disabled. It can still be viewed from the main menu
                    // and there is also an option to show it always in the option menu.
                    if (configuration.FirstStart && !fromIntro)
                        configuration.ShowIntro = false;

                    this.ShowMainMenu(renderView, cursor, fromIntro, IntroData.GraphicPalettes, introFontLarge,
                        introData.Texts.Skip(8).Take(4).Select(t => t.Value).ToArray(), canContinue, continueGame =>
                        {
                            cursor.Type = Data.CursorType.None;
                            mainMenu.FadeOutAndDestroy(continueGame, () => SetupGameCreator(continueGame));
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
                Android.Util.Log.Error("Ambermoon", error);

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
                    Quit();
                }
            }
        });
    }

    void Quit()
    {
        window.Close();
        Closed?.Invoke();
    }

    void ShowIntro(Action<bool> showMainMenuAction, IIntroData introData, Font introFont, Font introFontLarge)
    {
        mainMenu?.Destroy();
        mainMenu = null;
        if (configuration.Music)
        {
            musicManager.GetSong(Song.Intro)?.Stop(); // might be looping in the main menu, so we need to stop and reset it here
        }

        intro = new Intro(renderView, introData, introFont, introFontLarge, byClick =>
        {
            intro = null;
            showMainMenuAction?.Invoke(byClick);
        }, () =>
        {
            if (configuration.Music)
                musicManager.GetSong(Song.Intro)?.Play(musicManager);
        });
    }

    bool ShowVersionSelector(List<BuiltinVersion> versions, Action<IGameData, string, GameLanguage, Features> selectHandler, out TextureAtlasManager createdTextureAtlasManager)
    {
        var textureAtlasManager = TextureAtlasManager.CreateEmpty();
        createdTextureAtlasManager = textureAtlasManager;

        var gameData = new GameData();

        if (versions == null || versions.Count == 0)
        {
            // no versions
            loadingBar.SetProgress(1.0f);
            throw new AmbermoonException(ExceptionScope.Data, "No game versions found.");
        }

        GameData LoadBuiltinVersionData(BuiltinVersion builtinVersion, Func<ILegacyGameData> fallbackGameDataProvider,
            Action<float> progressTracker = null)
        {
            var gameData = new GameData();
            builtinVersion.SourceStream.Position = builtinVersion.Offset;
            var buffer = new byte[(int)builtinVersion.Size];
            builtinVersion.SourceStream.Read(buffer, 0, buffer.Length);
            var tempStream = new MemoryStream(buffer);
            // If the builtin version has a base version, it can provide files called "Intro_texts.amb" and "Extro_texts.amb"
            // which only contains the intro and outro texts. It is then merged with the base version's Ambermoon_intro or Ambermoon_extro file.
            var optionalAdditionalFiles = fallbackGameDataProvider == null ? null : new Dictionary<string, char>()
            {
                { "Intro_texts.amb", 'A' },
                { "Extro_texts.amb", 'A' }
            };
            gameData.LoadFromMemoryZip(tempStream, fallbackGameDataProvider, optionalAdditionalFiles, progressTracker, true);
            return gameData;
        }

        if (configuration.GameVersionIndex >= versions.Count)
            configuration.GameVersionIndex = -1;

        // Falls back to english
        int GetGameVersionIndexBySystemLanguage()
        {
            var uiCulture = CultureInfo.CurrentUICulture;
            string languageName = uiCulture.Parent.EnglishName;

            int versionIndex = versions.FindIndex(v => v.Language.Equals(languageName, StringComparison.CurrentCultureIgnoreCase) && !v.Info.Contains("advanced", StringComparison.CurrentCultureIgnoreCase));

            if (versionIndex == -1)
                versionIndex = versions.FindIndex(v => v.Language.Equals(languageName, StringComparison.CurrentCultureIgnoreCase));

            if (versionIndex == -1)
                versionIndex = versions.FindIndex(v => v.Language.Equals("english", StringComparison.CurrentCultureIgnoreCase) && !v.Info.Contains("advanced", StringComparison.CurrentCultureIgnoreCase));

            if (versionIndex == -1)
                versionIndex = versions.FindIndex(v => v.Language.Equals("english", StringComparison.CurrentCultureIgnoreCase));

            if (versionIndex == -1)
                versionIndex = 0;

            return versionIndex;
        }


        // Some versions merge with another one. Here only the basis versions are stored.
        var baseVersionIndices = versions.Select((version, index) => new { version, index }).Where(v => !v.version.MergeWithPrevious).Select(v => v.index).ToList();

        Action<float> progressTracker = progress => loadingBar.SetProgress(0.15f + progress * 0.85f);

        var gameDataPromise = ResourceProvider.GetResource(() =>
        {
            if (configuration.GameVersionIndex < 0)
            {
                // Try to select a version by the OS system language
                configuration.GameVersionIndex = GetGameVersionIndexBySystemLanguage();
            }

            Func<ILegacyGameData> fallbackGameDataProvider = baseVersionIndices.Contains(configuration.GameVersionIndex)
                ? null
                : () => LoadBuiltinVersionData(versions[baseVersionIndices.Last(idx => idx < configuration.GameVersionIndex)], null, progressTracker);
            gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex], fallbackGameDataProvider, progressTracker);

            return gameData;
        });

        gameDataPromise.ResultReady += (gameData) =>
        {
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

            var flagsData = new DataReader(FileProvider.GetFlagsData());
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

            musicInitialized = true;
            musicManager.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
            musicManager.Enabled = musicManager.Available && configuration.Music;
            if (configuration.ShowPyrdacorLogo)
            {
                logoPyrdacor = new LogoPyrdacor(musicManager, musicManager.GetPyrdacorSong());
                additionalPalettes = new Graphic[2] { logoPyrdacor.Palettes[0], flagsPalette };
            }
            else
            {
                additionalPalettes = new Graphic[2] { new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] }, flagsPalette };
            }

            fontProvider ??= new IngameFontProvider(new DataReader(FileProvider.GetIngameFontData()), gameData.FontProvider.GetFont());

            switchRenderViewAction = () =>
            {
                loadingBar.Destroy();
                loadingBar = null;

                renderView = CreateRenderView(gameData, configuration, gameData.GraphicProvider, fontProvider, additionalPalettes, () =>
                {
                    textureAtlasManager.AddUIOnly(gameData.GraphicProvider, fontProvider);
                    logoPyrdacor?.Initialize(textureAtlasManager);
                    AdvancedLogo.Initialize(textureAtlasManager);
                    textureAtlasManager.AddFromGraphics(Layer.Misc, new Dictionary<uint, Graphic>
                    {
                        { 1u, flagsGraphic }
                    });
                    textureAtlasManager.AddFromGraphics(Layer.MobileOverlays, new Dictionary<uint, Graphic>
                        {
                            { 0u, FileProvider.GetDonateButton() }
                        });
                    return textureAtlasManager;
                });
                renderView.AvailableFullscreenModes = new();
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
                if (configuration.GameVersionIndex < 0 || configuration.GameVersionIndex >= gameVersions.Count)
                    configuration.GameVersionIndex = 0;

                var cursor = new InvisibleCursor(renderView, gameData.CursorHotspots, textureAtlasManager);

                WaitForTask(() =>
                {
                    while (logoPyrdacor != null)
                        Thread.Sleep(100);
                }, () =>
                {
                    versionSelector = new VersionSelector(gameVersion, renderView, textureAtlasManager,
                        gameVersions, cursor, configuration.GameVersionIndex, configuration.SaveOption, configuration);
                    versionSelector.Closed += (gameVersionIndex, gameData, _) =>
                    {
                        donateButton?.Delete();
                        donateButton = null;
                        var gameVersion = gameVersions[gameVersionIndex];
                        configuration.SaveOption = SaveOption.ProgramFolder;
                        configuration.GameVersionIndex = gameVersionIndex;
                        selectHandler?.Invoke(gameData, Configuration.GetSavePath(Configuration.GetVersionSavegameFolder(gameVersion)),
                            gameVersion.Language, gameVersion.Features);
                    };

                    // Donate button
                    var textureAtlas = textureAtlasManager.GetOrCreate(Layer.MobileOverlays);
                    donateButton = renderView.SpriteFactory.CreateWithAlpha(64, 18, 200);
                    donateButton.Layer = renderView.GetLayer(Layer.MobileOverlays);
                    donateButton.X = (Global.VirtualScreenWidth - 58) / 2;
                    donateButton.Y = Global.VirtualScreenHeight - donateButton.Height - 16;
                    donateButton.TextureAtlasOffset = textureAtlas.GetOffset(0u);
                    donateButton.Visible = true;
                });
            };

            logoPyrdacor?.PlayMusic();
        };

        return true;
    }

    bool TestDonateButtonClick(Position mousePosition)
    {
        if (donateButton == null || !donateButton.Visible)
            return false;

        var position = renderView.ScreenToGame(mousePosition);

        if (new Rect(donateButton.X, donateButton.Y, 58, donateButton.Height).Contains(position))
        {
            OpenDonationLink?.Invoke();
            return true;
        }

        return false;
    }

    void InitGlyphs(IFontProvider fontProvider, TextureAtlasManager textureAtlasManager = null)
    {
        int glyphCount = fontProvider.GetFont().GlyphCount;
        var textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Text);
        renderView.RenderTextFactory.GlyphTextureMapping = Enumerable.Range(0, glyphCount).ToDictionary(x => (byte)x, x => textureAtlas.GetOffset((uint)x));
        var digitTextureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.SmallDigits);
        renderView.RenderTextFactory.DigitGlyphTextureMapping = Enumerable.Range(0, 10).ToDictionary(x => (byte)(ExecutableData.DigitGlyphOffset + x), x => digitTextureAtlas.GetOffset((uint)x));
    }

    GameRenderView CreateRenderView(IGameData gameData, IConfiguration configuration, IGraphicProvider graphicProvider,
        IFontProvider fontProvider, Graphic[] additionalPalettes, Func<TextureAtlasManager> textureAtlasManagerProvider)
    {
        bool AnyIntroActive() => fantasyIntro != null || logoPyrdacor != null || advancedLogo != null;
        var useFrameBuffer = true;
        var useEffects = configuration.Effects != Effects.None;
        var renderView = new GameRenderView(this, gameData, graphicProvider, fontProvider,
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
            //if (Fullscreen) // This needs a little help
            Game.OnMouseMove(ConvertMousePosition(mouse.Position), GetMouseButtons(mouse));
        }
    }

    void Window_Load()
    {
        window.MakeCurrent();

        // Setup input
        SetupInput(window.CreateInput());

        var platform = Silk.NET.Windowing.Window.GetWindowPlatform(true);
        var fullscreenSize = platform.GetMainMonitor().Bounds.Size;

        configuration.FullscreenWidth = fullscreenSize.X;
        configuration.FullscreenHeight = fullscreenSize.Y;

        var gl = Silk.NET.OpenGL.GL.GetApi(GLContext);
        gl.Viewport(new System.Drawing.Size(window.FramebufferSize.X, window.FramebufferSize.Y));
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(Silk.NET.OpenGL.ClearBufferMask.ColorBufferBit);
        GLContext.SwapBuffers();

        configuration.Width = Width = fullscreenSize.X;
        configuration.Height = Height = fullscreenSize.Y;

        // Create light weight render view for preloading data.
        // It is basically used to show the loading bar.
        {
            var useFrameBuffer = true;
            var useEffects = configuration.Effects != Effects.None;
            var preloadTextureAtlasManager = TextureAtlasManager.CreateEmpty();
            var preLoadRenverView = new RenderView(this, null, () =>
            {
                LoadingBar.Initialize(preloadTextureAtlasManager);
                return preloadTextureAtlasManager;
            }, window.FramebufferSize.X, window.FramebufferSize.Y, new Size(window.Size.X, window.Size.Y), ref useFrameBuffer, ref useEffects,
            () => KeyValuePair.Create(0, 0), () => 0, []);

            loadingBar = new(preLoadRenverView);
            window.DoRender();
        }

        preloadAction = () =>
        {
            var builtinVersionReader = new BinaryReader(FileProvider.GetVersions());

            loadingBar.SetProgress(0.05f);

            var versionsPromise = ResourceProvider.GetResource(() =>
            {
                List<BuiltinVersion> versions = null;

                if (builtinVersionReader != null)
                {
                    var versionLoader = new BuiltinVersionLoader();
                    versions = versionLoader.Load(builtinVersionReader);
                }

                return versions;
            });

            versionsPromise.ResultReady += (versions) =>
            {
                loadingBar.SetProgress(0.1f);

                musicManager = musicManagerFactory?.Invoke();

                loadingBar.SetProgress(0.15f);

                if (ShowVersionSelector(versions, (gameData, savePath, gameLanguage, features) =>
                {
                    try
                    {
                        if (loadingBar != null)
                        {
                            loadingBar.Destroy();
                            loadingBar = null;
                        }

                        builtinVersionReader?.Dispose();
                        renderView?.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        var advancedDiffsReader = gameData.Advanced ? new BinaryReader(FileProvider.GetAdvancedDiffsData()) : null;
                        StartGame(gameData as GameData, savePath, gameLanguage, features, advancedDiffsReader);
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("Ambermoon", "Error starting game: " + ex.ToString());
                        Quit();
                        return;
                    }
                    versionSelector = null;
                }, out var textureAtlasManager))
                {
                    // empty
                }
            };
        };
        preloading = true;
    }

    void Window_Render(double delta)
    {
        if (State != ActivityState.Stopped)
        {
            if (loadingBar != null)
                loadingBar.Render();
            else if (versionSelector != null)
                versionSelector.Render();
            else if (mainMenu != null)
                mainMenu.Render();
            else if (Game != null)
                renderView.Render(Game.ViewportOffset);
            else if (renderView != null)
                renderView.Render(null);

            window.SwapBuffers();
        }
    }

    void Window_Update(double delta)
    {
        if (preloading)
        {
            preloadAction?.Invoke();
            preloading = false;
            return;
        }
        else if (switchRenderViewAction != null)
        {
            switchRenderViewAction();
            switchRenderViewAction = null;
        }

        if (initializeErrorTime != null)
        {
            if ((DateTime.Now - initializeErrorTime.Value).TotalSeconds > 5)
                Quit();
            return;
        }

        if (loadingBar == null)
        {
            if (logoPyrdacor != null)
                logoPyrdacor.Update(renderView, () => logoPyrdacor = null);

            if (versionSelector != null)
                versionSelector.Update(delta);
            else if (logoPyrdacor == null && fantasyIntro != null)
                fantasyIntro.Update(delta);
            else if (logoPyrdacor == null && advancedLogo != null)
                advancedLogo.Update(renderView, () => { advancedLogo = null; renderView.ShowImageLayerOnly = false; });
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
                        Android.Util.Log.Error("Ambermoon", "Error creating game: " + ex.Message);
                        Quit();
                        return;
                    }
                    mainMenu?.Destroy();
                    mainMenu = null;
                    gameCreator = null;
                }
            }
            else if (Game != null)
            {
                Game.OnMobileMove(touchPad?.Direction);
                Game.Update(delta);
                touchPad?.Update(Game);
            }
        }
    }

    void Window_Resize(WindowDimension size)
    {
        try
        {
            renderView?.Resize(window.FramebufferSize.X, window.FramebufferSize.Y, size.X, size.Y);
        }
        catch
        {
            // ignore
        }
    }

    void Window_FramebufferResize(WindowDimension size)
    {
        try
        {
            renderView?.Resize(size.X, size.Y);
        }
        catch
        {
            // ignore
        }
    }

    void OnStateChanged()
    {
        if (state != ActivityState.Active)
            Game?.PauseGame();
        else
            Game?.ResumeGame();
    }

    void DoEvents()
    {
        lock (touchActions)
        {
            foreach (var touchAction in touchActions)
                touchAction();
            touchActions.Clear();
        }
        window.DoEvents();
    }

    public void Run(Configuration configuration, Func<MusicManager> musicManagerFactory,
        Action nameResetHandler, Action afterInitHandler)
    {
        this.configuration = configuration;
        this.musicManagerFactory = musicManagerFactory;
        var screenSize = configuration.GetScreenSize();
        Width = screenSize.Width;
        Height = screenSize.Height;

#if GLES
        var api = new GraphicsAPI
            (ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0));
#else
        var api = GraphicsAPI.Default;
#endif
        var videoMode = new VideoMode(60);
        var options = new WindowOptions(true, new WindowDimension(0, 0),
            new WindowDimension(Width, Height), 60.0, 120.0, api, gameVersion,
            WindowState.Normal, WindowBorder.Fixed, true, false, videoMode, 24);
        options.WindowClass = "Ambermoon.net";

        try
        {
            SdlWindowing.RegisterPlatform();
            SdlInput.RegisterPlatform();
            SdlWindowing.Use();
            nameResetHandler?.Invoke();
            window = Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
            window.Load += nameResetHandler;
            window.Load += Window_Load;
            window.Load += afterInitHandler;
            window.Render += Window_Render;
            window.Update += Window_Update;
            window.Resize += Window_Resize;
            window.FramebufferResize += Window_FramebufferResize;
            window.Closing += () => musicManager?.Stop();

            window.Initialize();
            window.Run(() =>
            {
                DoEvents();
                if (!window.IsClosing)
                    window.DoUpdate();
                if (!window.IsClosing)
                    window.DoRender();
            });
            DoEvents();
            window.Reset();
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
            Util.SafeCall(() => tutorialFinger?.Destroy());
            Util.SafeCall(() => touchPad?.Destroy());
            Util.SafeCall(() =>
        {
            infoText?.Delete();
            infoText = null;
        });
            Util.SafeCall(() => window?.Dispose());
        }
    }
}
