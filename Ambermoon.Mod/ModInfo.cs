namespace Ambermoon.Mod;

public record ModVersion(int Major, int Minor)
{
    public override string ToString()
    {
        return $"{Major}.{Minor:00}";
    }
}

public record ModInfo(string Name, string Description, ModVersion Version, DateOnly ReleaseDate, params string[] Authors);
