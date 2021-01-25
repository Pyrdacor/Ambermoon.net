namespace Ambermoon.Data.Pyrdacor.Serialization
{
    /// <summary>
    /// File types indicate the content of a file(container).
    /// They can be used to use special compression etc.
    /// </summary>
    public enum FileType : byte
    {
        Raw, // general purpose data
        Texts,
        Dictionary,
        Palettes,
        Textures,
        TilesetGraphics,
        UIGraphics,
        Graphics, // portraits, battle sprites, monsters, etc
        TilesetData,
        Labdata,
        CharacterData,
        MapData,
        Savegames,
        OtherData, // chests, merchants, places, etc        
        Music,
        Video
    }
}
