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

        public Monster? GetMonster(uint index) => index == 0 || !monsters.Value.ContainsKey(index) ? null : monsters.Value[index];

        public NPC? GetNPC(uint index) => index == 0 || !npcs.Value.ContainsKey(index) ? null : npcs.Value[index];

        public MonsterGroup? GetMonsterGroup(uint index) => index == 0 || !monsterGroups.Value.ContainsKey(index) ? null : monsterGroups.Value[index];

        public IReadOnlyList<Monster> Monsters => monsters.Value.Values.ToList().AsReadOnly();
        public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.Value.AsReadOnly();
    }
}
