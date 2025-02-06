using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon.Data.Pyrdacor
{
    internal class CharacterManager : ICharacterManager
    {
        readonly Lazy<Dictionary<uint, NPC>> npcs;
        readonly Lazy<Dictionary<uint, Monster>> monsters;        
        readonly Lazy<Dictionary<uint, MonsterGroup>> monsterGroups;

        public CharacterManager(Func<Dictionary<uint, NPC>> npcProvider,
            Func<Dictionary<uint, Monster>> monsterProvider,
            Func<Dictionary<uint, MonsterGroup>> monsterGroupProvider)
        {
            npcs = new Lazy<Dictionary<uint, NPC>>(npcProvider);
            monsters = new Lazy<Dictionary<uint, Monster>>(monsterProvider);
            monsterGroups = new Lazy<Dictionary<uint, MonsterGroup>>(monsterGroupProvider);
        }

        public Monster? GetMonster(uint index) => index == 0 || !monsters.Value.TryGetValue(index, out Monster? value) ? null : value;

        public NPC? GetNPC(uint index) => index == 0 || !npcs.Value.TryGetValue(index, out NPC? value) ? null : value;

        public MonsterGroup? GetMonsterGroup(uint index) => index == 0 || !monsterGroups.Value.TryGetValue(index, out MonsterGroup? value) ? null : value;

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
}
