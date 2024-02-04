using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Util;

    /// <summary>
    /// Opens a decision (yes/no) popup.
    /// </summary>
    public class DecisionEventData : EventData, IBranchEvent
    {

        #region Fields

        // TODO
        private readonly EventReferenceDataProperty _decisionNoEventIndex = new();

        #endregion


        #region Properties

        [Range(0, GameDataRepository.MaxEvents)]
        [DefaultValue(null)]
        public uint? DecisionNoEventIndex
        {
            get => _decisionNoEventIndex.Get(this);
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, GameDataRepository.MaxEvents);
                SetField(_decisionNoEventIndex, value);
            }
        }

        uint? IBranchEvent.BranchEventIndex => DecisionNoEventIndex;

        public override bool AllowInConversations => false;

        #endregion


        #region Constructors

        public DecisionEventData()
        {
            Data[0] = (byte)EventType.Decision;
            // TODO
            DecisionNoEventIndex = null;
            NextEventIndex = null;
        }

        internal DecisionEventData(EventData data)
        {
            // TODO
            _decisionNoEventIndex.Copy(data, this);
        }

        #endregion

    }
}
