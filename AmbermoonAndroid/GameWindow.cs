using Ambermoon;
using Ambermoon.Audio.Android;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Ambermoon.UI;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Reflection;
using Key = Ambermoon.Key;
using MousePosition = System.Numerics.Vector2;
using TextReader = Ambermoon.Data.Legacy.Serialization.TextReader;
using WindowDimension = Silk.NET.Maths.Vector2D<int>;

namespace AmbermoonAndroid
{
    class GameWindow : IContextProvider
    {
        string gameVersion = "Ambermoon.net";
        Configuration configuration;
        RenderView renderView;
        IView window;
        IKeyboard keyboard = null;
        IMouse mouse = null;
        ICursor cursor = null;
        MainMenu mainMenu = null;
        Func<Game> gameCreator = null;
        SongManager songManager = null;
        AudioOutput audioOutput = null;
        IRenderText infoText = null;
        DateTime? initializeErrorTime = null;
        List<Size> availableFullscreenModes = null;
        DateTime lastRenderTime = DateTime.MinValue;
        TimeSpan lastRenderDuration = TimeSpan.Zero;
        bool trapMouse = false;
        FloatPosition trappedMouseOffset = null;
        FloatPosition trappedMouseLastPosition = null;
        LogoPyrdacor logoPyrdacor = null;
        Graphic[] logoPalettes;

        public string Identifier { get; }
        public IGLContext GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }
        VersionSelector versionSelector = null;
        public Game Game { get; private set; }
        public bool Fullscreen
        {
            get => false;
            set
            {
                configuration.Fullscreen = false;

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
            if (logoPyrdacor != null)
            {
                logoPyrdacor?.Cleanup();
                logoPyrdacor = null;
            }
            else if (versionSelector != null)
                versionSelector.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
            else if (Game != null)
                Game.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
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

        internal void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (logoPyrdacor != null)
            {
                logoPyrdacor?.Cleanup();
                logoPyrdacor = null;
            }
            else if (versionSelector != null)
                versionSelector.OnMouseDown(position, buttons);
            else if (mainMenu != null)
                mainMenu.OnMouseDown(position, buttons);
            else if (Game != null)
                Game.OnMouseDown(position, buttons);
        }

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            OnMouseDown(ConvertMousePosition(position), GetMouseButtons(mouse));
        }

        internal void OnMouseUp(Position position, MouseButtons buttons)
        {
            if (versionSelector != null)
                versionSelector.OnMouseUp(position, buttons);
            else if (mainMenu != null)
                mainMenu.OnMouseUp(position, buttons);
            else if (Game != null)
                Game.OnMouseUp(position, buttons);
        }

        void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            OnMouseUp(ConvertMousePosition(position), ConvertMouseButtons(button));
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

