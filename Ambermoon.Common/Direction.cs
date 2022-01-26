using System.ComponentModel;

namespace Ambermoon
{
    public enum Direction
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    public enum CharacterDirection
    {
        Up,
        Right,
        Down,
        Left,
        Random,
        Keep = Random
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DirectionExtensions
    {
        public static Direction ToDirection(this CharacterDirection characterDirection) => characterDirection switch
        {
            CharacterDirection.Up => Direction.Up,
            CharacterDirection.Right => Direction.Right,
            CharacterDirection.Down => Direction.Down,
            CharacterDirection.Left => Direction.Left,
            _ => throw new AmbermoonException(ExceptionScope.Application, $"Character direction {characterDirection} can not be converted to a general direction.")
        };

        public static uint ToAngle(this CharacterDirection characterDirection) => characterDirection switch
        {
            CharacterDirection.Up => 0,
            CharacterDirection.Right => 90,
            CharacterDirection.Down => 180,
            CharacterDirection.Left => 270,
            _ => throw new AmbermoonException(ExceptionScope.Application, $"Character direction {characterDirection} can not be converted to an angle.")
        };
    }
}
