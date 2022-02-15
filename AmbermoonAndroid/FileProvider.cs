using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmbermoonAndroid
{
    internal static class FileProvider
    {
        static Activity activity = null;

        public static void Initialize(Activity activity)
        {
            FileProvider.activity = activity;
        }

        static Stream LoadStream(int id)
        {
            var stream = new MemoryStream();
            using var dataStream = activity.ApplicationContext.Resources.OpenRawResource(id);
            dataStream.CopyTo(stream);
            stream.Position = 0;
            return stream;
        }

        static byte[] LoadData(int id)
        {
            using var stream = LoadStream(id);
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return data;
        }

        public static Stream GetVersions() => LoadStream(Resource.Raw.versions);

        public static byte[] GetSongData() => LoadData(Resource.Raw.song);

        public static Stream GetLogoData() => LoadStream(Resource.Raw.logo);

        public static byte[] GetIntroFontData() => LoadData(Resource.Raw.IntroFont);
    }
}
