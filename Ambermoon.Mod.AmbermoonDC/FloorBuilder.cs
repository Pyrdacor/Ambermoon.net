using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Mod.Assets;
using static Ambermoon.Data.Tileset;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates the map of a floor from its layout and prepares everything else
/// which is needed for it (monster groups, chest contents, the recruit).
/// </summary>
internal class FloorBuilder(ModApi api, IReadOnlyList<DungeonTheme> themes)
{
    /// <summary>
    /// Two map indices are used alternately so that every descent is a real map change.
    /// The original game uses map indices 1 to 528.
    /// </summary>
    public const uint FirstMapIndex = 600;
    /// <summary>
    /// First monster group index for the monsters of the current floor
    /// (the original game uses up to 120 or so).
    /// </summary>
    public const uint FirstMonsterGroupIndex = 200;
    /// <summary>
    /// Chests 1 to 30 are used for the dungeon. They belong to original maps
    /// which are never visited in this mod.
    /// </summary>
    const uint FirstChestIndex = 1;
    /// <summary>
    /// Map whose character bits are used to remember recruits (never visited).
    /// </summary>
    public const uint RecruitBitsMap = 1000;
    /// <summary>
    /// Party member slots 2 to 15 are used for random recruits (slot 1 is the hero).
    /// </summary>
    const uint LastRecruitSlot = 15;

    readonly ModApi api = api;
    readonly IReadOnlyList<DungeonTheme> themes = themes;

    public static uint MapIndexForFloor(int floor) => FirstMapIndex + (uint)(floor % 2);

    public static bool IsDungeonMap(uint mapIndex) => mapIndex == FirstMapIndex || mapIndex == FirstMapIndex + 1;

    DCTexts Texts => DCTexts.Get(api.Language);

    public DungeonTheme ThemeForFloor(int seed, int floor) => themes[DungeonTheme.ThemeIndexForFloor(seed, floor, themes.Count)];

