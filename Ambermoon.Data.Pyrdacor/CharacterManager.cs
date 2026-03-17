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
    Func<GraphicAtlas> monsterGraphicProvider)
    : ICharacterManager
{
    readonly Lazy<Dictionary<uint, PartyMember>> initialPartyMembers = new(initialPartyMemberProvider);
    readonly Lazy<Dictionary<uint, NPC>> npcs = new(npcProvider);
    readonly Lazy<Dictionary<uint, Monster>> monsters = new(monsterProvider);        
    readonly Lazy<Dictionary<uint, MonsterGroup>> monsterGroups = new(monsterGroupProvider);
    readonly Lazy<GraphicAtlas> monsterGraphics = new(monsterGraphicProvider);

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

    public IReadOnlyList<PartyMember> InitialPartyMembers => initialPartyMembers.Value.Values.ToList().AsReadOnly();
    public IReadOnlyList<NPC> NPCs => npcs.Value.Values.ToList().AsReadOnly();
	public IReadOnlyList<Monster> Monsters => monsters.Value.Values.ToList().AsReadOnly();
    public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.Value.AsReadOnly();
    public IGraphicAtlas? MonsterGraphicAtlas => monsterGraphics.Value;
}
