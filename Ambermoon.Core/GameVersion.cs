using Ambermoon.Data;
using System;

namespace Ambermoon
{
    public class GameVersion
    {
        public string Version;
        public string Language;
        public string Info;
        public Func<IGameData> DataProvider;
    }
}
