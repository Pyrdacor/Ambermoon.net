namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    public enum SpinnerDirection
    {
        North,
        East,
        South,
        West,
        Random
    }

    public class SpinnerEventData : EventData
    {

        #region Fields

        private readonly EnumEventDataProperty<SpinnerDirection> _direction = new(1);
        // Bytes 2 to 9 are unused

        #endregion


        #region Properties

        public SpinnerDirection Direction
        {
            get => _direction.Get(this);
            set => SetField(_direction, value);
        }

        public override bool AllowInConversations => false;

        #endregion


        #region Constructors

        internal SpinnerEventData(EventData data)
        {
            _direction.Copy(data, this);
        }

        #endregion

    }
}
