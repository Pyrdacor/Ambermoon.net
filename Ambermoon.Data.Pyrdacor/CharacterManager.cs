using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Pyrdacor.Objects;

namespace Ambermoon.Data.Pyrdacor;

internal class CharacterManager(
    Func<Dictionary<uint, PartyMember>> initialPartyMemberProvider,
    Func<Dictionary<uint, NPC>> npcProvider,
    Func<Dictionary<uint, Monster>> monsterProvider,
    Func<Dictionary<uint, MonsterGroup>> monsterGroupProvider,
    Func<(GraphicAtlas Atlas, IReadOnlyDictionary<MonsterGraphicIndex, Size> Sizes)> monsterGraphicProvider)
    : ICharacterManager
{
    readonly Lazy<Dictionary<uint, PartyMember>> initialPartyMembers = new(initialPartyMemberProvider);
    readonly Lazy<Dictionary<uint, NPC>> npcs = new(npcProvider);
    readonly Lazy<Dictionary<uint, Monster>> monsters = new(monsterProvider);        
    readonly Lazy<Dictionary<uint, MonsterGroup>> monsterGroups = new(monsterGroupProvider);
    readonly Lazy<(GraphicAtlas Atlas, IReadOnlyDictionary<MonsterGraphicIndex, Size> Sizes)> monsterGraphics = new(monsterGraphicProvider);

    public PartyMember? GetInitialPartyMember(uint index) => initialPartyMembers.Value.GetByIndex(index);

    // NOTE: We don't provide a CombatGraphic for monsters as it is only used to create
    // the texture atlas but only if it is not provided in the MonsterGraphicAtlas
    // property (which we provide). Other projects which depend on the CombatGraphic
    // property of the Monster class might be affected by this!
    public Monster? GetMonster(uint index) => monsters.Value.GetByIndex(index);

    public NPC? GetNPC(uint index) => npcs.Value.GetByIndex(index);

    public MonsterGroup? GetMonsterGroup(uint index) => monsterGroups.Value.GetByIndex(index);

    public Monster CloneMonster(Monster monster)
    {
        var writer = new DataWriter();
        var monsterWriter = new MonsterWriter();
        monsterWriter.WriteMonster(monster, writer);

        var reader = DataReader.FromData(writer.ToArray());
        var monsterReader = new MonsterReader();
        var clone = Monster.Load(monster.Index, monsterReader, reader);
        clone.CombatGraphic = monster.CombatGraphic;

        return clone;
    }

    private GraphicAtlas CreateMonsterGraphicAtlas(MonsterGroup monsterGroup)
    {
        var (baseAtlas, sizes) = monsterGraphics.Value;
        var monsters = monsterGroup.Monsters.OfType<Monster>().DistinctBy(monster => monster.Index).ToList();
        var graphics = new Dictionary<uint, Graphic>(monsters.Count);

        foreach (var monster in monsters)
        {
            var offset = baseAtlas.Offsets[(uint)monster.CombatGraphicIndex];
            var size = sizes[monster.CombatGraphicIndex];
            var graphic = baseAtlas.Graphic.GetArea(offset.X, offset.Y, size.Width, size.Height);
            int numFrames = size.Width / (int)monster.FrameWidth;

            graphics.Add(monster.Index, graphic.CreateScaled(numFrames * (int)monster.MappedFrameWidth, (int)monster.MappedFrameHeight));
        }

        int x = 0;
        int y = 0;
        int width = 0;
        int nextY = 0;

        // Widest last
        var sortedGraphics = new List<KeyValuePair<uint, Graphic>>(graphics);
        sortedGraphics.Sort((a, b) => a.Value.Width.CompareTo(b.Value.Width));

        var textureAreas = new List<(uint Index, Position Position, Graphic Graphic)>(sortedGraphics.Count);

        while (sortedGraphics.Count != 0)
        {
            var (index, graphic) = sortedGraphics[^1];
            sortedGraphics.RemoveAt(sortedGraphics.Count - 1);

            if (y == 0)
            {
                textureAreas.Add((index, new Position(0, 0), graphic));
                y = graphic.Height;
                width = graphic.Width;
                nextY = y;
            }
            else
            {
                if (x + graphic.Width == width)
                {
                    textureAreas.Add((index, new Position(x, y), graphic));
                    y = Math.Max(nextY, y + graphic.Height);
                    nextY = y;
                    x = 0;
                }
                else if (x + graphic.Width < width)
                {
                    textureAreas.Add((index, new Position(x, y), graphic));
                    nextY = Math.Max(nextY, y + graphic.Height);
                    x += graphic.Width;
                }
                else
                {
                    // Search for smaller one
                    if (sortedGraphics.Count > 1)
                    {
                        bool foundMatch = false;

                        for (int i = graphics.Count - 2; i >= 0; i--)
                        {
                            var (id, g) = sortedGraphics[i];

                            if (x + g.Width <= width)
                            {
                                textureAreas.Add((id, new Position(x, y), g));
                                nextY = Math.Max(nextY, y + g.Height);

                                if (x + g.Width == width)
                                {
                                    y = nextY;
                                    x = 0;
                                }
                                else
                                {
                                    x += graphic.Width;
                                }

                                foundMatch = true;
                                sortedGraphics.RemoveAt(i);
                                break;
                            }
                        }

                        if (foundMatch)
                            continue;
                    }

                    y = nextY;
                    x = graphic.Width;
                    textureAreas.Add((index, new Position(0, y), graphic));
                    nextY = y + graphic.Height;
                }
            }
        }

        var atlasGraphic = new Graphic(width, nextY, 0);
        
        foreach (var textureArea in textureAreas)
        {
            atlasGraphic.AddOverlay(textureArea.Position.X, textureArea.Position.Y, textureArea.Graphic, false);
        }

        return new GraphicAtlas(atlasGraphic, textureAreas.ToDictionary(a => a.Index, a => a.Position));
    }

    public IReadOnlyList<PartyMember> InitialPartyMembers => initialPartyMembers.Value.Values.ToList().AsReadOnly();
    public IReadOnlyList<NPC> NPCs => npcs.Value.Values.ToList().AsReadOnly();
	public IReadOnlyList<Monster> Monsters => monsters.Value.Values.ToList().AsReadOnly();
    public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.Value.AsReadOnly();
    public Func<MonsterGroup, IGraphicAtlas>? MonsterGraphicAtlasProvider => CreateMonsterGraphicAtlas;
}
