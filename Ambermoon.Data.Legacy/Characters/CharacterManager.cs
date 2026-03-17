using Ambermoon.Data.Legacy.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Characters
{
    public class CharacterManager : ICharacterManager
    {
        readonly Dictionary<uint, PartyMember> initialPartyMembers = [];
        readonly Dictionary<uint, NPC> npcs = [];
        readonly Dictionary<uint, Monster> monsters = [];
        readonly Dictionary<uint, MonsterGroup> monsterGroups = [];

        public CharacterManager(ILegacyGameData gameData)
        {
            var partyMemberReader = new PartyMemberReader();
			var npcReader = new NPCReader();
            var monsterReader = new MonsterReader(gameData);
            var monsterGroupReader = new MonsterGroupReader();

            if (!gameData.Files.TryGetValue("Initial/Party_char.amb", out var partyCharacterContainer))
                partyCharacterContainer = gameData.Files["Save.00/Party_char.amb"];
            if (!gameData.Files.TryGetValue("Monster_char_data.amb", out var monsterDataContainer))
                monsterDataContainer = gameData.Files["Monster_char.amb"];

            foreach (var partyMemberFile in partyCharacterContainer.Files.Where(f => f.Value.Size != 0))
                initialPartyMembers.Add((uint)partyMemberFile.Key, PartyMember.Load((uint)partyMemberFile.Key, partyMemberReader, partyMemberFile.Value, gameData.Files["Party_texts.amb"].Files[partyMemberFile.Key]));
            foreach (var npcFile in gameData.Files["NPC_char.amb"].Files.Where(f => f.Value.Size != 0))
                npcs.Add((uint)npcFile.Key, NPC.Load((uint)npcFile.Key, npcReader, npcFile.Value, gameData.Files["NPC_texts.amb"].Files[npcFile.Key]));
            foreach (var monsterFile in monsterDataContainer.Files.Where(f => f.Value.Size != 0))
                monsters.Add((uint)monsterFile.Key, Monster.Load((uint)monsterFile.Key, monsterReader, monsterFile.Value));
            foreach (var monsterGroupFile in gameData.Files["Monster_groups.amb"].Files.Where(f => f.Value.Size != 0)) // load after monsters!
                monsterGroups.Add((uint)monsterGroupFile.Key, MonsterGroup.Load(this, monsterGroupReader, monsterGroupFile.Value));
		}

        public Monster GetMonster(uint index) => index == 0 || !monsters.TryGetValue(index, out Monster value) ? null : value;

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

        public PartyMember GetInitialPartyMember(uint index) => index == 0 || !initialPartyMembers.TryGetValue(index, out PartyMember value) ? null : value;

        public NPC GetNPC(uint index) => index == 0 || !npcs.TryGetValue(index, out NPC value) ? null : value;

        public MonsterGroup GetMonsterGroup(uint index) => index == 0 || !monsterGroups.TryGetValue(index, out MonsterGroup value) ? null : value;

        public IReadOnlyList<PartyMember> InitialPartyMembers => initialPartyMembers.Values.ToList().AsReadOnly();
        public IReadOnlyList<NPC> NPCs => npcs.Values.ToList().AsReadOnly();
        public IReadOnlyList<Monster> Monsters => monsters.Values.ToList().AsReadOnly();
        public IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups => monsterGroups.AsReadOnly();
        public IGraphicAtlas MonsterGraphicAtlas => null; // If not given, the CombatGraphic of the Monster is used to create the texture atlas.
    }
}
