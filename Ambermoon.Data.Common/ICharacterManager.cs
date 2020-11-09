namespace Ambermoon.Data
{
    public interface ICharacterManager
    {
        NPC GetNPC(uint index);
        Monster GetMonster(uint index);
        MonsterGroup GetMonsterGroup(uint index);
        Monster[] Monsters { get; }
    }
}
