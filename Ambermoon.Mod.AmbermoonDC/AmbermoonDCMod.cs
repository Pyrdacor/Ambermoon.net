using System.Reflection;
using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

public class AmbermoonDCMod : IMod
{
    public ModInfo Info { get; } = new
    (
        "Ambermoon DC",
        "Dungeon Crawler Mod",
        ModHelper.VersionFromAssembly(Assembly.GetExecutingAssembly()),
        "Pyrdacor"
    );
    public ISavegameManager? CustomSavegameManager { get; } = null;
}
