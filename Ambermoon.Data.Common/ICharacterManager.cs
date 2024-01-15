using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface ICharacterManager
    {
        NPC GetNPC(uint index);
        Monster GetMonster(uint index);
        MonsterGroup GetMonsterGroup(uint index);
        IReadOnlyList<NPC> NPCs { get; }
        IReadOnlyList<Monster> Monsters { get; }
        IReadOnlyDictionary<uint, MonsterGroup> MonsterGroups { get; }
    }
}
