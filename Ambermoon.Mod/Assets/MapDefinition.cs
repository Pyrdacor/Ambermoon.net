using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// One cell of a map grid legend.
/// For 3D maps Wall and Object are used, for 2D maps Back and Front.
/// Event is the 1-based index into <see cref="MapDefinition.Events"/>.
/// </summary>
public class MapCellDefinition
{
    public uint Wall { get; set; }
    public uint Object { get; set; }
    public uint Back { get; set; }
    public uint Front { get; set; }
    public uint Event { get; set; }
    /// <summary>
    /// 3D only: The block is not drawn at all and blocks movement.
    /// </summary>
    public bool Border { get; set; }
    /// <summary>
    /// 3D only: Automap symbol of the event on this cell (e.g. Chest, Door, Teleporter).
    /// </summary>
    public AutomapType Automap { get; set; } = AutomapType.None;
}

public class MapCharacterDefinition
{
    public CharacterType Type { get; set; } = CharacterType.Monster;
    /// <summary>
    /// Index of the party member, NPC, monster group or map text.
    /// </summary>
    public uint Index { get; set; }
    /// <summary>
    /// Labdata object index for 3D maps, tile or NPC graphic index for 2D maps.
    /// </summary>
    public uint Graphic { get; set; }
    public Map.CharacterReference.Flags Flags { get; set; }
    public uint Event { get; set; }
    public int CollisionClass { get; set; }
    public uint CombatBackground { get; set; }
    /// <summary>
    /// 1-based positions. Monsters and randomly moving characters only use the first one.
    /// </summary>
    public List<Position> Positions { get; set; } = [];
}

/// <summary>
/// JSON representation of a complete map, used for new maps in asset mods.
///
/// The map layout is given by a grid of strings (one string per row) and a
/// legend which maps each character to a cell definition. Events are given as a
/// list where "Next" references another event by its 1-based list index.
/// </summary>
public class MapDefinition
{
    public MapType Type { get; set; } = MapType.Map3D;
    public int Width { get; set; }
    public int Height { get; set; }
    public MapFlags Flags { get; set; } = MapFlags.Indoor | MapFlags.Automapper | MapFlags.CanRest | MapFlags.CanUseMagic;
    /// <summary>
    /// Labdata index for 3D maps, tileset index for 2D maps.
    /// </summary>
    public uint Labdata { get; set; }
    public uint Tileset { get; set; }
    public uint Music { get; set; } = 1;
    public uint Palette { get; set; } = 1;
    public World World { get; set; } = World.Lyramion;
    public uint LabyrinthBackground { get; set; }
    public uint NPCGraphics { get; set; }
    public List<string> Texts { get; set; } = [];
    public Dictionary<string, MapCellDefinition> Legend { get; set; } = [];
    public List<string> Grid { get; set; } = [];
    /// <summary>
    /// Each event is a JSON object with a "Type" (see EventType) and the
    /// properties of the matching event class. "Next" is the 1-based index
    /// of the next event in this list (0 = none). Branch targets of conditions,
    /// doors, chests etc. are 0-based indices into this list (65535 = none).
    /// </summary>
    public List<JObject> Events { get; set; } = [];
    public List<MapCharacterDefinition> Characters { get; set; } = [];
    public List<Map.GotoPoint> GotoPoints { get; set; } = [];

    public static MapDefinition Parse(string json) => JsonConvert.DeserializeObject<MapDefinition>(json, ModJson.Settings)
        ?? throw new InvalidDataException("Empty map definition.");

