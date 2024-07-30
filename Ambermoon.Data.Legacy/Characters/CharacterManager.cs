using Ambermoon.Data.Legacy.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Characters
{
    public class CharacterManager : ICharacterManager
    {
		readonly Dictionary<uint, NPC> npcs = new Dictionary<uint, NPC>();
        readonly Dictionary<uint, Monster> monsters = new Dictionary<uint, Monster>();        
        readonly Dictionary<uint, MonsterGroup> monsterGroups = new Dictionary<uint, MonsterGroup>();

        public CharacterManager(ILegacyGameData gameData)
        {
			var npcReader = new NPCReader();
            var monsterReader = new MonsterReader(gameData);
            var monsterGroupReader = new MonsterGroupReader();

            if (!gameData.Files.TryGetValue("Monster_char_data.amb", out var monsterDataContainer))
                monsterDataContainer = gameData.Files["Monster_char.amb"];

            foreach (var npcFile in gameData.Files["NPC_char.amb"].Files.Where(f => f.Value.Size != 0))
                npcs.Add((uint)npcFile.Key, NPC.Load((uint)npcFile.Key, npcReader, npcFile.Value, gameData.Files["NPC_texts.amb"].Files[npcFile.Key]));
            foreach (var monsterFile in monsterDataContainer.Files.Where(f => f.Value.Size != 0))
                monsters.Add((uint)monsterFile.Key, Monster.Load((uint)monsterFile.Key, monsterReader, monsterFile.Value));
            foreach (var monsterGroupFile in gameData.Files["Monster_groups.amb"].Files.Where(f => f.Value.Size != 0)) // load after monsters!
                monsterGroups.Add((uint)monsterGroupFile.Key, MonsterGroup.Load(this, monsterGroupReader, monsterGroupFile.Value));
		}

        public Monster GetMonster(uint index) => index == 0 || !monsters.ContainsKey(index) ? null : monsters[index];

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

		public NPC GetNPC(uint index) => index == 0 || !npcs.ContainsKey(index) ? null : npcs[index];

        public MonsterGroup GetMonsterGroup(uint index) => index == 0 || !monsterGroups.ContainsKey(index) ? null : monsterGroups[index];

        public IReadOnlyList<NPC> NPCs => npcs.Values.ToList();
        public IReadOnlyList<Monster> Monsters => monsters.Values.ToList();
        public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups;
    }
}
