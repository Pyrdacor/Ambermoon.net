using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Ambermoon.Data.Enumerations;
using Util;

/// <summary>
/// Changes an active spell of the party.
///
/// Note: In the original this is only a "remove active spell" event.
/// In Ambermoon Advanced you have more options.
/// </summary>
public class ChangeActiveSpellEventData : EventData
{

    #region Fields

    private readonly NullableEventDataProperty<ActiveSpellType> _activeSpell = new(new EnumEventDataProperty<ActiveSpellType>(1), 0);
    private readonly ByteEventDataProperty _addSpell = new(2);
    // Byte 3 is unused
    private readonly WordEventDataProperty _spellValue = new(4);
    private readonly WordEventDataProperty _duration = new(6);
    // Bytes 8 to 9 are unused

    #endregion


    #region Properties

    /// <summary>
    /// Specifies which active spell to change.
    ///
    /// If null, all spells are changed. This
    /// is especially useful for removing them.
    /// </summary>
    public ActiveSpellType? ActiveSpell
    {
        get => _activeSpell.Get(this);
        set => SetField(_activeSpell, value);
    }

    public bool AllSpells => ActiveSpell is null;

    /// <summary>
    /// If true, the given spells are added.
    /// Otherwise, they are removed.
    ///
    /// Advanced only. In original, this is always false.
    /// </summary>
    [AdvancedOnly]
    public bool AddSpell
    {
        get => _addSpell.Get(this) != 0;
        set => SetField(_addSpell, (byte)(value ? 1 : 0));
    }

    /// <summary>
    /// This is only used for added spells.
    /// It specifies the value of the spell.
    /// Like the light level or attack bonus.
    ///
    /// Advanced only.
    /// </summary>
    [Range(0, 100)]
    [AdvancedOnly]
    public uint SpellValue
    {
        get => _spellValue.Get(this);
        set
        {
            ValueChecker.Check(value, 0, 100);
            SetField(_spellValue, value);
        }
    }

    /// <summary>
    /// This is only used for added spells.
    /// It specifies the added duration of
    /// the spell in 5 minute chunks.
    /// The maximum of 288 is exactly 24 hours.
    ///
    /// Advanced only.
    /// </summary>
    [Range(0, 288)]
    [AdvancedOnly]
    public uint Duration
    {
        get => _duration.Get(this);
        set
        {
            ValueChecker.Check(value, 0, 288);
            SetField(_duration, value);
        }
    }

    #endregion


    #region Constructors

    public ChangeActiveSpellEventData()
    {
        Data[0] = (byte)EventType.ChangeBuffs;
        NextEventIndex = null;
    }

    internal ChangeActiveSpellEventData(EventData data)
    {
        _activeSpell.Copy(data, this);
        _addSpell.Copy(data, this);
        _spellValue.Copy(data, this);
        _duration.Copy(data, this);
    }

    #endregion

}