    public Map CreateMap(uint index)
    {
        if (Width <= 0 || Height <= 0)
            throw new InvalidDataException($"Map {index} needs a positive width and height.");
        if (Grid.Count != Height || Grid.Any(row => row.Length != Width))
            throw new InvalidDataException($"The grid of map {index} must have {Height} rows with {Width} characters each.");
        if (Characters.Count > 32)
            throw new InvalidDataException($"Map {index} has more than 32 characters.");

        bool is3D = Type == MapType.Map3D;
        var map = new Map
        {
            Index = index,
            Type = Type,
            Width = Width,
            Height = Height,
            Flags = Flags,
            TilesetOrLabdataIndex = is3D ? Labdata : Tileset,
            MusicIndex = Music,
            PaletteIndex = Palette,
            World = World,
            LabyrinthBackgroundIndex = LabyrinthBackground,
            NPCGfxIndex = NPCGraphics,
            Texts = [.. Texts],
            GotoPoints = [.. GotoPoints]
        };

        // Events
        var events = Events.Select((e, i) => CreateEvent(index, e, (uint)i)).ToList();

        for (int i = 0; i < events.Count; i++)
        {
            uint next = Events[i].Value<uint?>("Next") ?? 0;

            if (next > events.Count)
                throw new InvalidDataException($"Event {i + 1} of map {index} references unknown next event {next}.");

            events[i].Next = next == 0 ? null : events[(int)next - 1];
        }

        map.Events.AddRange(events);

        // Every event referenced by the grid or a character is an entry point (event list).
        var entryEvents = Legend.Values.Select(c => c.Event)
            .Concat(Characters.Select(c => c.Event))
            .Where(e => e != 0)
            .Distinct()
            .OrderBy(e => e)
            .ToList();

        if (entryEvents.Count > 64)
            throw new InvalidDataException($"Map {index} has more than 64 event entry points.");

        foreach (var entry in entryEvents)
        {
            if (entry > events.Count)
                throw new InvalidDataException($"Map {index} references unknown event {entry}.");

            map.EventList.Add(events[(int)entry - 1]);
            map.EventAutomapTypes.Add(Legend.Values.FirstOrDefault(c => c.Event == entry)?.Automap ?? AutomapType.None);
        }

        uint MapEventId(uint eventIndex) => eventIndex == 0 ? 0 : (uint)entryEvents.IndexOf(eventIndex) + 1;

        // Grid
        if (is3D)
        {
            map.Blocks = new Map.Block[Width, Height];
            map.InitialBlocks = new Map.Block[Width, Height];
        }
        else
        {
            map.Tiles = new Map.Tile[Width, Height];
            map.InitialTiles = new Map.Tile[Width, Height];
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                string key = Grid[y][x].ToString();

                if (!Legend.TryGetValue(key, out var cell))
                    throw new InvalidDataException($"Map {index} uses grid character '{key}' which is not part of the legend.");

                if (is3D)
                {
                    map.InitialBlocks![x, y] = new Map.Block
                    {
                        WallIndex = cell.Wall,
                        ObjectIndex = cell.Object,
                        MapEventId = MapEventId(cell.Event),
                        MapBorder = cell.Border
                    };
                    map.Blocks![x, y] = map.InitialBlocks[x, y].Clone();
                }
                else
                {
                    map.InitialTiles![x, y] = new Map.Tile
                    {
                        BackTileIndex = cell.Back,
                        FrontTileIndex = cell.Front,
                        MapEventId = MapEventId(cell.Event)
                    };
                    map.Tiles![x, y] = map.InitialTiles[x, y].Clone();
                }
            }
        }

        // Characters
        for (int i = 0; i < Characters.Count; i++)
        {
            var character = Characters[i];

            if (character.Positions.Count == 0)
                throw new InvalidDataException($"Character {i + 1} of map {index} needs at least one position.");

            var reference = new Map.CharacterReference
            {
                Type = character.Type,
                Index = character.Index,
                GraphicIndex = character.Graphic,
                CharacterFlags = character.Flags,
                EventIndex = MapEventId(character.Event),
                CollisionClass = character.CollisionClass,
                CombatBackgroundIndex = character.CombatBackground
            };
            reference.Positions.AddRange(character.Positions.Select(p => new Position(p)));
            map.CharacterReferences[i] = reference;
        }

        return map;
    }

    static Event CreateEvent(uint mapIndex, JObject json, uint index)
    {
        var typeToken = json["Type"] ?? throw new InvalidDataException($"Event {index + 1} of map {mapIndex} has no 'Type'.");
        var type = typeToken.ToObject<EventType>(JsonSerializer.Create(ModJson.Settings));
        var @event = EventFactory.Create(type);
        var data = (JObject)json.DeepClone();
        data.Remove("Type");
        data.Remove("Next");
        JsonConvert.PopulateObject(data.ToString(), @event, ModJson.Settings);
        @event.Index = index + 1;
        @event.Type = type;
        EventFactory.InitializeRawData(@event);

        return @event;
    }
}
