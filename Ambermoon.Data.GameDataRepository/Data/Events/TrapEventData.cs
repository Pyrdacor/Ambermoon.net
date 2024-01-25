namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Ambermoon.Data.GameDataRepository.Util;
    using Ambermoon.Data.Legacy.ExecutableData;
    using System.ComponentModel.DataAnnotations;
    using static TrapEvent;

    public enum SpinnerDirection
    {
        North,
        East,
        South,
        West,
        Random
    }

    public class TrapEventData : EventData
    {

        #region Fields

        private readonly EnumEventDataProperty<TrapAilment> _inflictedAilment = new(1);
        private readonly EnumEventDataProperty<TrapTarget> _target = new(2);
        private readonly EnumEventDataProperty<GenderFlag> _affectedGenders = new(3);
        private readonly ByteEventDataProperty _damage = new(4);
        // Bytes 5 to 9 are unused

        #endregion


        #region Properties

        public TrapAilment InflictedAilment
        {
            get => _inflictedAilment.Get(this);
            set => SetField(_inflictedAilment, value);
        }

        public TrapTarget Target
        {
            get => _target.Get(this);
            set => SetField(_target, value);
        }

        public GenderFlag AffectedGenders
        {
            get => _affectedGenders.Get(this);
            set => SetField(_affectedGenders, value);
        }

        [Range(0, byte.MaxValue)]
        public uint Damage
        {
            get => _damage.Get(this);
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(_damage, value);
            }
        }

        public override bool AllowInConversations => false;

        #endregion


        #region Constructors

        internal TrapEventData(EventData data)
        {
            _inflictedAilment.Copy(data, this);
            _target.Copy(data, this);
            _affectedGenders.Copy(data, this);
            _damage.Copy(data, this);
        }

        #endregion

    }
}
