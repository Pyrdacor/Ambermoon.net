using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Util;
using static TrapEvent;

/// <summary>
/// Adds damage and/or an ailment to the target.
///
/// The target can avoid the effect by a successful check against its luck.
///
/// The actual damage is calculated as follows: Damage + rand(0, (Damage / 2) - 1)
///
/// If Damage is 0, no damage is inflicted, but the ailment is still applied.
///
/// This event is often bound to door or chest locks but can also be used for
/// map traps, poison or just any form of punishment in the game.
/// </summary>
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

    public TrapEventData()
    {
        Data[0] = (byte)EventType.Trap;
        NextEventIndex = null;
    }

    internal TrapEventData(EventData data)
    {
        _inflictedAilment.Copy(data, this);
        _target.Copy(data, this);
        _affectedGenders.Copy(data, this);
        _damage.Copy(data, this);
    }

    #endregion

}
