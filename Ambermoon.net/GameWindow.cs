using Ambermoon.Data.Legacy;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;
using System;
using System.Linq;

namespace Ambermoon
{
    class GameWindow : IContextProvider
    {
        IRenderView renderView;
        IWindow window;

        public string Identifier { get; }
        public IGLContext GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Game Game { get; private set; }

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
                keyboard.KeyChar += Keyboard_KeyChar;
            }

            var mouse = inputContext.Mice.FirstOrDefault(m => m.IsConnected);

            if (mouse != null)
            {
                mouse.MouseDown += Mouse_MouseDown;
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
            _ => Key.Invalid,
        };

        void Keyboard_KeyChar(IKeyboard keyboard, char keyChar)
        {
            Game.OnKeyChar(keyChar);
        }

        void Keyboard_KeyDown(IKeyboard keyboard, Silk.NET.Input.Common.Key key, int value)
        {
            Game.OnKeyDown(ConvertKey(key), GetModifiers(keyboard));
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

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            Game.OnMouseDown(GetMouseButtons(mouse));
        }

        void Window_Load()
        {
            window.MakeCurrent();

            // Load game data
            var gameData = new GameData();
            gameData.Load(@"C:\Projects\ambermoon.net\FileSpecs"); // TODO

            // Create render view
            renderView = new RenderView(this, gameData, new GraphicProvider(gameData), Width, Height);

            // Setup input
            SetupInput(window.CreateInput());

            // Create game
            Game = new Game(renderView, new MapManager(gameData, new MapReader(), new TilesetReader()));
            Game.StartNew(); // TODO: Remove later
        }

        void Window_Render(double delta)
        {
            renderView.Render();
            window.SwapBuffers();
        }

        void Window_Update(double delta)
        {
            Game.Update(delta);
        }

        public void Run(int width, int height)
        {
            Width = width;
            Height = height;

            var videoMode = new VideoMode(new System.Drawing.Size(1024, 768), 60);
            var options = new WindowOptions(true, true, new System.Drawing.Point(100, 100),
                new System.Drawing.Size(1024, 768), 60.0, 60.0, GraphicsAPI.Default,
                "Ambermoon.net", WindowState.Normal, WindowBorder.Fixed, VSyncMode.Off,
                10, false, videoMode);

            try
            {
                window = Window.Create(options);
                window.Load += Window_Load;
                window.Render += Window_Render;
                window.Update += Window_Update;
                window.Run();
            }
            finally
            {
                window?.Dispose();
            }
        }
    }
}
