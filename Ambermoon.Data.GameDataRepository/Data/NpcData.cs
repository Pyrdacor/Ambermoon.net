using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data;

using Serialization;
using Util;

public sealed class NpcData : CharacterData, IConversationCharacter, IIndexedData, IEquatable<NpcData>, IImageProvidingData
{

    #region Fields

    private uint _age = 1;
    private Language _spokenLanguages = Language.None;
    private ExtendedLanguage _additionalSpokenLanguages = ExtendedLanguage.None;
    private uint _graphicIndex = 0;
    private uint _lookAtCharTextIndex = 0;

    #endregion


    #region Properties

    public override CharacterType Type => CharacterType.NPC;

    [Range(1, ushort.MaxValue)]
    public uint Age
    {
        get => _age;
        set
        {
            ValueChecker.Check(value, 1, ushort.MaxValue);
            SetField(ref _age, value);
        }
    }

    public Language SpokenLanguages
    {
        get => _spokenLanguages;
        set => SetField(ref _spokenLanguages, value);
    }

    /// <summary>
    /// Advanced only (episode 4+).
    /// </summary>
    public ExtendedLanguage AdditionalSpokenLanguages
    {
        get => _additionalSpokenLanguages;
        set => SetField(ref _additionalSpokenLanguages, value);
    }

    [Range(0, 255)]
    public uint GraphicIndex
    {
        get => _graphicIndex;
        set
        {
            ValueChecker.Check(value, 0, 255);
            SetField(ref _graphicIndex, value);
        }
    }

    [Range(0, ushort.MaxValue)]
    public uint LookAtCharTextIndex
    {
        get => _lookAtCharTextIndex;
        set
        {
            ValueChecker.Check(value, 0, ushort.MaxValue);
            SetField(ref _lookAtCharTextIndex, value);
        }
    }

    #endregion


    #region Constructors

    public NpcData()
    {
    }

    #endregion


    #region Serialization

    public void Serialize(IDataWriter dataWriter, int majorVersion, bool advanced)
    {
        void WriteFillBytes(int count)
        {
            for (int i = 0; i < count; i++)
                dataWriter.Write(0);
        }

        dataWriter.Write((byte)Type);
        dataWriter.Write((byte)Gender);
        dataWriter.Write((byte)Race);
        dataWriter.Write((byte)Class);
        WriteFillBytes(1);
        dataWriter.Write((byte)Level);
        WriteFillBytes(2);
        dataWriter.Write((byte)SpokenLanguages);
        WriteFillBytes(1);
        dataWriter.Write((byte)GraphicIndex);
        WriteFillBytes(2);
        if (advanced && majorVersion >= 4)
        {
            dataWriter.Write((byte)AdditionalSpokenLanguages);
        }
        else
        {
            WriteFillBytes(1);
        }
        WriteFillBytes(92);
        dataWriter.Write((ushort)Age);
        WriteFillBytes(128);
        dataWriter.Write((ushort)LookAtCharTextIndex);
        WriteFillBytes(36);
        string name = Name;
        if (name.Length > 15)
            name = name[..15];
        dataWriter.WriteWithoutLength(name.PadRight(16, '\0'));

        // TODO: Events
    }

    public static IIndexedData Deserialize(IDataReader dataReader, uint index, int majorVersion, bool advanced)
    {
        var npcData = (NpcData)Deserialize(dataReader, majorVersion, advanced);
        (npcData as IMutableIndex).Index = index;
        return npcData;
    }

    public static IData Deserialize(IDataReader dataReader, int majorVersion, bool advanced)
    {
        if (dataReader.ReadByte() != (byte)CharacterType.NPC)
            throw new InvalidDataException("The given data is no valid NPC data.");

        void SkipBytes(int amount) => dataReader.Position += amount;

        var npcData = new NpcData();

        npcData.Gender = (Gender)dataReader.ReadByte();
        npcData.Race = (Race)dataReader.ReadByte();
        npcData.Class = (Class)dataReader.ReadByte();
        SkipBytes(1);
        npcData.Level = dataReader.ReadByte();
        SkipBytes(2);
        npcData.SpokenLanguages = (Language)dataReader.ReadByte();
        SkipBytes(1);
        npcData.GraphicIndex = dataReader.ReadByte();
        SkipBytes(2);
        if (advanced && majorVersion >= 4)
        {
            npcData.AdditionalSpokenLanguages = (ExtendedLanguage)dataReader.ReadByte();
        }
        else
        {
            SkipBytes(1);
        }
        SkipBytes(92);
        npcData.Age = dataReader.ReadWord();
        SkipBytes(128);
        npcData.LookAtCharTextIndex = dataReader.ReadWord();
        SkipBytes(36);
        npcData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

        // TODO: Events

        return npcData;
    }

    #endregion


    #region Equality

    public bool Equals(NpcData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) &&
                _age == other._age &&
                _spokenLanguages == other._spokenLanguages &&
                _additionalSpokenLanguages == other._additionalSpokenLanguages &&
                _graphicIndex == other._graphicIndex &&
                _lookAtCharTextIndex == other._lookAtCharTextIndex;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NpcData)obj);
    }

    public static bool operator ==(NpcData? left, NpcData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NpcData? left, NpcData? right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode() => base.GetHashCode();

    #endregion


    #region Cloning

    public NpcData Copy()
    {
        NpcData copy = new()
        {
            Gender = Gender,
            Race = Race,
            Class = Class,
            Level = Level,
            SpokenLanguages = SpokenLanguages,
            GraphicIndex = GraphicIndex,
            AdditionalSpokenLanguages = AdditionalSpokenLanguages,
            Age = Age,
            LookAtCharTextIndex = LookAtCharTextIndex,
            Name = Name
        };

        // TODO: Events

        (copy as IMutableIndex).Index = Index;

        return copy;
    }

    public override object Clone() => Copy();

    #endregion
}
