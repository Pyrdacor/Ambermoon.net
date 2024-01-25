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

    /// <summary>
    /// Represents a spinner on a 3D map.
    /// </summary>
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

        public override bool AllowOn2DMaps => false;

        #endregion


        #region Constructors

        public SpinnerEventData()
        {
            Data[0] = (byte)EventType.Spinner;
            Direction = SpinnerDirection.Random;
            NextEventIndex = null;
        }

        internal SpinnerEventData(EventData data)
        {
            _direction.Copy(data, this);
        }

        #endregion

    }
}
