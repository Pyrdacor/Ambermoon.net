using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Util;
using static RewardEvent;

/// <summary>
/// Grants some kind of reward when triggered.
/// </summary>
public class RewardEventData : EventData
{

    #region Fields

    private readonly EnumEventDataProperty<RewardType> _rewardType = new(1);
    private readonly EnumEventDataProperty<RewardOperation> _rewardOperation = new(2);
    private readonly ByteEventDataProperty _random = new(3);
    private readonly EnumEventDataProperty<RewardTarget> _rewardTarget = new(4);
    // Byte 5 is unused
    private readonly NullableEventDataProperty<uint> _rewardSubType = new(new WordEventDataProperty(6), 0);
    private readonly WordEventDataProperty _value = new(8);

    #endregion


    #region Properties

    public RewardType RewardType
    {
        get => _rewardType.Get(this);
        set => SetField(_rewardType, value);
    }

    public RewardOperation RewardOperation
    {
        get => _rewardOperation.Get(this);
        set => SetField(_rewardOperation, value);
    }

    /// <summary>
    /// If true, the reward value is random between 1 and the given value.
    /// </summary>
    public bool Random
    {
        get => _random.Get(this) != 0;
        set => SetField(_random, (byte)(value ? 1 : 0));
    }

    public RewardTarget RewardTarget
    {
        get => _rewardTarget.Get(this);
        set => SetField(_rewardTarget, value);
    }

    /// <summary>
    /// For specific reward types, this field specifies
    /// a sub type. For example if the reward grants
    /// an increase of an attribute, this field specifies
    /// which attribute to increase.
    /// 
    /// For other reward types, this field might be unused.
    /// </summary>
    [DefaultValue(null)]
    [Range(0, ushort.MaxValue)]
    public uint? RewardSubType
    {
        get => _rewardSubType.Get(this);
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 0, ushort.MaxValue);
            SetField(_rewardSubType, value);
        }
    }

    /// <summary>
    /// Value of the reward.
    /// </summary>
    [Range(0, ushort.MaxValue)]
    public uint Value
    {
        get => _value.Get(this);
        set
        {
            ValueChecker.Check(value, 0, ushort.MaxValue);
            SetField(_value, value);
        }
    }

    #endregion


    #region Constructors

    public RewardEventData()
    {
        Data[0] = (byte)EventType.Reward;
        RewardSubType = null;
        NextEventIndex = null;
    }

    internal RewardEventData(EventData data)
    {
        _rewardType.Copy(data, this);
        _rewardOperation.Copy(data, this);
        _random.Copy(data, this);
        _rewardTarget.Copy(data, this);
        _rewardSubType.Copy(data, this);
        _value.Copy(data, this);
    }

    #endregion

}
