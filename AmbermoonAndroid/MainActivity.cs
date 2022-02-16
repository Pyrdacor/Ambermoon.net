using Ambermoon;
using Android.Views;
using Silk.NET.Windowing.Sdl.Android;

namespace AmbermoonAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : SilkActivity, GestureDetector.IOnGestureListener
    {
        private readonly GameWindow gameWindow = new();
        private GestureDetector gestureDetector;

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (gestureDetector != null)
                return gestureDetector.OnTouchEvent(e);

            return base.OnTouchEvent(e);
        }

        protected override void OnRun()
        {
            gestureDetector = new GestureDetector(this, this);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            FileProvider.Initialize(this);

            appDataDir = ApplicationContext!.FilesDir!.AbsolutePath;

            var configuration = LoadConfig();

            try
            {
                gameWindow.Run(configuration);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                SaveConfig(configuration);
            }
        }

        const string ConfigurationFileName = "ambermoon.cfg";
        string appDataDir = "";

        Configuration LoadConfig()
        {
            var path = Path.Combine(appDataDir, ConfigurationFileName);
            return Configuration.Load(path, new Configuration { FirstStart = true });
        }

        void SaveConfig(Configuration configuration)
        {
            var path = Path.Combine(appDataDir, ConfigurationFileName);

            try
            {
                configuration.Save(path);
            }
            catch
            {
                Console.WriteLine("Unable to save configuration.");
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                PrintException(ex);
            else
                Console.WriteLine(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");
        }

        static void PrintException(Exception ex)
        {
            string message = ex.Message;

            if (ex.InnerException != null)
            {
                message += Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            Console.WriteLine(message + Environment.NewLine + ex.StackTrace);
        }

        public bool OnDown(MotionEvent e)
        {
            // TODO
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            // TODO
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            if (e.PointerCount == 1)
            {
                var coords = new MotionEvent.PointerCoords();
                e.GetPointerCoords(0, coords);
                var position = new Position(Util.Round(coords.X), Util.Round(coords.Y));
                gameWindow.OnMouseDown(position, MouseButtons.Right);
                gameWindow.OnMouseUp(position, MouseButtons.Right);
            }
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            var coords = new MotionEvent.PointerCoords();
            e2.GetPointerCoords(0, coords);
            var position = new Position(Util.Round(coords.X), Util.Round(coords.Y));
            gameWindow.OnMouseScroll(position, Util.Round(distanceY), Util.Round(distanceX));
            return true;
        }

        public void OnShowPress(MotionEvent e)
        {
            // TODO
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            if (e.PointerCount == 1)
            {
                var coords = new MotionEvent.PointerCoords();
                e.GetPointerCoords(0, coords);
                var position = new Position(Util.Round(coords.X), Util.Round(coords.Y));
                gameWindow.OnMouseDown(position, MouseButtons.Left);
                gameWindow.OnMouseUp(position, MouseButtons.Left);
                return true;
            }

            return false;
        }
    }
}