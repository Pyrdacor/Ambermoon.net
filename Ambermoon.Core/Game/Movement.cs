using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon;

partial class GameCore
{
    readonly bool[] keys = new bool[EnumHelper.GetValues<Key>().Length];
    readonly Func<List<Key>> pressedKeyProvider;
    uint lastMoveTicksReset = 0;
    readonly Movement movement;

    internal void ResetMoveKeys(bool forceDisable = false)
    {
        var pressedKeys = pressedKeyProvider?.Invoke();

        void ResetKey(Key key) => keys[(int)key] = !forceDisable && pressedKeys?.Contains(key) == true;

        ResetKey(Key.Up);
        ResetKey(Key.Down);
        ResetKey(Key.Left);
        ResetKey(Key.Right);
        ResetKey(Key.W);
        ResetKey(Key.A);
        ResetKey(Key.S);
        ResetKey(Key.D);
        ResetKey(Key.Q);
        ResetKey(Key.E);

        if (!WindowActive && !layout.PopupActive && layout.ButtonGridPage == 0)
        {
            layout.ReleaseButton(0, true);
            layout.ReleaseButton(1, true);
            layout.ReleaseButton(2, true);
            layout.ReleaseButton(3, true);
            layout.ReleaseButton(5, true);
            layout.ReleaseButton(6, true);
            layout.ReleaseButton(7, true);
            layout.ReleaseButton(8, true);
        }

        lastMoveTicksReset = CurrentTicks;
    }

