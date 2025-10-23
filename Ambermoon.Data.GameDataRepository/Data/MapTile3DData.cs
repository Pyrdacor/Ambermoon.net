using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data;

using Util;
using Serialization;

public enum MapTile3DType
{
    /// <summary>
    /// Free 3D tile.
    /// </summary>
    Free,
    /// <summary>
    /// 3D object.
    /// </summary>
    Object,
    /// <summary>
    /// 3D wall block.
    /// </summary>
    Wall,
    /// <summary>
    /// Invalid marker. Use for map borders or areas which are not accessible.
    /// </summary>
    Invalid
}

public class MapTile3DData : IData, IEquatable<MapTile3DData>, INotifyPropertyChanged
{

    #region Fields

    private uint? _mapEventId;
    private uint? _objectIndex;
    private uint? _wallIndex;
    private MapTile3DType _type = MapTile3DType.Free;

    #endregion


    #region Properties

    /// <summary>
    /// Index of the object, representing this tile.
    /// Will be null, if the tile is not an object.
    /// </summary>
    public uint? ObjectIndex
    {
        get => _objectIndex;
        private set => SetField(ref _objectIndex, value);
    }

    /// <summary>
    /// Index of the wall, representing this tile.
    /// Will be null, if the tile is not a wall.
    /// </summary>
    public uint? WallIndex
    {
        get => _wallIndex;
        private set => SetField(ref _wallIndex, value);
    }

    /// <summary>
    /// Index of the map event entry associated with this map tile.
    /// </summary>
    [Range(1, byte.MaxValue)]
    public uint? MapEventId
    {
        get => _mapEventId;
        set
        {
            if (value is not null)
                ValueChecker.Check(value.Value, 1, byte.MaxValue);
            SetField(ref _mapEventId, value);
        }
    }

    /// <summary>
    /// Type of this tile.
    /// </summary>
    public MapTile3DType Type
    {
        get => _type;
        private set => SetField(ref _type, value);
    }

    /// <summary>
    /// Determines if the map tile contains a map event.
    /// </summary>
    public bool HasMapEvent => MapEventId != null;

    /// <summary>
    /// Default empty 3D map tile.
    /// </summary>
    public static MapTile3DData Empty => new() { Type = MapTile3DType.Free };

    #endregion


    #region Methods

    /// <summary>
    /// Sets this map block to become a wall.
    /// </summary>
    /// <param name="wallIndex">The wall index (1 to 154).</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetWall([Range(GameDataRepository.MinWall3DIndex, GameDataRepository.MaxWall3DIndex)] uint wallIndex)
    {
        // Valid wall indices range from 1 to 154.
        // Technically they are stored as index 101 to 254.
        if (wallIndex is < GameDataRepository.MinWall3DIndex or > GameDataRepository.MaxWall3DIndex)
            throw new ArgumentOutOfRangeException(nameof(wallIndex), $"Wall indices must be in the range {GameDataRepository.MinWall3DIndex} to {GameDataRepository.MaxWall3DIndex}.");

        Type = MapTile3DType.Wall;
        WallIndex = wallIndex;
        ObjectIndex = null;
    }

    /// <summary>
    /// Sets this map block to become a 3D object.
    /// </summary>
    /// <param name="objectIndex">The object index (1 to 100).</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetObject([Range(GameDataRepository.MinObject3DIndex, GameDataRepository.MaxObject3DIndex)] uint objectIndex)
    {
        // Valid object indices range from 1 to 100.
        // Technically they are stored as index 1 to 100 as well.
        if (objectIndex is < GameDataRepository.MinObject3DIndex or > GameDataRepository.MaxObject3DIndex)
            throw new ArgumentOutOfRangeException(nameof(objectIndex), $"Object indices must be in the range {GameDataRepository.MinObject3DIndex} to {GameDataRepository.MaxObject3DIndex}.");

        Type = MapTile3DType.Object;
        ObjectIndex = objectIndex;
        WallIndex = null;
    }

    /// <summary>
    /// Sets this map block to become a free 3D tile.
    /// </summary>
    public void SetFree()
    {
        Type = MapTile3DType.Free;
        ObjectIndex = null;
        WallIndex = null;
    }

    /// <summary>
    /// Sets this map block to become an invalid 3D tile (filler tile).
    /// </summary>
    public void SetInvalid()
    {
        Type = MapTile3DType.Invalid;
        ObjectIndex = null;
        WallIndex = null;
    }

    /// <summary>
    /// If the tile is a 3D object, this method returns true and provides the object index.
    /// Otherwise, it returns false and provides null.
    /// </summary>
    /// <param name="objectIndex"></param>
    /// <returns></returns>
    public bool TryGetObject(out uint? objectIndex)
    {
        if (Type == MapTile3DType.Object)
        {
            objectIndex = ObjectIndex;
            return true;
        }
        else
        {
            objectIndex = null;
            return false;
        }
    }

    /// <summary>
    /// If the tile is a 3D wall, this method returns true and provides the wall index.
    /// Otherwise, it returns false and provides null.
    /// </summary>
    /// <param name="wallIndex"></param>
    /// <returns></returns>
    public bool TryGetWall(out uint? wallIndex)
    {
        if (Type == MapTile3DType.Wall)
        {
            wallIndex = WallIndex;
            return true;
        }
        else
        {
            wallIndex = null;
            return false;
        }
    }

    #endregion


    #region Serialization

    public void Serialize(IDataWriter dataWriter, int majorVersion, bool advanced)
    {
        byte index = Type switch
        {
            MapTile3DType.Free => 0,
            MapTile3DType.Object => (byte)ObjectIndex!,
            MapTile3DType.Wall => (byte)(GameDataRepository.MaxObject3DIndex + WallIndex!),
            _ => 255
        };
        dataWriter.Write(index);
        dataWriter.Write((byte)(MapEventId ?? 0));
    }

    public static IData Deserialize(IDataReader dataReader, int majorVersion, bool advanced)
    {
        uint index = dataReader.ReadByte();

        uint mapEventId = dataReader.ReadByte();
        var mapBlock = new MapTile3DData() { MapEventId = mapEventId == 0 ? null : mapEventId };

        switch (index)
        {
            case 0:
                mapBlock.SetFree();
                break;
            case <= GameDataRepository.MaxObject3DIndex:
                mapBlock.SetObject(index);
                break;
            case 255:
                mapBlock.SetInvalid();
                break;
            default:
                mapBlock.SetWall(index - GameDataRepository.MaxObject3DIndex);
                break;
        }

        return mapBlock;
    }

    #endregion


    #region Equality

    public bool Equals(MapTile3DData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return
            WallIndex == other.WallIndex &&
            ObjectIndex == other.ObjectIndex &&
            Type == other.Type &&
            MapEventId == other.MapEventId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MapTile3DData)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MapEventId, ObjectIndex ?? WallIndex ?? 0, (int)Type);
    }

    public static bool operator ==(MapTile3DData? left, MapTile3DData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MapTile3DData? left, MapTile3DData? right)
    {
        return !Equals(left, right);
    }

    #endregion


    #region Cloning

    public MapTile3DData Copy()
    {
        return new()
        {
            WallIndex = WallIndex,
            ObjectIndex = ObjectIndex,
            Type = Type,
            MapEventId = MapEventId
        };
    }

    public object Clone() => Copy();

    #endregion


    #region Property Changes

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

}
