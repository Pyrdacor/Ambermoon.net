using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events;

using Util;

/// <summary>
/// Shows the riddlemouth screen when triggered.
/// </summary>
public class RiddlemouthEventData : EventData
{

    #region Fields

    private readonly ByteEventDataProperty _riddleTextIndex = new(1);
    private readonly ByteEventDataProperty _solvedTextIndex = new(2);
    // Bytes 3 to 5 are unused
    private readonly WordEventDataProperty _correctAnswerKeyword1 = new(6);
    private readonly WordEventDataProperty _correctAnswerKeyword2 = new(8);

    #endregion


    #region Properties

    /// <summary>
    /// Text index of the riddle text shown to the player.
    /// </summary>
    [Range(0, byte.MaxValue)]
    public uint RiddleTextIndex
    {
        get => _riddleTextIndex.Get(this);
        set
        {
            ValueChecker.Check(value, 0, byte.MaxValue);
            SetField(_riddleTextIndex, value);
        }
    }

    /// <summary>
    /// Text index of the text shown when the riddle is solved.
    /// </summary>
    [Range(0, byte.MaxValue)]
    public uint SolvedTextIndex
    {
        get => _solvedTextIndex.Get(this);
        set
        {
            ValueChecker.Check(value, 0, byte.MaxValue);
            SetField(_solvedTextIndex, value);
        }
    }

    /// <summary>
    /// Index of the dictionary keyword which is a
    /// valid answer to the riddle.
    /// </summary>
    [Range(0, ushort.MaxValue)]
    public uint CorrectAnswerKeyword1
    {
        get => _correctAnswerKeyword1.Get(this);
        set
        {
            ValueChecker.Check(value, 0, ushort.MaxValue);
            SetField(_correctAnswerKeyword1, value);
        }
    }

    /// <summary>
    /// Index of the dictionary keyword which is a
    /// valid answer to the riddle.
    /// 
    /// Note: If only one answer is desired, set
    /// both CorrectAnswerKeyword1 and CorrectAnswerKeyword2
    /// to the same value.
    /// </summary>
    [Range(0, ushort.MaxValue)]
    public uint CorrectAnswerKeyword2
    {
        get => _correctAnswerKeyword2.Get(this);
        set
        {
            ValueChecker.Check(value, 0, ushort.MaxValue);
            SetField(_correctAnswerKeyword2, value);
        }
    }

    public override bool AllowInConversations => false;

    public override bool AllowOn2DMaps => false;

    #endregion


    #region Constructors

    public RiddlemouthEventData()
    {
        Data[0] = (byte)EventType.Riddlemouth;
        NextEventIndex = null;
    }

    internal RiddlemouthEventData(EventData data)
    {
        _riddleTextIndex.Copy(data, this);
        _solvedTextIndex.Copy(data, this);
        _correctAnswerKeyword1.Copy(data, this);
        _correctAnswerKeyword2.Copy(data, this);
    }

    #endregion

}
