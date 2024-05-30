using Ambermoon;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Silk.NET.Windowing.Sdl.Android;

namespace AmbermoonAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : SilkActivity, GestureDetector.IOnGestureListener
    {
        private GameWindow gameWindow;
        private MusicManager musicManager;
        private GestureDetector gestureDetector;

		public override bool DispatchTouchEvent(MotionEvent ev)
		{
			if (gestureDetector != null)
			{
				gestureDetector.OnTouchEvent(ev);
			}
			return base.DispatchTouchEvent(ev);
		}

		public override bool OnTouchEvent(MotionEvent e)
        {
			if (gestureDetector != null)
			{
				gestureDetector.OnTouchEvent(e);
				return true;
			}
			return base.OnTouchEvent(e);
		}

        private void NameResetHandler()
        {
			RunOnUiThread(() => Title = "Ambermoon");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
            musicManager?.Stop();
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
            string version;
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
			{
#pragma warning disable CA1416 // Validate platform compatibility
				version = PackageManager?.GetPackageInfo(new VersionedPackage(PackageName, 0), PackageManager.PackageInfoFlags.Of(0)).VersionName ?? "1.0";
#pragma warning restore CA1416 // Validate platform compatibility
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				version = PackageManager?.GetPackageInfo(PackageName, 0).VersionName ?? "1.0";
#pragma warning restore CS0618 // Type or member is obsolete
			}

			gameWindow = new($"Ambermoon.net V{version}");

			ActionBar?.Hide();
			Title = "Ambermoon";

			base.OnCreate(savedInstanceState);

			gestureDetector = new GestureDetector(this, this);
		}

		protected override void OnRun()
        {
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;           

            FileProvider.Initialize(this);

            //appDataDir = ApplicationContext!.FilesDir!.AbsolutePath;

            var configuration = LoadConfig();
            configuration.SaveRequested += () => SaveConfig(configuration);

            try
            {
                musicManager = new MusicManager(this);
                gameWindow.Run(configuration, musicManager, NameResetHandler);
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

        Configuration LoadConfig()
        {
            return Configuration.Load(new Configuration { FirstStart = true });
        }

        void SaveConfig(Configuration configuration)
        {
            try
            {
                configuration.Save();
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
                message += System.Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            Console.WriteLine(message + System.Environment.NewLine + ex.StackTrace);
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