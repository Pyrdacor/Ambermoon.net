using Silk.NET.Windowing.Sdl.Android;

namespace AmbermoonAndroid
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : SilkActivity
    {
        protected override void OnRun()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            appDataDir = ApplicationContext!.FilesDir!.AbsolutePath;

            var configuration = LoadConfig();
            var gameWindow = new GameWindow();

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

        public override void LoadLibraries()
        {
            base.LoadLibraries();
            Console.WriteLine();
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
                message += System.Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            Console.WriteLine(message + System.Environment.NewLine + ex.StackTrace);
        }
    }
}