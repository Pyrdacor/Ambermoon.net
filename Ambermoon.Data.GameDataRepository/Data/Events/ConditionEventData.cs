using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Util;

    /// <summary>
    /// Checks a condition and branches based on the result.
    /// </summary>
    public class ConditionEventData : EventData, IBranchEvent
    {

        #region Fields

        // TODO
        private readonly EventReferenceDataProperty _conditionFalseEventIndex = new();

        #endregion


        #region Properties

        [Range(0, GameDataRepository.MaxEvents)]
        [DefaultValue(null)]
        public uint? ConditionFalseEventIndex
        {
            get => _conditionFalseEventIndex.Get(this);
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, GameDataRepository.MaxEvents);
                SetField(_conditionFalseEventIndex, value);
            }
        }

        uint? IBranchEvent.BranchEventIndex => ConditionFalseEventIndex;

        #endregion


        #region Constructors

        public ConditionEventData()
        {
            Data[0] = (byte)EventType.Condition;
            // TODO
            ConditionFalseEventIndex = null;
            NextEventIndex = null;
        }

        internal ConditionEventData(EventData data)
        {
            // TODO
            _conditionFalseEventIndex.Copy(data, this);
        }

        #endregion

    }
}
