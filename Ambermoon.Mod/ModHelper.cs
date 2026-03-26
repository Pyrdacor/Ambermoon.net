using System.Reflection;

namespace Ambermoon.Mod;

public static class ModHelper
{
    public static ModVersion VersionFromAssembly(Assembly assembly)
    {
        var asmVersion = assembly.GetName().Version;

        return new(asmVersion!.Major, asmVersion.Minor);
    }
}
