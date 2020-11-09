using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Characters
{
    public class CharacterManager : ICharacterManager
    {
        readonly Dictionary<uint, NPC> npcs = new Dictionary<uint, NPC>();
        readonly Dictionary<uint, Monster> monsters = new Dictionary<uint, Monster>();        
        readonly Dictionary<uint, MonsterGroup> monsterGroups = new Dictionary<uint, MonsterGroup>();

        public CharacterManager(IGameData gameData, IGraphicProvider graphicProvider)
        {
            var npcReader = new NPCReader();
            var monsterReader = new MonsterReader(gameData, graphicProvider);
            var monsterGroupReader = new MonsterGroupReader();

            foreach (var npcFile in gameData.Files["NPC_char.amb"].Files)
                npcs.Add((uint)npcFile.Key, NPC.Load((uint)npcFile.Key, npcReader, npcFile.Value, gameData.Files["NPC_texts.amb"].Files[npcFile.Key]));
            foreach (var monsterFile in gameData.Files["Monster_char_data.amb"].Files)
                monsters.Add((uint)monsterFile.Key, Monster.Load((uint)monsterFile.Key, monsterReader, monsterFile.Value));
            foreach (var monsterGroupFile in gameData.Files["Monster_groups.amb"].Files) // load after monsters!
                monsterGroups.Add((uint)monsterGroupFile.Key, MonsterGroup.Load(this, monsterGroupReader, monsterGroupFile.Value));
        }

        public Monster GetMonster(uint index) => index == 0 ? null : monsters[index];

        public NPC GetNPC(uint index) => index == 0 ? null : npcs[index];

        public MonsterGroup GetMonsterGroup(uint index) => index == 0 ? null : monsterGroups[index];

        public Monster[] Monsters => monsters.Values.ToArray();
    }
}
