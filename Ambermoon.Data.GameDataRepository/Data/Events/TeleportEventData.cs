using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Ambermoon.Data.Enumerations;
using Util;
using static TeleportEvent;

public enum TeleportDirection
{
    North,
    East,
    South,
    West,
    Unchanged
}

/// <summary>
/// Represents any location change on a map.
///
/// Mostly used for map exits and teleporters, but also for:
/// - Wind gates
/// - Starting the outro
///
/// Map changes can also be done through floor or ceiling
/// to implement falling and climbing or levitation.
/// </summary>
public class TeleportEventData : EventData
{

    #region Fields

    private readonly ByteEventDataProperty _x = new(1);
    private readonly ByteEventDataProperty _y = new(2);
    private readonly EnumEventDataProperty<TeleportDirection> _direction = new(3);
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

    public TeleportDirection Direction
    {
        get => _direction.Get(this);
        set => SetField(_direction, value);
    }

    [DefaultValue(null)]
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

    public bool SamePosition => X == 0 && Y == 0;
    public bool SameDirection => Direction == TeleportDirection.Unchanged;
    public bool SameMap => MapIndex == 0;
    public bool Invalid => (X == 0) != (Y == 0) || (SamePosition && SameMap);

    #endregion


    #region Constructors

    public TeleportEventData()
    {
        Data[0] = (byte)EventType.Teleport;
        NewTravelType = null;
        NextEventIndex = null;
    }

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


    #region Methods

    public void KeepPosition()
    {
        X = 0;
        Y = 0;
    }

    public void KeepDirection()
    {
        Direction = TeleportDirection.Unchanged;
    }

    public void KeepMap()
    {
        MapIndex = 0;
    }

    #endregion

}
