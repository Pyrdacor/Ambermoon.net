using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    // Note: 3D graphics are loaded through labdata
    public enum GraphicType
    {
        Tileset1,
        Tileset2,
        Tileset3,
        Tileset4,
        Tileset5,
        Tileset6,
        Tileset7,
        Tileset8,
        Tileset9,
        Tileset10,
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
        RiddlemouthGraphics
    }

    public enum MonsterRow
    {
        Farthest,
        Far,
        Middle,
        Near
    }

    public interface IGraphicProvider
    {
        Dictionary<int, Graphic> Palettes { get; }
        Dictionary<int, int> NPCGraphicOffsets { get; }
		Dictionary<int, List<int>> NPCGraphicFrameCounts { get; }
		List<Graphic> GetGraphics(GraphicType type);
        CombatBackgroundInfo Get2DCombatBackground(uint index, bool advanced);
        CombatBackgroundInfo Get3DCombatBackground(uint index, bool advanced);
        CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index);
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
}
