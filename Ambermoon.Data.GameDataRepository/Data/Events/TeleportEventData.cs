using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Ambermoon.Data.Enumerations;
    using Util;
    using static TeleportEvent;

    public class TeleportEventData : EventData
    {

        #region Fields

        private readonly ByteEventDataProperty _x = new(1);
        private readonly ByteEventDataProperty _y = new(2);
        private readonly EnumEventDataProperty<CharacterDirection> _direction = new(3);
        private readonly NullableEventDataProperty<TravelType> _newTravelType = new(new EnumEventDataProperty<TravelType>(4), 0xff);
        private readonly EnumEventDataProperty<TransitionType> _transition = new(5);
        private readonly WordEventDataProperty _mapIndex = new(6);

        #endregion


        #region Properties

        [Range(0, GameDataRepository.MaxMapWidth)]
        public uint X
        {
            get => _x.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapWidth);
                SetField(_x, value);
            }
        }

        [Range(0, GameDataRepository.MaxMapHeight)]
        public uint Y
        {
            get => _y.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapHeight);
                SetField(_y, value);
            }
        }

        public CharacterDirection Direction
        {
            get => _direction.Get(this);
            set => SetField(_direction, value);
        }

        public TravelType? NewTravelType
        {
            get => _newTravelType.Get(this);
            set => SetField(_newTravelType, value);
        }

        public TransitionType Transition
        {
            get => _transition.Get(this);
            set => SetField(_transition, value);
        }

        [Range(0, GameDataRepository.MaxMaps)]
        public uint MapIndex
        {
            get => _mapIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMaps);
                SetField(_mapIndex, value);
            }
        }

        #endregion


        #region Constructors

        internal TeleportEventData(EventData data)
        {
            _x.Copy(data, this);
            _y.Copy(data, this);
            _direction.Copy(data, this);
            _newTravelType.Copy(data, this);
            _transition.Copy(data, this);
            _mapIndex.Copy(data, this);
        }

        #endregion

    }
}