            if (versionSelector != null)
                versionSelector.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (mainMenu != null)
                mainMenu.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
            else if (Game != null)
                Game.OnMouseMove(ConvertMousePosition(position), GetMouseButtons(mouse));
        }

        internal void OnMouseScroll(Position position, int deltaX, int deltaY)
        {
            if (versionSelector != null)
                versionSelector.OnMouseWheel(deltaX, deltaY, position);
            else if (Game != null)
                Game.OnMouseWheel(deltaX, deltaY, position);
        }

        void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
        {
            var position = trapMouse ? new MousePosition(trappedMouseOffset.X, trappedMouseOffset.Y) : mouse.Position;

            OnMouseScroll(ConvertMousePosition(position), Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y));
        }

        void ShowMainMenu(IRenderView renderView, Ambermoon.Render.Cursor cursor, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            Font introFont, string[] mainMenuTexts, bool canContinue, Action<bool> startGameAction, GameLanguage gameLanguage)
        {
            void PlayMusic(Song song)
            {
                if (configuration.Music)
                    songManager.GetSong(song)?.Play(audioOutput);

                if (infoText != null)
                    infoText.Visible = false;
            }

            mainMenu = new MainMenu(renderView, cursor, paletteIndices, introFont, mainMenuTexts, canContinue,
                GetText(gameLanguage, 1), GetText(gameLanguage, 2), PlayMusic, configuration.ShowThalionLogo);
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
                    /*case MainMenu.CloseAction.Intro:
                        // TODO
                        musicCache.GetSong(Data.Enumerations.Song.Intro)?.Play(audioOutput);
                        break;*/
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
                    "Musik wird geladen ...",
                    "Starte Spiel ...",
                    "Bereite neues Spiel vor ..."
                }
            },
            { GameLanguage.English, new string[]
                {
                    "Loading music ...",
                    "Starting game ...",
                    "Preparing new game ..."
                }
            }
        };

        static string GetText(GameLanguage gameLanguage, int index) => LoadingTexts[gameLanguage][index];

        void StartGame(GameData gameData, string savePath, GameLanguage gameLanguage, Features features)
        {
            // Load intro data
            var introData = new IntroData(gameData);
            var introFont = new Font(FileProvider.GetIntroFontData(), 12);

            // Load outro data
            var outroData = new OutroData(gameData);
            var outroFont = new Font(outroData.Glyphs, 6, 0);
            var outroFontLarge = new Font(outroData.LargeGlyphs, 10, (uint)outroData.Glyphs.Count);

            // Load game data
            var executableData = ExecutableData.FromGameData(gameData);
            var graphicProvider = new GraphicProvider(gameData, executableData, introData, outroData);
            var fontProvider = new FontProvider(executableData);

            if (audioOutput == null)
            {
                audioOutput = new AudioOutput(1, 44100);
                audioOutput.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
                audioOutput.Enabled = audioOutput.Available && configuration.Music;
                if (configuration.ShowPyrdacorLogo)
                {
                    logoPyrdacor = new LogoPyrdacor(audioOutput, SongManager.LoadCustomSong(new DataReader(FileProvider.GetSongData()), 0, false, false));
                    logoPalettes = logoPyrdacor.Palettes;
                }
                else
                {
                    logoPalettes = new Graphic[1] { new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] } };
                }
            }

            // Create render view
            renderView = CreateRenderView(gameData, configuration, graphicProvider, fontProvider, logoPalettes, () =>
            {
                var textureAtlasManager = TextureAtlasManager.Instance;
                textureAtlasManager.AddAll(gameData, graphicProvider, fontProvider, introFont.GlyphGraphics,
                    introData.Graphics.ToDictionary(g => (uint)g.Key, g => g.Value));
                logoPyrdacor?.Initialize(textureAtlasManager);
                return textureAtlasManager;
            });
            renderView.AvailableFullscreenModes = availableFullscreenModes;

            InitGlyphs();

            songManager = new SongManager(gameData);

            var text = renderView.TextProcessor.CreateText("");
            infoText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text), text, Ambermoon.Data.Enumerations.Color.White, false,
                new Rect(0, Global.VirtualScreenHeight / 2 - 3, Global.VirtualScreenWidth, 6), TextAlign.Center);
            infoText.DisplayLayer = 254;
            infoText.Visible = false;

            RunTask(() =>
            {
                try
                {
                    var textDictionary = TextDictionary.Load(new TextDictionaryReader(), gameData.Dictionaries.First()); // TODO: maybe allow choosing the language later?
                    foreach (var objectTextFile in gameData.Files["Object_texts.amb"].Files)
                        executableData.ItemManager.AddTexts((uint)objectTextFile.Key, TextReader.ReadTexts(objectTextFile.Value));
                    var savegameManager = new SavegameManager(savePath);
                    savegameManager.GetSavegameNames(gameData, out int currentSavegame, 10);
                    bool canContinue = currentSavegame != 0;
                    var cursor = new Ambermoon.Render.Cursor(renderView, executableData.Cursors.Entries.Select(c => new Position(c.HotspotX, c.HotspotY)).ToList().AsReadOnly());
                    cursor.UpdatePosition(ConvertMousePosition(mouse.Position), null);
                    cursor.Type = Ambermoon.Data.CursorType.None;

                    void SetupGameCreator(bool continueGame)
                    {
                        try
                        {
                            var mapManager = new MapManager(gameData, new MapReader(), new TilesetReader(), new LabdataReader());
                            var savegameSerializer = new SavegameSerializer();
                            var dataNameProvider = new DataNameProvider(executableData);
                            var characterManager = new CharacterManager(gameData, graphicProvider);
                            var places = Places.Load(new PlacesReader(), renderView.GameData.Files["Place_data"].Files[1]);
                            var lightEffectProvider = new LightEffectProvider(executableData);

                            gameCreator = () =>
                            {
                                var game = new Game(configuration, gameLanguage, renderView, mapManager, executableData.ItemManager,
                                    characterManager, savegameManager, savegameSerializer, dataNameProvider, textDictionary, places,
                                    cursor, lightEffectProvider, audioOutput, songManager, FullscreenChangeRequest, ChangeResolution,
                                    QueryPressedKeys, new OutroFactory(renderView, outroData, outroFont, outroFontLarge), features);
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
                                };
                                game.DrugTicked += Drug_Ticked;
                                mainMenu.GameDataLoaded = true;
                                game.Run(continueGame, ConvertMousePosition(mouse.Position));
                                return game;
                            };
                        }
                        catch (Exception ex)
                        {
                            gameCreator = () => throw ex;
                        }
                    }

                    while (logoPyrdacor != null)
                        Thread.Sleep(100);

                    ShowMainMenu(renderView, cursor, IntroData.GraphicPalettes, introFont,
                        introData.Texts.Skip(8).Take(4).Select(t => t.Value).ToArray(), canContinue, continueGame =>
                    {
                        cursor.Type = Ambermoon.Data.CursorType.None;
                        mainMenu.FadeOutAndDestroy(continueGame, () => RunTask(() => SetupGameCreator(continueGame)));
                    }, gameLanguage);
                }
                catch (Exception ex)
                {             
                    try
                    {
                        string error = @"Error loading data   \(o_o\)";
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

        bool ShowVersionSelector(Action<IGameData, string, GameLanguage, Features> selectHandler)
        {
            var versionLoader = new BuiltinVersionLoader();
            var versions = versionLoader.Load();
            var gameData = new GameData();
            var dataPath = configuration.UseDataPath ? configuration.DataPath : Configuration.ExecutableDirectoryPath;

            if (versions.Count == 0)
            {
                // no versions
                versionLoader.Dispose();
                gameData.Load(dataPath);
                selectHandler?.Invoke(gameData, GetSavePath(Configuration.VersionSavegameFolders[4]), gameData.Language.ToGameLanguage(),
                    gameData.Advanced ? Features.AmbermoonAdvanced : Features.None);
                return false;
            }

            GameData LoadBuiltinVersionData(BuiltinVersion builtinVersion, Func<IGameData> fallbackGameDataProvider)
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

            if (configuration.GameVersionIndex < 0 || configuration.GameVersionIndex > 2)
                configuration.GameVersionIndex = 0;

            GameData.GameDataInfo? additionalVersionInfo = null;

            try
            {
                additionalVersionInfo = GameData.GetInfo(dataPath);
            }
            catch
            {
                if (configuration.GameVersionIndex == 4)
                    configuration.GameVersionIndex = 0;
            }

            if (configuration.GameVersionIndex < 4)
            {
                gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex],
                    configuration.GameVersionIndex == 0 ? (Func<IGameData>)null : () => LoadBuiltinVersionData(versions[0], null));
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

            var builtinVersionDataProviders = new Func<IGameData>[4];
            builtinVersionDataProviders[0] = () => configuration.GameVersionIndex == 0 ? gameData : LoadBuiltinVersionData(versions[0], null);
            builtinVersionDataProviders[1] = () => configuration.GameVersionIndex == 1 ? gameData : LoadBuiltinVersionData(versions[1], builtinVersionDataProviders[0]);
            builtinVersionDataProviders[2] = () => configuration.GameVersionIndex == 2 ? gameData : LoadBuiltinVersionData(versions[2], null);
            builtinVersionDataProviders[3] = () => configuration.GameVersionIndex == 3 ? gameData : LoadBuiltinVersionData(versions[3], builtinVersionDataProviders[2]);
            var executableData = ExecutableData.FromGameData(gameData);
            var graphicProvider = new GraphicProvider(gameData, executableData, null, null);
            var textureAtlasManager = TextureAtlasManager.CreateEmpty();
            var fontProvider = new FontProvider(executableData);
            foreach (var objectTextFile in gameData.Files["Object_texts.amb"].Files)
                executableData.ItemManager.AddTexts((uint)objectTextFile.Key, TextReader.ReadTexts(objectTextFile.Value));

            audioOutput = new AudioOutput(1, 44100);
            audioOutput.Volume = Util.Limit(0, configuration.Volume, 100) / 100.0f;
            audioOutput.Enabled = audioOutput.Available && configuration.Music;
            if (configuration.ShowPyrdacorLogo)
            {
                logoPyrdacor = new LogoPyrdacor(audioOutput, SongManager.LoadCustomSong(new DataReader(FileProvider.GetSongData()), 0, false, false));
                logoPalettes = logoPyrdacor.Palettes;
            }
            else
            {
                logoPalettes = new Graphic[1] { new Graphic { Width = 32, Height = 1, IndexedGraphic = false, Data = new byte[32 * 4] } };
            }

            renderView = CreateRenderView(gameData, configuration, graphicProvider, fontProvider, logoPalettes, () =>
            {
                textureAtlasManager.AddUIOnly(graphicProvider, fontProvider);
                logoPyrdacor?.Initialize(textureAtlasManager);
                return textureAtlasManager;
            });
            renderView.AvailableFullscreenModes = availableFullscreenModes;
            InitGlyphs(textureAtlasManager);
            var gameVersions = new List<GameVersion>(3);
            for (int i = 0; i < versions.Count; ++i)
            {
                var builtinVersion = versions[i];
                gameVersions.Add(new GameVersion
                {
                    Version = builtinVersion.Version,
                    Language = builtinVersion.Language,
                    Info = builtinVersion.Info,
                    DataProvider = builtinVersionDataProviders[i],
                    Features = builtinVersion.Features
                });
            }
            if (additionalVersionInfo != null)
            {
                gameVersions.Add(new GameVersion
                {
                    Version = additionalVersionInfo.Value.Version,
                    Language = additionalVersionInfo.Value.Language,
                    Info = "From external data",
                    DataProvider = configuration.GameVersionIndex == 4 ? (Func<IGameData>)(() => gameData) : LoadGameDataFromDataPath,
                    Features = additionalVersionInfo.Value.Advanced ? Features.AmbermoonAdvanced : Features.None
                });
            }
            var cursor = new Ambermoon.Render.Cursor(renderView, executableData.Cursors.Entries.Select(c => new Position(c.HotspotX, c.HotspotY)).ToList().AsReadOnly(),
                textureAtlasManager);

            RunTask(() =>
            {
                while (logoPyrdacor != null)
                    Thread.Sleep(100);

                versionSelector = new VersionSelector(gameVersion, renderView, textureAtlasManager, gameVersions, cursor, configuration.GameVersionIndex, configuration.SaveOption);
                versionSelector.Closed += (gameVersionIndex, gameData, saveInDataPath) =>
                {
                    configuration.SaveOption = saveInDataPath ? SaveOption.DataFolder : SaveOption.ProgramFolder;
                    configuration.GameVersionIndex = gameVersionIndex;
                    selectHandler?.Invoke(gameData, saveInDataPath ? dataPath : GetSavePath(Configuration.VersionSavegameFolders[gameVersionIndex]),
                        gameVersions[gameVersionIndex].Language.ToGameLanguage(), gameVersions[gameVersionIndex].Features);
                    versionLoader.Dispose();
                };
            });

            return true;
        }

        void InitGlyphs(TextureAtlasManager textureAtlasManager = null)
        {
            var textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Text);
            renderView.RenderTextFactory.GlyphTextureMapping = Enumerable.Range(0, 94).ToDictionary(x => (byte)x, x => textureAtlas.GetOffset((uint)x));
            renderView.RenderTextFactory.DigitGlyphTextureMapping = Enumerable.Range(0, 10).ToDictionary(x => (byte)(ExecutableData.DigitGlyphOffset + x), x => textureAtlas.GetOffset(100 + (uint)x));
        }

        RenderView CreateRenderView(GameData gameData, IConfiguration configuration, GraphicProvider graphicProvider,
            FontProvider fontProvider, Graphic[] additionalPalettes = null, Func<TextureAtlasManager> textureAtlasManagerProvider = null)
        {
            var useFrameBuffer = true;
            var useEffects = configuration.Effects != Effects.None;
            var renderView = new RenderView(this, gameData, graphicProvider,
                new TextProcessor(), textureAtlasManagerProvider, window.FramebufferSize.X, window.FramebufferSize.Y,
                new Size(window.Size.X, window.Size.Y), ref useFrameBuffer, ref useEffects,
                () => KeyValuePair.Create(logoPyrdacor != null ? 0 : (int)configuration.GraphicFilter, logoPyrdacor != null ? 0 : (int)configuration.GraphicFilterOverlay),
                () => (int)configuration.Effects, additionalPalettes, Ambermoon.Renderer.DeviceType.MobileLandscape,
                //Ambermoon.Renderer.SizingPolicy.FitRatioForceLandscape, Ambermoon.Renderer.OrientationPolicy.Fixed);
                Ambermoon.Renderer.SizingPolicy.FitRatio, Ambermoon.Renderer.OrientationPolicy.Fixed); // TODO
            if (!useFrameBuffer)
            {
                configuration.GraphicFilter = GraphicFilter.None;
                configuration.GraphicFilterOverlay = GraphicFilterOverlay.None;
            }
            if (!useEffects)
                configuration.Effects = Effects.None;
            return renderView;
        }

        static string GetSavePath(string version)
        {
            string suffix = $"Saves{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";
            string alternativeSuffix = $"SavesRemake{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";

            try
            {
                var path = Path.Combine(Configuration.ExecutableDirectoryPath, suffix);
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    path = Path.Combine(Configuration.ExecutableDirectoryPath, alternativeSuffix);
                    Directory.CreateDirectory(path);
                }
                return path;
            }
            catch
            {
                var path = Path.Combine(Configuration.FallbackConfigDirectory, suffix);
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    path = Path.Combine(Configuration.FallbackConfigDirectory, alternativeSuffix);
                    Directory.CreateDirectory(path);
                }
                return path;
            }
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
            //var windowIcon = new Silk.NET.Core.RawImage(16, 16, new Memory<byte>(Resources.WindowIcon));
            //window.SetWindowIcon(ref windowIcon);

            window.MakeCurrent();

            // Setup input
            SetupInput(window.CreateInput());

            availableFullscreenModes = new List<Size>();

            var fullscreenSize = availableFullscreenModes.OrderBy(r => r.Width * r.Height).LastOrDefault();

            if (fullscreenSize != null)
            {
                configuration.FullscreenWidth = fullscreenSize.Width;
                configuration.FullscreenHeight = fullscreenSize.Height;
            }

            if (configuration.Fullscreen)
            {
                ChangeFullscreenMode(true); // This will adjust the window
            }

            if (ShowVersionSelector((gameData, savePath, gameLanguage, features) =>
            {
                renderView?.Dispose();
                StartGame(gameData as GameData, savePath, gameLanguage, features);
                WindowMoved();
                versionSelector = null;
            }))
            {
                WindowMoved();
            }
        }

        void Window_Render(double delta)
        {
            const int refreshRate = 60;
            var timePerFrame = 1000.0 / refreshRate;

            window.VSync = lastRenderDuration.TotalMilliseconds <= timePerFrame;

            if (window.VSync)
            {
                var renderDuration = DateTime.Now - lastRenderTime;
                if (lastRenderDuration.TotalMilliseconds < 10 &&
                    renderDuration.TotalMilliseconds < timePerFrame - 4.0 * delta * 1000.0 - lastRenderDuration.TotalMilliseconds)
                    return;
            }

            var startRenderTime = DateTime.Now;

            if (versionSelector != null)
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

        void Window_Update(double delta)
        {
            if (initializeErrorTime != null)
            {
                if ((DateTime.Now - initializeErrorTime.Value).TotalSeconds > 5)
                    window.Close();
                return;
            }

            if (logoPyrdacor != null)
                logoPyrdacor.Update(renderView, () => logoPyrdacor = null);

            if (versionSelector != null)
                versionSelector.Update(delta);
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
                    catch
                    {
                        window.Close();
                        return;
                    }
                    mainMenu?.Destroy();
                    mainMenu = null;
                    gameCreator = null;
                }
            }
            else if (Game != null)
            {
                Game.Update(delta);
            }
        }

        void Window_Resize(WindowDimension size)
        {
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
            WindowMoved();
        }

        void WindowMoved()
        {
            if (renderView != null)
                renderView.MaxScreenSize = new Size(window.Size.X, window.Size.Y);
        }

        void UpdateWindow(IConfiguration configuration)
        {
            if (!Fullscreen)
            {
                var size = configuration.GetScreenSize();
                this.configuration.Width = Width = size.Width;
                this.configuration.Height = Height = size.Height;
            }

            renderView?.Resize(window.FramebufferSize.X, window.FramebufferSize.Y, window.Size.X, window.Size.Y);
        }

        public void Run(Configuration configuration)
        {
            this.configuration = configuration;
            var screenSize = configuration.GetScreenSize();
            Width = screenSize.Width;
            Height = screenSize.Height;

            var api = new GraphicsAPI
                (ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0));
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            gameVersion = $"Ambermoon.net v{version.Major}.{version.Minor}.{version.Build}";
            var videoMode = new VideoMode(60);
            var options = new WindowOptions(true, new WindowDimension(100, 100),
                new WindowDimension(Width, Height), 60.0, 60.0, api, gameVersion,
                WindowState.Normal, WindowBorder.Fixed, true, false, videoMode, 24);

            try
            {
                Silk.NET.Windowing.Sdl.SdlWindowing.Use();
                window = Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
                window.Load += Window_Load;
                window.Render += Window_Render;
                window.Update += Window_Update;
                window.Resize += Window_Resize;
                window.FramebufferResize += Window_FramebufferResize;
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
                            System.Threading.Thread.Sleep(1000);
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