    /// <summary>
    /// Creates the map of the floor and registers it together with its monster groups.
    /// This is done for new floors and also when a savegame is loaded.
    /// </summary>
    public Map Build(FloorLayout layout, DCState state)
    {
        int floor = layout.Floor;
        uint mapIndex = MapIndexForFloor(floor);
        var random = FloorGenerator.CreateRandom(state.Seed, floor, 2);
        var theme = ThemeForFloor(state.Seed, floor);
        var texts = Texts;
        var map = new Map
        {
            Index = mapIndex,
            Type = MapType.Map3D,
            Width = layout.Width,
            Height = layout.Height,
            TilesetOrLabdataIndex = theme.LabdataIndex,
            Flags = (floor >= 4 ? MapFlags.Dungeon : MapFlags.Indoor) | MapFlags.Automapper | MapFlags.CanRest |
                MapFlags.NoSleepUntilDawn | MapFlags.CanUseMagic,
            MusicIndex = layout.IsBossFloor ? theme.Definition.BossMusic : theme.MusicForFloor(floor),
            PaletteIndex = theme.Definition.Palette,
            World = World.Lyramion, // Note: Party members can only be dismissed on Lyramion.
            Blocks = new Map.Block[layout.Width, layout.Height],
            InitialBlocks = new Map.Block[layout.Width, layout.Height],
            Texts =
            [
                string.Format(texts.FloorName, floor, theme.Name(api.Language).ToUpper()),
                string.Format(texts.DescendQuestion, floor + 1),
                texts.WayBackBlocked,
                texts.SecretFound
            ]
        };
        const uint DescendText = 1;
        const uint WayBackText = 2;
        const uint SecretText = 3;
        var events = new MapEventBuilder(map);

        // Walls and floor
        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                var pos = new Pos(x, y);
                var block = new Map.Block();

                switch (layout[pos])
                {
                    case CellType.Wall:
                    case CellType.SecretWall:
                        block.WallIndex = theme.RandomWall(random);
                        break;
                    case CellType.IllusoryWall:
                        block.WallIndex = theme.RandomIllusoryWall(random);
                        break;
                    case CellType.Door:
                        block.WallIndex = theme.DoorWall;
                        break;
                    default:
                        if (layout.Decorations.TryGetValue(pos, out var decoration) && theme.DecorationObjects.Count != 0)
                            block.ObjectIndex = theme.DecorationObjects[decoration % theme.DecorationObjects.Count];
                        else if (layout.Obstacles.TryGetValue(pos, out var obstacle) && theme.ObstacleObjects.Count != 0)
                            block.ObjectIndex = theme.ObstacleObjects[obstacle % theme.ObstacleObjects.Count];
                        break;
                }

                map.Blocks[x, y] = block;
            }
        }

        static uint X(Pos pos) => (uint)pos.X + 1;
        static uint Y(Pos pos) => (uint)pos.Y + 1;

        // Start: a ladder which leads nowhere
        var start = map.Blocks[layout.Start.X, layout.Start.Y];
        start.ObjectIndex = theme.EntranceObject;
        start.MapEventId = events.AddEntry(AutomapType.Exit,
            EventFactory.MapText(WayBackText, Data.EventTrigger.EyeCursor));

        // Stairs: a hole which leads to the next floor
        var stairs = map.Blocks[layout.Stairs.X, layout.Stairs.Y];
        stairs.ObjectIndex = theme.ExitObject;
        stairs.MapEventId = events.AddEntry(AutomapType.Exit,
            EventFactory.Create<DecisionEvent>(e => e.TextIndex = DescendText),
            EventFactory.Teleport(MapIndexForFloor(floor + 1), 1, 1, CharacterDirection.Up, TeleportEvent.TransitionType.Falling));

        // Doors
        for (int i = 0; i < layout.Doors.Count; i++)
        {
            var door = layout.Doors[i];
            map.Blocks[door.Position.X, door.Position.Y].MapEventId = events.AddEntry(AutomapType.Door,
                EventFactory.Door((byte)i, door.Locked ? (uint)Math.Min(60, 10 + floor) : 0),
                EventFactory.ChangeBlock(X(door.Position), Y(door.Position), theme.OpenDoorWall));
        }

        // Secret walls which disappear when touched
        foreach (var secretWall in layout.SecretWalls)
        {
            uint entryIndex = (uint)map.EventList.Count; // index of the next entry
            map.Blocks[secretWall.X, secretWall.Y].MapEventId = events.AddEntry(AutomapType.None,
                EventFactory.Create<ConditionEvent>(e =>
                {
                    e.TypeOfCondition = ConditionEvent.ConditionType.Hand;
                    e.Value = 1;
                }),
                EventFactory.ChangeBlock(X(secretWall), Y(secretWall)),
                // Deactivate this event afterwards
                EventFactory.Create<ActionEvent>(e =>
                {
                    e.TypeOfAction = ActionEvent.ActionType.SetEventBit;
                    e.ObjectIndex = ((mapIndex - 1) << 6) | entryIndex;
                    e.Value = 1;
                }),
                EventFactory.MapText(SecretText, Data.EventTrigger.Always));
        }

        // Chests
        for (int i = 0; i < layout.Chests.Count; i++)
        {
            var chest = layout.Chests[i];
            var block = map.Blocks[chest.Position.X, chest.Position.Y];
            block.ObjectIndex = theme.ChestObject;
            block.MapEventId = events.AddEntry(AutomapType.Chest,
                EventFactory.Chest(FirstChestIndex + (uint)i, chest.Locked ? (uint)Math.Min(60, 10 + floor) : 0));
        }

        // Teleporters
        if (theme.TeleporterObject != 0)
        {
            foreach (var teleporter in layout.Teleporters)
            {
                var block = map.Blocks[teleporter.Position.X, teleporter.Position.Y];
                block.ObjectIndex = theme.TeleporterObject;
                block.MapEventId = events.AddEntry(AutomapType.Teleporter,
                    EventFactory.Teleport(mapIndex, X(teleporter.Target), Y(teleporter.Target), teleporter.Direction));
            }
        }

        for (int y = 0; y < layout.Height; y++)
            for (int x = 0; x < layout.Width; x++)
                map.InitialBlocks[x, y] = map.Blocks[x, y].Clone();

        // Characters
        int characterIndex = 0;
        var tileFlags = (TileFlags)((theme.Labdata.CombatBackground << 28) | 0x110);

        void AddCharacter(Map.CharacterReference character, Pos position)
        {
            character.Positions.Add(new Position(position.X + 1, position.Y + 1));
            character.CollisionClass = 1;
            character.CombatBackgroundIndex = theme.Labdata.CombatBackground;
            character.TileFlags = tileFlags;
            map.CharacterReferences[characterIndex++] = character;
        }

        var groupBuilder = new MonsterGroupBuilder(api, theme, state.Seed, floor);

        if (layout.Boss != null)
        {
            var (group, sprite) = groupBuilder.CreateBossGroup(layout.IsBossWithMinionsFloor, state.BossItem);
            uint groupIndex = FirstMonsterGroupIndex + (uint)characterIndex;
            api.SetMonsterGroup(groupIndex, group);
            AddCharacter(new Map.CharacterReference
            {
                Type = CharacterType.Monster,
                Index = groupIndex,
                GraphicIndex = theme.GetSpriteObject(sprite),
                CharacterFlags = Map.CharacterReference.Flags.None // bosses don't move
            }, layout.Boss.Value);
        }

        if (layout.Recruit != null && state.Recruit != 0)
        {
            AddCharacter(new Map.CharacterReference
            {
                Type = CharacterType.PartyMember,
                Index = state.Recruit,
                GraphicIndex = theme.GetSpriteObject("person"),
                CharacterFlags = Map.CharacterReference.Flags.RandomMovement
            }, layout.Recruit.Value);
        }

        foreach (var monster in layout.Monsters)
        {
            var (group, sprite) = groupBuilder.CreateGroup();
            uint groupIndex = FirstMonsterGroupIndex + (uint)characterIndex;
            api.SetMonsterGroup(groupIndex, group);
            var flags = Map.CharacterReference.Flags.RandomMovement;

            if (monster.OnlyMoveWhenSeeingPlayer)
                flags |= Map.CharacterReference.Flags.MoveOnlyWhenSeePlayer;

            AddCharacter(new Map.CharacterReference
            {
                Type = CharacterType.Monster,
                Index = groupIndex,
                GraphicIndex = theme.GetSpriteObject(sprite),
                CharacterFlags = flags
            }, monster.Position);
        }

        api.SetMap(map);

        return map;
    }

    /// <summary>
    /// Prepares the savegame for a floor which is entered for the first time:
    /// clears the map state, fills the chests, sets the door locks and prepares the recruit.
    /// </summary>
    public void InitializeNewFloor(Savegame savegame, FloorLayout layout, DCState state)
    {
        uint mapIndex = MapIndexForFloor(layout.Floor);
        api.ResetMapState(savegame, mapIndex);

        var loot = new LootGenerator(api, FloorGenerator.CreateRandom(state.Seed, layout.Floor, 3), layout.Floor);

        for (int i = 0; i < layout.Chests.Count; i++)
        {
            var spot = layout.Chests[i];
            uint chestIndex = FirstChestIndex + (uint)i;
            savegame.Chests[chestIndex] = loot.CreateChest(spot.Kind);

            if (spot.Locked)
                savegame.LockChest(chestIndex - 1);
            else
                savegame.UnlockChest(chestIndex - 1);
        }

        for (int i = 0; i < layout.Doors.Count; i++)
        {
            if (layout.Doors[i].Locked)
                savegame.LockDoor((uint)i);
            else
                savegame.UnlockDoor((uint)i);
        }

        state.BossItem = layout.IsBossFloor ? loot.SelectBossItem(PartyOf(savegame)) : 0;
        state.Recruit = 0;

        if (layout.IsRecruitFloor)
            state.Recruit = PrepareRecruit(savegame, layout.Floor, state);
    }

    static List<PartyMember> PartyOf(Savegame savegame)
        => [.. Enumerable.Range(0, 6).Select(savegame.GetPartyMember).Where(p => p != null).Cast<PartyMember>()];

    uint PrepareRecruit(Savegame savegame, int floor, DCState state)
    {
        var random = FloorGenerator.CreateRandom(state.Seed, floor, 4);
        var party = PartyOf(savegame);

        if (party.Count >= 6)
            return 0;

        // The party member slots of the original characters (except the hero) are used for the
        // random recruits. Unused slots are preferred, otherwise the oldest recruit which is not
        // part of the party is replaced.
        var freeSlots = Enumerable.Range(2, (int)LastRecruitSlot - 1).Select(i => (uint)i)
            .Where(index => !savegame.CurrentPartyMemberIndices.Contains(index))
            .ToList();

        if (freeSlots.Count == 0)
            return 0;

        uint recruitIndex = freeSlots.FirstOrDefault(index => !state.UsedRecruitSlots.Contains(index));

        if (recruitIndex == 0)
            recruitIndex = state.UsedRecruitSlots.First(freeSlots.Contains);

        state.UsedRecruitSlots.Remove(recruitIndex);
        state.UsedRecruitSlots.Add(recruitIndex);

        uint averageLevel = (uint)Math.Round(party.Average(p => (double)p.Level));
        uint targetLevel = Math.Clamp(Math.Max(averageLevel, (uint)(floor * 3 / 4)), 1, 50);

        var recruit = new RecruitGenerator(api, random).Create(savegame, recruitIndex, targetLevel);
        state.RecruitTexts[recruitIndex] = recruit.Texts;

        new LootGenerator(api, random, floor).EquipRecruit(recruit);

        return recruitIndex;
    }

    /// <summary>
    /// Builds the event lists of a map. Each entry is a chain of events.
    /// </summary>
    class MapEventBuilder(Map map)
    {
        readonly Map map = map;

        /// <summary>
        /// Adds an event chain and returns the map event id (1-based) for the block.
        /// </summary>
        public uint AddEntry(AutomapType automapType, params Event[] chain)
        {
            if (map.EventList.Count >= 64)
                throw new InvalidOperationException("Too many map events.");

            for (int i = 0; i < chain.Length; i++)
            {
                chain[i].Index = (uint)map.Events.Count + 1;
                chain[i].Next = i < chain.Length - 1 ? chain[i + 1] : null;
                map.Events.Add(chain[i]);
            }

            map.EventList.Add(chain[0]);
            map.EventAutomapTypes.Add(automapType);

            return (uint)map.EventList.Count;
        }
    }
}
