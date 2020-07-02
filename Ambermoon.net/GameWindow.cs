using Ambermoon.Data.Legacy;
using Ambermoon.Render;
using Ambermoon.Renderer.OpenGL;
using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

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

        void Window_Load()
        {
            var gameData = new GameData();
            gameData.Load(@"C:\Projects\ambermoon.net\FileSpecs"); // TODO
            renderView = new RenderView(this, gameData, new GraphicProvider(gameData), Width, Height);
            Game = new Game(renderView, new MapManager(gameData, new MapReader()));
        }

        void Window_Render(double delta)
        {
            window.MakeCurrent();
            renderView.Render();
            window.SwapBuffers();
        }

        void Window_Update(double delta)
        {
            window.MakeCurrent();
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
