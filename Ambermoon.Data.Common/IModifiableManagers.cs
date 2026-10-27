using System.Collections.Generic;

namespace Ambermoon.Data
{
    /// <summary>
    /// Allows mods to add or replace maps and labdata at runtime.
    /// </summary>
    public interface IModifiableMapManager : IMapManager
    {
        /// <summary>
        /// Adds or replaces the map with the index of the given map.
        /// </summary>
        void SetMap(Map map);
        void SetLabdata(uint index, Labdata labdata);
    }

    /// <summary>
    /// Allows mods to add or replace characters and monster groups.
    /// </summary>
    public interface IModifiableCharacterManager : ICharacterManager
    {
        void SetMonster(Monster monster);
        void SetMonsterGroup(uint index, MonsterGroup monsterGroup);
        void SetInitialPartyMember(PartyMember partyMember);
        void SetNPC(NPC npc);
    }

    /// <summary>
    /// Allows mods to add or replace items.
    /// </summary>
    public interface IModifiableItemManager : IItemManager
    {
        void SetItem(Item item);
        void SetTexts(uint index, List<string> texts);
    }
}
