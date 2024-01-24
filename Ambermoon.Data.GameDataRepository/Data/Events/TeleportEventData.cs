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
            get => _mapIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapWidth);
                SetField(_mapIndex, value);
            }
        }

        [Range(0, GameDataRepository.MaxMapHeight)]
        public uint Y
        {
            get => _mapIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapHeight);
                SetField(_mapIndex, value);
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

        [Range(0, ushort.MaxValue)]
        public uint MapIndex
        {
            get => _mapIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(_mapIndex, value);
            }
        }

        #endregion


        #region Constructors

        internal TeleportEventData(EventData data)
        {
            _mapIndex.Copy(data, this);

            Data = (byte[])data.Data.Clone();
            X = data.Data[1];
            Y = data.Data[2];
            Direction = (CharacterDirection)data.Data[3];
            NewTravelType = data.Data[4] == 0xff ? null : (TravelType)data.Data[4];
            Transition = (TransitionType)data.Data[5];
            MapIndex = data.FirstWord;
        }

        #endregion

    }
}
