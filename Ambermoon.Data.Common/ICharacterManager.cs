using System;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface ICharacterManager
    {
        PartyMember GetInitialPartyMember(uint index);
        NPC GetNPC(uint index);
        Monster GetMonster(uint index);
		Monster CloneMonster(Monster monster);
		MonsterGroup GetMonsterGroup(uint index);
        IReadOnlyList<PartyMember> InitialPartyMembers { get; }
        IReadOnlyList<NPC> NPCs { get; }
        IReadOnlyList<Monster> Monsters { get; }
        IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups { get; }
        Func<MonsterGroup, IGraphicAtlas> MonsterGraphicAtlasProvider { get; }
    }
}
