using System.ComponentModel.DataAnnotations;
using Ambermoon.Data.GameDataRepository.Util;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    /// <summary>
    /// Starts a battle against a monster group.
    /// </summary>
    public class StartBattleEventData : EventData
    {

        #region Fields

        // Bytes 2 to 5 are unused
        private readonly WordEventDataProperty _monsterGroupIndex = new(6);
        // Bytes 8 to 9 are unused

        #endregion


        #region Properties

        [Range(0, ushort.MaxValue)]
        public uint MonsterGroupIndex
        {
            get => _monsterGroupIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                _monsterGroupIndex.Set(this, value);
            }
        }

        public override bool AllowInConversations => false;

        #endregion


        #region Constructors

        public StartBattleEventData()
        {
            Data[0] = (byte)EventType.StartBattle;
            NextEventIndex = null;
        }

        internal StartBattleEventData(EventData data)
        {
            _monsterGroupIndex.Copy(data, this);
        }

        #endregion

    }
}
