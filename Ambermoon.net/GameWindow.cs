using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Geometry;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Ambermoon.UI;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.Windowing.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TextReader = Ambermoon.Data.Legacy.Serialization.TextReader;

namespace Ambermoon
{
    class GameWindow : IContextProvider
    {
        Configuration configuration;
        RenderView renderView;
        IWindow window;
        IMouse mouse = null;
        ICursor cursor = null;
        MainMenu mainMenu = null;
        Func<Game> gameCreator = null;

        static readonly string[] VersionSavegameFolders = new string[3]
        {
            "german",
            "english",
            "external"
        };

        public string Identifier { get; }
        public IGLContext GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }
        VersionSelector versionSelector = null;
        public Game Game { get; private set; }
        public bool Fullscreen
        {
            get => configuration.Fullscreen;
            set
            {
                configuration.Fullscreen = value;
                window.WindowState = configuration.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;

                if (cursor != null)
                    cursor.CursorMode = value ? CursorMode.Disabled : CursorMode.Hidden;
            }
        }

        public GameWindow(string id = "MainWindow")
        {
            Identifier = id;
        }

        void SetupInput(IInputContext inputContext)
        {
            var keyboard = inputContext.Keyboards.FirstOrDefault(k => k.IsConnected);

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
                cursor.CursorMode = Fullscreen ? CursorMode.Disabled : CursorMode.Hidden;
                mouse.MouseDown += Mouse_MouseDown;
                mouse.MouseUp += Mouse_MouseUp;
                mouse.MouseMove += Mouse_MouseMove;
                mouse.Scroll += Mouse_Scroll;
            }
        }

        static KeyModifiers GetModifiers(IKeyboard keyboard)
        {
            var modifiers = KeyModifiers.None;

            if (keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.ShiftLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.ShiftRight))
                modifiers |= KeyModifiers.Shift;
            if (keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.ControlLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.ControlRight))
                modifiers |= KeyModifiers.Control;
            if (keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.AltLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Common.Key.AltRight))
                modifiers |= KeyModifiers.Alt;

            return modifiers;
        }

        static Key ConvertKey(Silk.NET.Input.Common.Key key) => key switch
        {
            Silk.NET.Input.Common.Key.Left => Key.Left,
            Silk.NET.Input.Common.Key.Right => Key.Right,
            Silk.NET.Input.Common.Key.Up => Key.Up,
            Silk.NET.Input.Common.Key.Down => Key.Down,
            Silk.NET.Input.Common.Key.Escape => Key.Escape,
            Silk.NET.Input.Common.Key.F1 => Key.F1,
            Silk.NET.Input.Common.Key.F2 => Key.F2,
            Silk.NET.Input.Common.Key.F3 => Key.F3,
            Silk.NET.Input.Common.Key.F4 => Key.F4,
            Silk.NET.Input.Common.Key.F5 => Key.F5,
            Silk.NET.Input.Common.Key.F6 => Key.F6,
            Silk.NET.Input.Common.Key.F7 => Key.F7,
            Silk.NET.Input.Common.Key.F8 => Key.F8,
            Silk.NET.Input.Common.Key.F9 => Key.F9,
            Silk.NET.Input.Common.Key.F10 => Key.F10,
            Silk.NET.Input.Common.Key.F11 => Key.F11,
            Silk.NET.Input.Common.Key.F12 => Key.F12,
            Silk.NET.Input.Common.Key.Enter => Key.Return,
            Silk.NET.Input.Common.Key.KeypadEnter => Key.Return,
            Silk.NET.Input.Common.Key.Delete => Key.Delete,
            Silk.NET.Input.Common.Key.Backspace => Key.Backspace,
            Silk.NET.Input.Common.Key.Tab => Key.Tab,
            Silk.NET.Input.Common.Key.Keypad0 => Key.Num0,
            Silk.NET.Input.Common.Key.Keypad1 => Key.Num1,
            Silk.NET.Input.Common.Key.Keypad2 => Key.Num2,
            Silk.NET.Input.Common.Key.Keypad3 => Key.Num3,
            Silk.NET.Input.Common.Key.Keypad4 => Key.Num4,
            Silk.NET.Input.Common.Key.Keypad5 => Key.Num5,
            Silk.NET.Input.Common.Key.Keypad6 => Key.Num6,
            Silk.NET.Input.Common.Key.Keypad7 => Key.Num7,
            Silk.NET.Input.Common.Key.Keypad8 => Key.Num8,
            Silk.NET.Input.Common.Key.Keypad9 => Key.Num9,
            Silk.NET.Input.Common.Key.PageUp => Key.PageUp,
            Silk.NET.Input.Common.Key.PageDown => Key.PageDown,
            Silk.NET.Input.Common.Key.Home => Key.Home,
            Silk.NET.Input.Common.Key.End => Key.End,
            Silk.NET.Input.Common.Key.Space => Key.Space,
            Silk.NET.Input.Common.Key.W => Key.W,
            Silk.NET.Input.Common.Key.A => Key.A,
            Silk.NET.Input.Common.Key.S => Key.S,
            Silk.NET.Input.Common.Key.D => Key.D,
            _ => Key.Invalid,
        };

        void Keyboard_KeyChar(IKeyboard keyboard, char keyChar)
        {
            if (versionSelector != null)
                versionSelector.OnKeyChar(keyChar);
            else if (Game != null)
                Game.OnKeyChar(keyChar);
        }

        void Keyboard_KeyDown(IKeyboard keyboard, Silk.NET.Input.Common.Key key, int value)
        {
            if (key == Silk.NET.Input.Common.Key.F11)
                Fullscreen = !Fullscreen;
            else
            {
                if (versionSelector != null)
                    versionSelector.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
                else if (Game != null)
                    Game.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
            }
        }

        void Keyboard_KeyUp(IKeyboard keyboard, Silk.NET.Input.Common.Key key, int value)
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

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            if (versionSelector != null)
                versionSelector.OnMouseDown(mouse.Position.Round(), GetMouseButtons(mouse));
            else if (mainMenu != null)
                mainMenu.OnMouseDown(mouse.Position.Round(), ConvertMouseButtons(button));
            else if (Game != null)
                Game.OnMouseDown(mouse.Position.Round(), GetMouseButtons(mouse));
        }

        void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
            if (versionSelector != null)
                versionSelector.OnMouseUp(mouse.Position.Round(), ConvertMouseButtons(button));
            else if (mainMenu != null)
                mainMenu.OnMouseUp(mouse.Position.Round(), ConvertMouseButtons(button));
            else if (Game != null)
                Game.OnMouseUp(mouse.Position.Round(), ConvertMouseButtons(button));
        }

        void Mouse_MouseMove(IMouse mouse, System.Drawing.PointF position)
        {
            if (versionSelector != null)
                versionSelector.OnMouseMove(position.Round(), GetMouseButtons(mouse));
            else if (mainMenu != null)
                mainMenu.OnMouseMove(mouse.Position.Round(), GetMouseButtons(mouse));
            else if (Game != null)
                Game.OnMouseMove(position.Round(), GetMouseButtons(mouse));
        }

        void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
        {
            if (versionSelector != null)
                versionSelector.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), mouse.Position.Round());
            else if (Game != null)
                Game.OnMouseWheel(Util.Round(wheelDelta.X), Util.Round(wheelDelta.Y), mouse.Position.Round());
        }

        void ShowMainMenu(IRenderView renderView, Render.Cursor cursor, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            IntroFont introFont, string[] mainMenuTexts, bool canContinue, Action<bool> startGameAction)
        {
            mainMenu = new MainMenu(renderView, cursor, paletteIndices, introFont, mainMenuTexts, canContinue);
            mainMenu.Closed += closeAction =>
            {
                switch (closeAction)
                {
                    case MainMenu.CloseAction.NewGame:
                        startGameAction?.Invoke(false);
                        break;
                    case MainMenu.CloseAction.Continue:
                        startGameAction?.Invoke(true);
                        break;
                    case MainMenu.CloseAction.Intro:
                        // TODO
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

        void StartGame(GameData gameData, string savePath, GameLanguage gameLanguage)
        {
            // Load intro data
            var introData = new IntroData(gameData);
            var introFont = new IntroFont();

            // Load game data
            var executableData = new ExecutableData(AmigaExecutable.Read(gameData.Files["AM2_CPU"].Files[1]));
            var graphicProvider = new GraphicProvider(gameData, executableData, introData);
            var textDictionary = TextDictionary.Load(new TextDictionaryReader(), gameData.Dictionaries.First()); // TODO: maybe allow choosing the language later?
            var fontProvider = new FontProvider(executableData);
            foreach (var objectTextFile in gameData.Files["Object_texts.amb"].Files)
                executableData.ItemManager.AddTexts((uint)objectTextFile.Key, TextReader.ReadTexts(objectTextFile.Value));

            // Create render view
            renderView = CreateRenderView(gameData, executableData, graphicProvider, fontProvider, () =>
            {
                var textureAtlasManager = TextureAtlasManager.Instance;
                textureAtlasManager.AddAll(gameData, graphicProvider, fontProvider, introFont.GlyphGraphics,
                    introData.Graphics.ToDictionary(g => (uint)g.Key, g => g.Value));
                return textureAtlasManager;
            });
            InitGlyphs();
            var cursor = new Render.Cursor(renderView, executableData.Cursors.Entries.Select(c => new Position(c.HotspotX, c.HotspotY)).ToList().AsReadOnly());
            cursor.UpdatePosition(mouse.Position.Round(), null);
            var savegameManager = new SavegameManager(savePath);
            savegameManager.GetSavegameNames(gameData, out int currentSavegame);
            bool canContinue = currentSavegame != 0;

            ShowMainMenu(renderView, cursor, IntroData.GraphicPalettes, introFont,
                introData.Texts.Skip(8).Take(4).Select(t => t.Value).ToArray(), canContinue, continueGame =>
            {
                cursor.Type = Data.CursorType.None;
                mainMenu.FadeOutAndDestroy();
                Task.Run(() =>
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
                                cursor, lightEffectProvider);
                            game.QuitRequested += window.Close;
                            game.MouseTrappedChanged += (bool trapped, Position position) =>
                            {
                                this.cursor.CursorMode = Fullscreen || trapped ? CursorMode.Disabled : CursorMode.Hidden;

                                mouse.Position = new System.Drawing.PointF(position.X, position.Y);
                            };
                            game.ConfigurationChanged += (configuration, windowChange) =>
                            {
                                if (windowChange)
                                {
                                    UpdateWindow(configuration);
                                    Fullscreen = configuration.Fullscreen;
                                }
                            };
                            game.Run(continueGame, mouse.Position.Round());
                            return game;
                        };
                    }
                    catch (Exception ex)
                    {
                        gameCreator = () => throw ex;
                    }
                });
            });
        }

        void ShowVersionSelector(Action<IGameData, string, GameLanguage> selectHandler)
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
                selectHandler?.Invoke(gameData, GetSavePath(VersionSavegameFolders[2]), gameData.Language.ToGameLanguage());
                return;
            }

            GameData LoadBuiltinVersionData(BuiltinVersion builtinVersion)
            {
                var gameData = new GameData();
                builtinVersion.SourceStream.Position = builtinVersion.Offset;
                var buffer = new byte[(int)builtinVersion.Size];
                builtinVersion.SourceStream.Read(buffer, 0, buffer.Length);
                var tempStream = new System.IO.MemoryStream(buffer);
                gameData.LoadFromMemoryZip(tempStream);
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

            var additionalVersion = GameData.GetVersionInfo(dataPath, out var language);

            if (additionalVersion == null && configuration.GameVersionIndex == 2)
                configuration.GameVersionIndex = 0;

            if (configuration.GameVersionIndex < 2)
            {
                gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex]);
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
                    gameData = LoadBuiltinVersionData(versions[configuration.GameVersionIndex]);
                }
            }

            var builtinVersionDataProviders = new Func<IGameData>[2]
            {
                () => configuration.GameVersionIndex == 0 ? gameData : LoadBuiltinVersionData(versions[0]),
                () => configuration.GameVersionIndex == 1 ? gameData : LoadBuiltinVersionData(versions[1])
            };
            var executableData = new ExecutableData(AmigaExecutable.Read(gameData.Files["AM2_CPU"].Files[1]));
            var graphicProvider = new GraphicProvider(gameData, executableData, null);
            var textureAtlasManager = TextureAtlasManager.CreateEmpty();
            var fontProvider = new FontProvider(executableData);
            foreach (var objectTextFile in gameData.Files["Object_texts.amb"].Files)
                executableData.ItemManager.AddTexts((uint)objectTextFile.Key, TextReader.ReadTexts(objectTextFile.Value));
            renderView = CreateRenderView(gameData, executableData, graphicProvider, fontProvider, () =>
            {
                textureAtlasManager.AddUIOnly(graphicProvider, fontProvider);
                return textureAtlasManager;
            });
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
                    DataProvider = builtinVersionDataProviders[i]
                });
            }
            if (additionalVersion != null)
            {
                gameVersions.Add(new GameVersion
                {
                    Version = additionalVersion,
                    Language = language,
                    Info = "From external data",
                    DataProvider = configuration.GameVersionIndex == 2 ? (Func<IGameData>)(() => gameData) : LoadGameDataFromDataPath
                });
            }
            var cursor = new Render.Cursor(renderView, executableData.Cursors.Entries.Select(c => new Position(c.HotspotX, c.HotspotY)).ToList().AsReadOnly(),
                textureAtlasManager);
            versionSelector = new VersionSelector(renderView, textureAtlasManager, gameVersions, cursor, configuration.GameVersionIndex, configuration.SaveOption);
            versionSelector.Closed += (gameVersionIndex, gameData, saveInDataPath) =>
            {
                configuration.SaveOption = saveInDataPath ? SaveOption.DataFolder : SaveOption.ProgramFolder;
                configuration.GameVersionIndex = gameVersionIndex;
                selectHandler?.Invoke(gameData, saveInDataPath ? dataPath : GetSavePath(VersionSavegameFolders[gameVersionIndex]),
                    gameVersions[gameVersionIndex].Language.ToGameLanguage());
                versionLoader.Dispose();
            };
        }

        void InitGlyphs(TextureAtlasManager textureAtlasManager = null)
        {
            var textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Text);
            renderView.RenderTextFactory.GlyphTextureMapping = Enumerable.Range(0, 94).ToDictionary(x => (byte)x, x => textureAtlas.GetOffset((uint)x));
            renderView.RenderTextFactory.DigitGlyphTextureMapping = Enumerable.Range(0, 10).ToDictionary(x => (byte)(ExecutableData.DigitGlyphOffset + x), x => textureAtlas.GetOffset(100 + (uint)x));
        }

        RenderView CreateRenderView(GameData gameData, ExecutableData executableData, GraphicProvider graphicProvider,
            FontProvider fontProvider, Func<TextureAtlasManager> textureAtlasManagerProvider = null)
        {
            var screenResolution = configuration.GetScreenResolution();
            var aspectRatio = (float)screenResolution.Width / screenResolution.Height;

            return new RenderView(this, gameData, graphicProvider, fontProvider,
                new TextProcessor(), textureAtlasManagerProvider, Width, Height, aspectRatio);
        }

        string GetSavePath(string version)
        {
            string suffix = $"Saves{Path.DirectorySeparatorChar}{version.Replace(' ', '_')}";

            try
            {
                var path = Path.Combine(Configuration.ExecutableDirectoryPath, suffix);
                Directory.CreateDirectory(path);
                return path;
            }
            catch
            {
                var path = Path.Combine(Configuration.FallbackConfigDirectory, suffix);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        void Window_Load()
        {
            window.MakeCurrent();

            if (configuration.Fullscreen)
                Fullscreen = true; // This will adjust the window

            // Setup input
            SetupInput(window.CreateInput());

            ShowVersionSelector((gameData, savePath, gameLanguage) =>
            {
                renderView?.Dispose();
                StartGame(gameData as GameData, savePath, gameLanguage);
                WindowMoved();
                versionSelector = null;
            });
        }

        void Window_Render(double delta)
        {
            if (versionSelector != null)
                versionSelector.Render();
            if (mainMenu != null)
                mainMenu.Render();
            else if (Game != null)
                renderView.Render(Game.ViewportOffset);
            window.SwapBuffers();
        }

        void Window_Update(double delta)
        {
            if (versionSelector != null)
                versionSelector.Update(delta);
            else if (mainMenu != null)
            {
                mainMenu.Update(delta);

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
                    Console.WriteLine("***** Ambermoon Cheat Console *****");
                    Console.WriteLine("Type 'help' for more information.");
                }
            }
            else if (Game != null)
            {
                Game.Update(delta);

                if (Console.KeyAvailable)
                    Cheats.ProcessInput(Console.ReadKey(true), Game);
            }
        }

        void Window_Resize(System.Drawing.Size size)
        {
            if (size.Width != Width || size.Height != Height)
            {
                // This seems to happen when changing the screen resolution.
                window.Size = new System.Drawing.Size(Width, Height);
            }
        }

        void Window_Move(System.Drawing.Point position)
        {
            WindowMoved();
        }

        void WindowMoved()
        {
            if (renderView != null)
            {
                var monitorSize = window.Monitor.Bounds.Size;
                renderView.MaxScreenSize = new Size(monitorSize.Width, monitorSize.Height);
            }
        }

        void UpdateWindow(IConfiguration configuration)
        {
            var screenSize = configuration.GetScreenSize();
            this.configuration.Width = Width = screenSize.Width;
            this.configuration.Height = Height = screenSize.Height;
            window.Size = new System.Drawing.Size(screenSize.Width, screenSize.Height);
            var screenResolution = configuration.GetScreenResolution();
            renderView.VirtualAspectRatio = (float)screenResolution.Width / screenResolution.Height;
            renderView.Resize(screenSize.Width, screenSize.Height);
        }

        public void Run(Configuration configuration)
        {
            this.configuration = configuration;
            var screenSize = configuration.GetScreenSize();
            this.configuration.Width = Width = screenSize.Width;
            this.configuration.Height = Height = screenSize.Height;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var videoMode = new VideoMode(new System.Drawing.Size(Width, Height), 60);
            var options = new WindowOptions(true, true, new System.Drawing.Point(100, 100),
                new System.Drawing.Size(Width, Height), 60.0, 60.0, GraphicsAPI.Default,
                $"Ambermoon.net v{version.Major}.{version.Minor}.{version.Build} beta",
                WindowState.Normal, WindowBorder.Fixed, VSyncMode.Off,
                10, false, videoMode, 24);

            try
            {
                window = Silk.NET.Windowing.Window.Create(options);
                window.Load += Window_Load;
                window.Render += Window_Render;
                window.Update += Window_Update;
                window.Resize += Window_Resize;
                window.Move += Window_Move;
                window.Run();
            }
            finally
            {
                try
                {
                    window?.Dispose();
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
