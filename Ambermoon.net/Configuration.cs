using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace Ambermoon
{
    internal class Configuration
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 800;
        public bool Fullscreen { get; set; } = false;
        public string DataPath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
