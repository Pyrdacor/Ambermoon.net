using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Ambermoon
{
    internal class Configuration
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 800;
        public bool Fullscreen { get; set; } = false;
        public string DataPath { get; set; } = ExecutablePath;

        public static string ExecutablePath
        {
            get
            {
                const bool isWindows = true; // TODO

                var assemblyPath = Process.GetCurrentProcess().MainModule.FileName;

                if (assemblyPath.EndsWith("dotnet"))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().Location;
                }

                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

                if (isWindows)
                {
                    if (assemblyDirectory.EndsWith(@"Debug") || assemblyDirectory.EndsWith(@"Release"))
                    {
                        string projectFile = Path.GetFileNameWithoutExtension(assemblyPath) + ".csproj";

                        var root = new DirectoryInfo(assemblyDirectory);

                        while (root.Parent != null)
                        {
                            if (File.Exists(Path.Combine(root.FullName, projectFile)))
                                break;

                            root = root.Parent;

                            if (root.Parent == null) // we could not find it (should not happen)
                                return assemblyDirectory;
                        }

                        return root.FullName;
                    }
                    else
                    {
                        return assemblyDirectory;
                    }
                }
                else
                {
                    return assemblyDirectory;
                }
            }
        }

        public static Configuration Load(string filename)
        {
            if (!File.Exists(filename))
                return new Configuration();

            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }

        public void Save(string filename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            File.WriteAllText(filename, JsonConvert.SerializeObject(this));
        }
    }
}
