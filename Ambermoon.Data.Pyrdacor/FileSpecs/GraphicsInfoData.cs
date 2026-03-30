using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class GraphicsInfoData : IFileSpec<GraphicsInfoData>, IFileSpec
{
    public static string Magic => "GIN";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    readonly Dictionary<int, int> npcGraphicOffsets = new(2);
    readonly Dictionary<int, List<int>> npcGraphicFrameCounts = new(2);
    readonly Dictionary<CombatGraphicIndex, CombatGraphicInfo> combatGraphicInfos = new(42);
    readonly Dictionary<TravelType, GraphicInfo> stationaryImageInfos = new(5);
    readonly Dictionary<TravelType, TravelGraphicInfo[]> travelGraphicInfos = new(20);
    Character2DAnimationInfo playerAnimationInfo = new();
    readonly List<Position> cursorHotspots = [];


    public IReadOnlyDictionary<int, int> NPCGraphicOffsets => npcGraphicOffsets.AsReadOnly();
    public IReadOnlyDictionary<int, List<int>> NPCGraphicFrameCounts => npcGraphicFrameCounts.AsReadOnly();
    public IReadOnlyDictionary<CombatGraphicIndex, CombatGraphicInfo> CombatGraphicInfos => combatGraphicInfos.AsReadOnly();
    public IReadOnlyDictionary<TravelType, GraphicInfo> StationaryImageInfos => stationaryImageInfos.AsReadOnly();
    public IReadOnlyDictionary<TravelType, TravelGraphicInfo[]> TravelGraphicInfos => travelGraphicInfos.AsReadOnly();
    public Character2DAnimationInfo PlayerAnimationInfo => playerAnimationInfo;
    public IReadOnlyList<Position> CursorHotspots => cursorHotspots;

    public GraphicsInfoData()
    {

    }

    public GraphicsInfoData
    (
        IReadOnlyDictionary<int, int> npcGraphicOffsets,
        IReadOnlyDictionary<int, List<int>> npcGraphicFrameCounts,
        IReadOnlyDictionary<CombatGraphicIndex, CombatGraphicInfo> combatGraphicInfos,
        IReadOnlyDictionary<TravelType, GraphicInfo> stationaryImageInfos,
        IReadOnlyDictionary<TravelType, TravelGraphicInfo[]> travelGraphicInfos,
        Character2DAnimationInfo playerAnimationInfo,
        IReadOnlyList<Position> cursorHotspots
    )
    {
        this.npcGraphicOffsets = new(npcGraphicOffsets);
        this.npcGraphicFrameCounts = new(npcGraphicFrameCounts);
        this.combatGraphicInfos = new(combatGraphicInfos);
        this.stationaryImageInfos = new(stationaryImageInfos);
        this.travelGraphicInfos = new(travelGraphicInfos);
        this.playerAnimationInfo = playerAnimationInfo;
        this.cursorHotspots = new(cursorHotspots);
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int npcGraphicFileCount = dataReader.ReadByte(); // usually 2 (number of NPC graphic sets)
        int totalFrameCount = 0;
        npcGraphicFrameCounts.Clear();
        npcGraphicOffsets.Clear();
        combatGraphicInfos.Clear();
        stationaryImageInfos.Clear();
        travelGraphicInfos.Clear();
        cursorHotspots.Clear();

        for (int i = 0; i < npcGraphicFileCount; i++)
        {
            int index = dataReader.ReadByte();
            int graphicCount = dataReader.ReadWord();

            npcGraphicOffsets.Add(index, totalFrameCount);

            var frameCounts = new List<int>(graphicCount);

            npcGraphicFrameCounts.Add(index, frameCounts);

            for (int g = 0; g < graphicCount; g++)
            {
                int frameCount = dataReader.ReadByte();
                frameCounts.Add(frameCount);
                totalFrameCount += frameCount;
            }
        }

        int numCombatGraphicInfos = dataReader.ReadWord();

        for (int i = 0; i < numCombatGraphicInfos; i++)
        {
            uint frames = dataReader.ReadByte();
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();
            uint palette = dataReader.ReadByte();

            combatGraphicInfos.Add((CombatGraphicIndex)i, new(frames, width, height, palette, i == (int)CombatGraphicIndex.UISwordAndMace));
        }

        int numStationaryImageInfos = dataReader.ReadByte();

        for (int i = 0; i < numStationaryImageInfos; i++)
        {
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();

            stationaryImageInfos.Add((TravelType)i, new()
            {
                Width = width,
                Height = height,
                GraphicFormat = GraphicFormat.Palette5Bit, // doesn't really matter (only used by legacy graphic provider which won't be used)
                Alpha = true,
            });
        }

        int numTravelGraphicInfos = dataReader.ReadByte();

        for (int i = 0; i < numTravelGraphicInfos; i++)
        {
            var infos = new TravelGraphicInfo[4];

            for (int d = 0; d < 4; d++) // 4 directions
            {
                uint offsetX = dataReader.ReadWord();
                uint offsetY = dataReader.ReadWord();
                uint width = dataReader.ReadWord();
                uint height = dataReader.ReadWord();

                infos[d] = new()
                {
                    OffsetX = offsetX,
                    OffsetY = offsetY,
                    Width = width,
                    Height = height,
                };
            }

            travelGraphicInfos.Add((TravelType)i, infos);
        }

        playerAnimationInfo.FrameWidth = dataReader.ReadWord();
        playerAnimationInfo.FrameHeight = dataReader.ReadWord();
        playerAnimationInfo.StandFrameIndex = dataReader.ReadByte();
        playerAnimationInfo.SitFrameIndex = dataReader.ReadByte();
        playerAnimationInfo.SleepFrameIndex = dataReader.ReadByte();
        playerAnimationInfo.NumStandFrames = dataReader.ReadByte();
        playerAnimationInfo.NumSitFrames = dataReader.ReadByte();
        playerAnimationInfo.NumSleepFrames = dataReader.ReadByte();
        playerAnimationInfo.TicksPerFrame = dataReader.ReadWord();
        playerAnimationInfo.NoDirections = dataReader.ReadBool();
        playerAnimationInfo.IgnoreTileType = dataReader.ReadBool();
        playerAnimationInfo.UseTopSprite = dataReader.ReadBool();

        int numCursorHotspots = dataReader.ReadByte();

        for (int i = 0; i < numCursorHotspots; i++)
        {
            int x = dataReader.ReadByte();
            int y = dataReader.ReadByte();

            cursorHotspots.Add(new(x, y));
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((byte)npcGraphicOffsets.Count);
        
        foreach (var npcGraphicOffset in npcGraphicOffsets.OrderBy(i => i.Key))
        {
            dataWriter.Write((byte)npcGraphicOffset.Key);
            var frameCounts = npcGraphicFrameCounts[npcGraphicOffset.Key];
            dataWriter.Write((ushort)frameCounts.Count);

            foreach (var frameCount in frameCounts)
                dataWriter.Write((byte)frameCount);
        }

        dataWriter.Write((ushort)combatGraphicInfos.Count);

        foreach (var combatGraphicInfo in combatGraphicInfos.OrderBy(i => i.Key))
        {
            var info = combatGraphicInfo.Value;

            dataWriter.Write((byte)info.FrameCount);
            dataWriter.Write((ushort)info.GraphicInfo.Width);
            dataWriter.Write((ushort)info.GraphicInfo.Height);
            dataWriter.Write((byte)info.Palette);
        }

        dataWriter.Write((byte)stationaryImageInfos.Count);

        foreach (var stationaryImageInfo in stationaryImageInfos.OrderBy(i => i.Key))
        {
            var info = stationaryImageInfo.Value;

            dataWriter.Write((ushort)info.Width);
            dataWriter.Write((ushort)info.Height);
        }

        dataWriter.Write((byte)travelGraphicInfos.Count);

        foreach (var travelGraphicInfo in travelGraphicInfos.OrderBy(i => i.Key))
        {
            var infos = travelGraphicInfo.Value;

            for (int d = 0; d < 4; d++) // 4 directions
            {
                var info = infos[d];

                dataWriter.Write((ushort)info.OffsetX);
                dataWriter.Write((ushort)info.OffsetY);
                dataWriter.Write((ushort)info.Width);
                dataWriter.Write((ushort)info.Height);
            }
        }

        dataWriter.Write((ushort)playerAnimationInfo.FrameWidth);
        dataWriter.Write((ushort)playerAnimationInfo.FrameHeight);
        dataWriter.Write((byte)playerAnimationInfo.StandFrameIndex);
        dataWriter.Write((byte)playerAnimationInfo.SitFrameIndex);
        dataWriter.Write((byte)playerAnimationInfo.SleepFrameIndex);
        dataWriter.Write((byte)playerAnimationInfo.NumStandFrames);
        dataWriter.Write((byte)playerAnimationInfo.NumSitFrames);
        dataWriter.Write((byte)playerAnimationInfo.NumSleepFrames);
        dataWriter.Write((ushort)playerAnimationInfo.TicksPerFrame);
        dataWriter.Write(playerAnimationInfo.NoDirections);
        dataWriter.Write(playerAnimationInfo.IgnoreTileType);
        dataWriter.Write(playerAnimationInfo.UseTopSprite);

        dataWriter.Write((byte)cursorHotspots.Count);

        foreach (var cursorHotspot in cursorHotspots)
        {
            dataWriter.Write((byte)cursorHotspot.X);
            dataWriter.Write((byte)cursorHotspot.Y);
        }
    }
}
