using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data;

// Note: 3D graphics are loaded through labdata
public enum GraphicType
{
    Player,
    Portrait,
    Item,
    Layout,
    LabBackground,
    Cursor,
    Pics80x80,
    UIElements,
    EventPictures,
    TravelGfx,
    Transports,
    NPC,
    CombatBackground,
    CombatGraphics,
    BattleFieldIcons,
    AutomapGraphics,
    RiddlemouthGraphics,
    Tileset1, // NOTE: All other tilesets follow after this so keep this last!
}

public enum MonsterRow
{
    Farthest,
    Far,
    Middle,
    Near
}

public interface IPaletteProvider
{
    Dictionary<int, Graphic> Palettes { get; }
}

public interface IGraphicInfoProvider : IPaletteProvider
{
    IReadOnlyDictionary<int, int> NPCGraphicOffsets { get; }
    IReadOnlyDictionary<int, List<int>> NPCGraphicFrameCounts { get; }
    CombatBackgroundInfo Get2DCombatBackground(uint index, bool advanced);
    CombatBackgroundInfo Get3DCombatBackground(uint index, bool advanced);
    CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index);
    IReadOnlyList<Graphic> GetLabBackgroundGraphics();
    float GetMonsterRowImageScaleFactor(MonsterRow row);
    byte PaletteIndexFromColorIndex(Map map, byte colorIndex);
    byte DefaultTextPaletteIndex { get; }
    byte PrimaryUIPaletteIndex { get; }
    byte SecondaryUIPaletteIndex { get; }
    byte AutomapPaletteIndex { get; }
    byte FirstIntroPaletteIndex { get; }
    byte FirstOutroPaletteIndex { get; }
    byte FirstFantasyIntroPaletteIndex { get; }
}

public interface IGraphicProvider : IGraphicInfoProvider
{
    List<Graphic> GetGraphics(GraphicType type);
}

public interface IGraphicAtlas
{
    Graphic Graphic { get; }
    Dictionary<uint, Position> Offsets { get; }
}

public interface IGraphicAtlasProvider : IGraphicInfoProvider
{
    IGraphicAtlas GetGraphicAtlas(GraphicType type);
}
