using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Extensions;

namespace Ambermoon.Data.Pyrdacor;

internal class CharacterManager(
    Func<Dictionary<uint, NPC>> npcProvider,
    Func<Dictionary<uint, Monster>> monsterProvider,
    Func<Dictionary<uint, MonsterGroup>> monsterGroupProvider)
    : ICharacterManager
{
    readonly Lazy<Dictionary<uint, NPC>> npcs = new(npcProvider);
    readonly Lazy<Dictionary<uint, Monster>> monsters = new(monsterProvider);        
    readonly Lazy<Dictionary<uint, MonsterGroup>> monsterGroups = new(monsterGroupProvider);

    public Monster? GetMonster(uint index) => monsters.Value.GetByIndex(index);

    public NPC? GetNPC(uint index) => npcs.Value.GetByIndex(index);

    public MonsterGroup? GetMonsterGroup(uint index) => monsterGroups.Value.GetByIndex(index);

    public Monster CloneMonster(Monster monster)
    {
        var writer = new DataWriter();
        var monsterWriter = new MonsterWriter();
        monsterWriter.WriteMonster(monster, writer);

        var reader = new DataReader(writer.ToArray());
        var monsterReader = new MonsterReader();
        var clone = Monster.Load(monster.Index, monsterReader, reader);
        clone.CombatGraphic = monster.CombatGraphic;

        return clone;
    }

    public IReadOnlyList<NPC> NPCs => npcs.Value.Values.ToList().AsReadOnly();
	public IReadOnlyList<Monster> Monsters => monsters.Value.Values.ToList().AsReadOnly();
    public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.Value.AsReadOnly();
}
