using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Util;

/// <summary>
/// This event can be used to show a simple text popup on the map.
/// But it is also used for large event picture windows like
/// the intro sequence with your grandfather or the event where
/// Valdyn enters your world.
/// </summary>
public class MapTextEventData : EventData
{

    #region Fields

    private readonly NullableEventDataProperty<uint> _eventPictureIndex = new(new ByteEventDataProperty(1), 0xff);
    private readonly EnumEventDataProperty<EventTrigger> _trigger = new(2);
    private readonly ByteEventDataProperty _triggerWhenBlind = new(3);
    // Byte 4 is unused
    private readonly ByteEventDataProperty _textIndex = new(5);
    // Bytes 6 to 9 are unused

    #endregion


    #region Properties

    /// <summary>
    /// If given, a large picture is shown when the event is triggered.
    /// This is used for the initial sequence where your grandfather is
    /// lying in bed, when you approach the teleporter to Valdyn's world
    /// and whenever you travel with the air ship.
    ///
    /// If this is null (default), this event just shows a normal text popup.
    /// </summary>
    [Range(0, byte.MaxValue - 1)]
    [DefaultValue(null)]
    public uint? EventPictureIndex
    {
        get => _eventPictureIndex.Get(this);
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 0, byte.MaxValue - 1);
            SetField(_eventPictureIndex, value);
        }
    }

    [DefaultValue(EventTrigger.Always)]
    public EventTrigger Trigger
    {
        get => _trigger.Get(this);
        set => SetField(_trigger, value);
    }

    [DefaultValue(true)]
    public bool TriggerWhenBlind
    {
        get => _triggerWhenBlind.Get(this) != 0;
        set => SetField(_triggerWhenBlind, (byte)(value ? 1 : 0));
    }

    [Range(0, byte.MaxValue)]
    public uint TextIndex
    {
        get => _textIndex.Get(this);
        set
        {
            ValueChecker.Check(value, 0, byte.MaxValue);
            SetField(_textIndex, value);
        }
    }

    public override bool AllowInConversations => false;

    #endregion


    #region Constructors

    public MapTextEventData()
    {
        Data[0] = (byte)EventType.MapText;
        EventPictureIndex = null;
        Trigger = EventTrigger.Always;
        TriggerWhenBlind = true;
        NextEventIndex = null;
    }

    internal MapTextEventData(EventData data)
    {
        _eventPictureIndex.Copy(data, this);
        _trigger.Copy(data, this);
        _triggerWhenBlind.Copy(data, this);
        _textIndex.Copy(data, this);
    }

    #endregion

}