    internal void Move(bool fromNumpadButton, float speedFactor3D, params CursorType[] cursorTypes)
    {
        if (Is3D)
        {
            bool moveForward = cursorTypes.Contains(CursorType.ArrowForward);
            bool moveBackward = cursorTypes.Contains(CursorType.ArrowBackward);
            bool turnLeft = moveForward ? cursorTypes.Contains(CursorType.ArrowTurnLeft) : cursorTypes.Contains(CursorType.ArrowRotateLeft);
            bool turnRight = moveForward ? cursorTypes.Contains(CursorType.ArrowTurnRight) : cursorTypes.Contains(CursorType.ArrowRotateRight);

            if (CanPartyMove())
            {
                bool strafeLeft = cursorTypes.Contains(CursorType.ArrowStrafeLeft);
                bool strafeRight = cursorTypes.Contains(CursorType.ArrowStrafeRight);

                if (moveForward)
                {
                    if (strafeLeft || turnLeft)
                    {
                        player3D!.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else if (strafeRight || turnRight)
                    {
                        player3D!.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else
                        player3D!.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                }
                else if (moveBackward)
                {
                    if (strafeLeft || turnLeft)
                    {
                        player3D!.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else if (strafeRight || turnRight)
                    {
                        player3D!.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else
                        player3D!.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                }
                else if (cursorTypes.Contains(CursorType.ArrowStrafeLeft))
                    player3D!.MoveLeft(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                else if (cursorTypes.Contains(CursorType.ArrowStrafeRight))
                    player3D!.MoveRight(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
            }

            if (!moveForward && !moveBackward)
            {
                void PlayTurnSequence(int steps, Action turnAction)
                {
                    PlayTimedSequence(steps, () =>
                    {
                        turnAction?.Invoke();
                        CurrentSavegame!.CharacterDirection = player!.Direction = player3D!.Direction;
                    }, 65);
                }

                if (cursorTypes.Contains(CursorType.ArrowTurnLeft))
                {
                    player3D!.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                    if (!fromNumpadButton && CanPartyMove())
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                }
                else if (cursorTypes.Contains(CursorType.ArrowTurnRight))
                {
                    player3D!.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                    if (!fromNumpadButton && CanPartyMove())
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                }
                else if (cursorTypes.Contains(CursorType.ArrowRotateLeft))
                {
                    if (fromNumpadButton)
                    {
                        PlayTurnSequence(12, () => player3D!.TurnLeft(15.0f));
                    }
                    else
                    {
                        player3D!.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        if (CanPartyMove())
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                }
                else if (cursorTypes.Contains(CursorType.ArrowRotateRight))
                {
                    if (fromNumpadButton)
                    {
                        PlayTurnSequence(12, () => player3D!.TurnRight(15.0f));
                    }
                    else
                    {
                        player3D!.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        if (CanPartyMove())
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                }
            }

            if (cursorTypes.Length == 1 && (cursorTypes[0] < CursorType.ArrowForward || cursorTypes[0] > CursorType.Wait))
            {
                clickMoveActive = false;
                UntrapMouse();
            }

            CurrentSavegame.CharacterDirection = player!.Direction = player3D!.Direction;
        }
        else
        {
            switch (cursorTypes[0])
            {
                case CursorType.ArrowUpLeft:
                    Move2D(-1, -1);
                    break;
                case CursorType.ArrowUp:
                    Move2D(0, -1);
                    break;
                case CursorType.ArrowUpRight:
                    Move2D(1, -1);
                    break;
                case CursorType.ArrowLeft:
                    Move2D(-1, 0);
                    break;
                case CursorType.ArrowRight:
                    Move2D(1, 0);
                    break;
                case CursorType.ArrowDownLeft:
                    Move2D(-1, 1);
                    break;
                case CursorType.ArrowDown:
                    Move2D(0, 1);
                    break;
                case CursorType.ArrowDownRight:
                    Move2D(1, 1);
                    break;
                default:
                    clickMoveActive = false;
                    break;
            }

            CurrentSavegame.CharacterDirection = player!.Direction = player2D!.Direction;
        }
    }

    bool Move2D(int x, int y)
    {
        if (!CanPartyMove())
            return false;

        bool Move()
        {
            bool diagonal = x != 0 && y != 0;

            if (!player2D!.Move(x, y, CurrentTicks, TravelType, out bool eventTriggered, !diagonal, null, !diagonal))
            {
                if (eventTriggered || !diagonal)
                    return false;

                var prevDirection = player2D.Direction;

                if (!player2D.Move(0, y, CurrentTicks, TravelType, out eventTriggered, false, prevDirection, false))
                {
                    if (eventTriggered)
                        return false;

                    return player2D.Move(x, 0, CurrentTicks, TravelType, out _, true, prevDirection);
                }
            }

            return true;
        }

        bool result = Move();

        if (result)
            GameTime!.MoveTick(Map!, travelType);

        return result;
    }

    bool DisallowMoving() => paused || WindowActive || !InputEnable || allInputDisabled || pickingNewLeader || pickingTargetPlayer || pickingTargetInventory;

    void Move(bool tapped = false)
    {
        if (DisallowMoving())
            return;

        bool left = ((!is3D || !CoreConfiguration.TurnWithArrowKeys) && keys[(int)Key.Left]) || ((!is3D || CoreConfiguration.Movement3D == Movement3D.WASDQE) && keys[(int)Key.A]);
        bool right = ((!is3D || !CoreConfiguration.TurnWithArrowKeys) && keys[(int)Key.Right]) || ((!is3D || CoreConfiguration.Movement3D == Movement3D.WASDQE) && keys[(int)Key.D]);
        bool up = keys[(int)Key.Up] || keys[(int)Key.W];
        bool down = keys[(int)Key.Down] || keys[(int)Key.S];
        bool turnLeft = (CoreConfiguration.TurnWithArrowKeys && keys[(int)Key.Left]) || (CoreConfiguration.Movement3D == Movement3D.WASDQE ? keys[(int)Key.Q] : keys[(int)Key.A]);
        bool turnRight = (CoreConfiguration.TurnWithArrowKeys && keys[(int)Key.Right]) || (CoreConfiguration.Movement3D == Movement3D.WASDQE ? keys[(int)Key.E] : keys[(int)Key.D]);

        if (left && !right)
        {
            if (!is3D)
            {
                // diagonal movement is handled in up/down
                if (!up && !down)
                    Move2D(-1, 0);
            }
            else if (CanPartyMove())
            {
                player3D!.MoveLeft(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;
            }
        }
        else if (right && !left)
        {
            if (!is3D)
            {
                // diagonal movement is handled in up/down
                if (!up && !down)
                    Move2D(1, 0);
            }
            else if (CanPartyMove())
            {
                player3D!.MoveRight(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;
            }
        }
        if (is3D)
        {
            if (turnLeft && !turnRight)
            {
                int turns = tapped ? 6 : 1;

                player3D!.TurnLeft(movement.TurnSpeed3D * turns);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;
            }
            else if (!turnLeft && turnRight)
            {
                int turns = tapped ? 6 : 1;

                player3D!.TurnRight(movement.TurnSpeed3D * turns);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;
            }
        }
        if (up && !down)
        {
            if (!is3D)
            {
                int x = left && !right ? -1 :
                    right && !left ? 1 : 0;
                Move2D(x, -1);
            }
            else if (CanPartyMove())
            {
                bool moved = player3D!.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;

                if (tapped && moved)
                {
                    // Tapping in 3D will move 6 times to make some distance
                    for (int i = 0; i < 5; i++)
                    {
                        moved = player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                        CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;

                        if (!moved)
                            break;
                    }
                }
            }
        }
        else if (down && !up)
        {
            if (!is3D)
            {
                int x = left && !right ? -1 :
                    right && !left ? 1 : 0;
                Move2D(x, 1);
            }
            else if (CanPartyMove())
            {
                player3D!.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player!.Direction = player3D.Direction;
            }
        }
    }

    class Movement(bool legacyMode, bool mobile)
    {
        readonly uint[] tickDivider =
        [
            GetTickDivider3D(legacyMode), // 3D movement
            // 2D movement
            7,  // Indoor
            4,  // Outdoor walk
            8,  // Horse
            4,  // Raft
            8,  // Ship
            4,  // Magical disc
            15, // Eagle
            30, // Fly
            4,  // Swim
            10, // Witch broom
            8,  // Sand lizard
            8,  // Sand ship
            15, // Wasp
        ];

        uint TickDivider(bool is3D, bool worldMap, TravelType travelType) => tickDivider[is3D ? 0 : !worldMap ? (mobile ? 2 : 1) : 2 + (int)travelType];
        public uint MovementTicks(bool is3D, bool worldMap, TravelType travelType) => TicksPerSecond / TickDivider(is3D, worldMap, travelType);
        public float MoveSpeed3D { get; } = GetMoveSpeed3D(legacyMode, mobile);
        public float TurnSpeed3D { get; } = GetTurnSpeed3D(legacyMode, mobile);

        static uint GetTickDivider3D(bool legacyMode) => legacyMode ? 8u : 60u;
        static float GetMoveSpeed3D(bool legacyMode, bool mobile) => mobile ? 0.03f : legacyMode ? 0.25f : 0.04f;
        static float GetTurnSpeed3D(bool legacyMode, bool mobile) => mobile ? 1.5f : legacyMode ? 15.0f : 2.0f;
    }
}
