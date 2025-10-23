using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Util;

/// <summary>
/// Shows a locked door screen when triggered.
/// </summary>
public class DoorEventData : EventData, IBranchEvent
{

    #region Fields

    private readonly ByteEventDataProperty _lockedPercentage = new(1);
    private readonly ByteEventDataProperty _saveIndex = new(2);
    private readonly NullableEventDataProperty<uint> _closedDoorTextIndex = new(new ByteEventDataProperty(3), 0xff);
    private readonly NullableEventDataProperty<uint> _unlockTextIndex = new(new ByteEventDataProperty(4), 0xff);
    // Byte 5 is unused
    private readonly NullableEventDataProperty<uint> _keyIndex = new(new WordEventDataProperty(6), 0);
    private readonly EventReferenceDataProperty _unlockFailEventIndex = new();

    #endregion


    #region Properties

    /// <summary>
    /// Specifies how well the door is locked. 0 means unlocked, 100 means
    /// it can only be opened with a specific key. If the value is between
    /// those values, the door can be opened with a lock pick or with the
    /// lock picking skill. The higher the value, the harder it is to open.
    /// If the skill is used, a dice roll is performed against the user's
    /// skill level. If the roll is successful, the door is opened.
    /// Otherwise, traps associated with the door are triggered and the
    /// door remains closed.
    /// </summary>
    [Range(0, 100)]
    public uint LockedPercentage
    {
        get => _lockedPercentage.Get(this);
        set
        {
            ValueChecker.Check(value, 0, 100);
            if (value < 100 && KeyIndex is not null)
                throw new ArgumentException("Locked percentage must be 100 if a key index is set.");
            SetField(_lockedPercentage, value);
        }
    }

    /// <summary>
    /// Index of the door which is used to reference it in the savegame.
    ///
    /// This index is 1-based, so the first door has index 1.
    /// 
    /// Note that in Ambermoon Advanced, only 128 doors can be saved.
    /// </summary>
    [Range(1, GameDataRepository.MaxDoors)]
    public uint SaveIndex
    {
        get => _saveIndex.Get(this);
        set
        {
            ValueChecker.Check(value, 1, GameDataRepository.MaxDoors);
            SetField(_saveIndex, value);
        }
    }

    /// <summary>
    /// If given, the text with this index is displayed when the
    /// closed door is approached.
    /// </summary>
    [DefaultValue(null)]
    [Range(0, byte.MaxValue - 1)]
    public uint? ClosedDoorTextIndex
    {
        get => _closedDoorTextIndex.Get(this);
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 0, byte.MaxValue - 1);
            SetField(_closedDoorTextIndex, value);
        }
    }

    /// <summary>
    /// If given, the text with this index is displayed when the
    /// door is unlocked.
    /// </summary>
    [DefaultValue(null)]
    [Range(0, byte.MaxValue - 1)]
    public uint? UnlockTextIndex
    {
        get => _unlockTextIndex.Get(this);
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 0, byte.MaxValue - 1);
            SetField(_unlockTextIndex, value);
        }
    }

    /// <summary>
    /// Key index required to open the door.
    /// </summary>
    [DefaultValue(null)]
    [Range(1, GameDataRepository.MaxItems)]
    public uint? KeyIndex
    {
        get => _keyIndex.Get(this);
        set
        {
            if (value is not null)
            {
                ValueChecker.Check(value.Value, 1, GameDataRepository.MaxItems);
                LockedPercentage = 100; // Always set to 100 when requiring a key
            }
            SetField(_keyIndex, value);
        }
    }

    [Range(0, GameDataRepository.MaxEvents)]
    [DefaultValue(null)]
    public uint? UnlockFailEventIndex
    {
        get => _unlockFailEventIndex.Get(this);
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 0, GameDataRepository.MaxEvents);
            SetField(_unlockFailEventIndex, value);
        }
    }

    uint? IBranchEvent.BranchEventIndex => UnlockFailEventIndex;

    public override bool AllowInConversations => false;

    #endregion


    #region Constructors

    public DoorEventData()
    {
        Data[0] = (byte)EventType.Door;
        ClosedDoorTextIndex = null;
        UnlockTextIndex = null;
        KeyIndex = null;
        UnlockFailEventIndex = null;
        NextEventIndex = null;
    }

    internal DoorEventData(EventData data)
    {
        _lockedPercentage.Copy(data, this);
        _saveIndex.Copy(data, this);
        _closedDoorTextIndex.Copy(data, this);
        _unlockTextIndex.Copy(data, this);
        _keyIndex.Copy(data, this);
        _unlockFailEventIndex.Copy(data, this);
    }

    #endregion

}
