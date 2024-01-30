using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.GameDataRepository.Collections;
using Ambermoon.Data.GameDataRepository.Data.Events;
using Ambermoon.Data.GameDataRepository.Enumerations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;
    using Util;

    public enum Tile2DType
    {
        Normal,
        ChairNorth,
        ChairEast,
        ChairSouth,
        ChairWest,
        Bed,
        Water,
    }

    /// <summary>
    /// This should only be used for foreground tiles.
    /// It controls the render order of the tile and the player.
    /// </summary>
    public enum Tile2DRenderOrder
    {
        /// <summary>
        /// Normal render order.
        ///
        /// Background tiles are drawn behind the player.
        /// Foreground tiles are drawn above the lower half of the player but below the upper half.
        /// </summary>
        Normal,
        /// <summary>
        /// The tile is always drawn behind the player.
        /// </summary>
        AlwaysBelowPlayer,
        /// <summary>
        /// The tile is always drawn above the player.
        /// This is only true for foreground tiles though!
        /// Background tiles will still be drawn behind the player.
        /// </summary>
        AlwaysAbovePlayer
    }

    public sealed class Tile2DIconData : IMutableIndex, IIndexedData, IEquatable<Tile2DIconData>,
        INotifyPropertyChanged
    {
        #region Fields

        private Tile2DType _type;
        private Tile2DRenderOrder _renderOrder;
        private uint _allowedCollisionClasses;
        private bool _waveAnimation;
        private bool _randomAnimation;
        private bool _autoPoison;
        private bool _hidePlayer;
        private uint _graphicIndex;
        private uint _combatBackgroundIndex;
        private uint _colorIndex;
        private uint _numberOfFrames;
        private bool _useBackgroundTileFlags;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// Type of this tile.
        /// </summary>
        public Tile2DType Type
        {
            get => _type;
            set
            {
                SetField(ref _type, value);
                if (value == Tile2DType.Water)
                    AllowedCollisionClasses |= (uint)Tileset.TileFlags.AllowMovementSwim;
                else
                    AllowedCollisionClasses &= ~(uint)Tileset.TileFlags.AllowMovementSwim;
            }
        }

        /// <summary>
        /// This should only be used for foreground tiles.
        /// It controls the render order of the tile and the player.
        /// </summary>
        public Tile2DRenderOrder RenderOrder
        {
            get => _renderOrder;
            set => SetField(ref _renderOrder, value);
        }


        /// <summary>
        /// The collision classes this tile allows passing.
        ///
        /// Note that swimming is handled by the tile type
        /// and is ignored here.
        /// </summary>
        [Range(0, (1 << 15) - 1)]
        public uint AllowedCollisionClasses
        {
            get => _allowedCollisionClasses;
            set
            {
                ValueChecker.Check(value, 0, (1 << 15) - 1);

                if (Type == Tile2DType.Water)
                    value |= (uint)Tileset.TileFlags.AllowMovementSwim;
                else
                    value &= ~(uint)Tileset.TileFlags.AllowMovementSwim;

                SetField(ref _allowedCollisionClasses, value);
            }
        }

        /// <summary>
        /// Travel types this tile allows passing.
        ///
        /// Useful for world maps. Otherwise use <see cref="AllowedCollisionClasses"/>.
        /// </summary>
        public TravelType AllowedTravelTypes
        {
            get => (TravelType)AllowedCollisionClasses;
            set
            {
                if (value.HasFlag(Tileset.TileFlags.AllowMovementSwim))
                    Type = Tile2DType.Water;
                else if (Type == Tile2DType.Water)
                    Type = Tile2DType.Normal;

                AllowedCollisionClasses = (uint)value;
            }
        }

        /// <summary>
        /// Graphic index of the tile. If the tile uses an animation,
        /// this is the index to the first frame of the animation.
        ///
        /// The index points into the tileset graphic data for the same
        /// tileset. So for example tileset file 8 accesses tileset graphic
        /// data in the same sub-file in XIcon_gfx.amb.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public uint GraphicIndex
        {
            get => _graphicIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _graphicIndex, value);
            }
        }

        /// <summary>
        /// Color index inside the map palette to show on the minimap.
        /// </summary>
        [Range(0, GameDataRepository.PaletteSize - 1)]
        public uint ColorIndex
        {
            get => _colorIndex;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.PaletteSize - 1);
                SetField(ref _colorIndex, value);
            }
        }

        /// <summary>
        /// Number of frames for this tile animation.
        /// If 1, this is a normal tile without animation.
        ///
        /// Note that for animations, always those
        /// graphics are used which follow the first frame
        /// graphic provided by <see cref="GraphicIndex"/>.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint NumberOfFrames
        {
            get => _numberOfFrames;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _numberOfFrames, value);
            }
        }

        /// <summary>
        /// Normally animations are cyclic. So if the last frame
        /// is reached, it starts again at the first frame.
        /// 
        /// If this is active the animation instead will decrease
        /// the frames one by one after reaching the last frame.
        /// So it will be a forth and back frame iteration.
        /// 
        /// 0 -> 1 -> 2 -> 1 -> 0 -> 1 -> 2 -> 1 -> ...
        /// 
        /// Instead of:
        /// 
        /// 0 -> 1 -> 2 -> 0 -> 1 -> 2 -> 0 -> 1 -> ...
        /// </summary>
        public bool WaveAnimation
        {
            get => _waveAnimation;
            set => SetField(ref _waveAnimation, value);
        }

        /// <summary>
        /// Normally animations will run continuously and
        /// start at the first frame when the map is loaded.
        /// 
        /// If this is active, animations start randomly
        /// dependent on some random value and will pause
        /// after the full animation to start at the next
        /// random occasion.
        /// 
        /// NOTE: Currently this is different in the remake
        /// but should be fixed soon. There it just randomly
        /// picks the start frame on map load and then run
        /// the animation continuously.
        /// </summary>
        public bool RandomAnimation
        {
            get => _randomAnimation;
            set => SetField(ref _randomAnimation, value);
        }

        /// <summary>
        /// If set, the player will be poisoned when
        /// entering the tile. But only at the first
        /// frame of the animation. Often used in
        /// combination with <see cref="RandomAnimation"/>.
        /// </summary>
        public bool AutoPoison
        {
            get => _autoPoison;
            set => SetField(ref _autoPoison, value);
        }

        /// <summary>
        /// If set, the player becomes invisible
        /// when entering the tile. This is often
        /// used for doors.
        /// </summary>
        public bool HidePlayer
        {
            get => _hidePlayer;
            set => SetField(ref _hidePlayer, value);
        }

        /// <summary>
        /// This is only considered for foreground tiles. If set, they
        /// will use the flags of the underlying tile. This is
        /// often used to inherit the blocking modes of the tile.
        /// </summary>
        public bool UseBackgroundTileFlags
        {
            get => _useBackgroundTileFlags;
            set => SetField(ref _useBackgroundTileFlags, value);
        }

        /// <summary>
        /// Combat background index which is used when
        /// starting a fight on the tile. Only used for
        /// <see cref="StartBattleEventData"/> though as
        /// map characters have their own combat background index.
        /// </summary>
        [Range(0, GameDataRepository.CombatBackgroundCount - 1)]
        public uint CombatBackgroundIndex
        {
            get => _combatBackgroundIndex;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.CombatBackgroundCount - 1);
                SetField(ref _combatBackgroundIndex, value);
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            uint tileFlags = (AllowedCollisionClasses << 8) & 0x7fff00;

            if (tileFlags == 0) // no collision classes allowed?
                tileFlags = 0x80; // shortcut (= block all movement)

            tileFlags |= (CombatBackgroundIndex & 0xf) << 28;
            if (WaveAnimation)
                tileFlags |= (uint)Tile2DFlags.WaveAnimation;
            if (RandomAnimation)
                tileFlags |= (uint)Tile2DFlags.RandomAnimationStart;
            if (AutoPoison)
                tileFlags |= (uint)Tile2DFlags.AutoPoison;
            if (HidePlayer)
                tileFlags |= (uint)Tile2DFlags.HidePlayer;
            if (UseBackgroundTileFlags)
                tileFlags |= (uint)Tile2DFlags.UseBackgroundTileFlags;
            switch (RenderOrder)
            {
                case Tile2DRenderOrder.AlwaysAbovePlayer:
                    tileFlags |= (uint)Tile2DFlags.CustomRenderOrder;
                    break;
                case Tile2DRenderOrder.AlwaysBelowPlayer:
                    tileFlags |= (uint)(Tile2DFlags.CustomRenderOrder | Tile2DFlags.CustomRenderOrderMode);
                    break;
            }
            switch (Type)
            {
                case Tile2DType.Water:
                    tileFlags |= (uint)Tile2DFlags.AllowMovementSwim;
                    break;
                case Tile2DType.ChairNorth:
                case Tile2DType.ChairEast:
                case Tile2DType.ChairSouth:
                case Tile2DType.ChairWest:
                case Tile2DType.Bed:
                {
                    var mask = (uint)Type; // 1 to 5
                    mask <<= 23; // 23 bits left
                    tileFlags |= mask;
                    goto default;
                }
                default:
                    tileFlags &= ~(uint)Tile2DFlags.AllowMovementSwim;
                    break;
            }

            dataWriter.Write(tileFlags);
            dataWriter.Write((ushort)GraphicIndex);
            dataWriter.Write((byte)NumberOfFrames);
            dataWriter.Write((byte)ColorIndex);
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var tile2DIconData = (Tile2DIconData)Deserialize(dataReader, advanced);
            (tile2DIconData as IMutableIndex).Index = index;
            return tile2DIconData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var tile2DIconData = new Tile2DIconData();

            uint flags = dataReader.ReadDword();
            tile2DIconData.GraphicIndex = dataReader.ReadWord();
            tile2DIconData.NumberOfFrames = dataReader.ReadByte();
            tile2DIconData.ColorIndex = dataReader.ReadByte();
            tile2DIconData.AllowedCollisionClasses = (flags >> 8) & 0x7fff;
            tile2DIconData.CombatBackgroundIndex = (flags >> 28) & 0xf;
            var tileFlags = (Tile2DFlags)flags;
            tile2DIconData.WaveAnimation = tileFlags.HasFlag(Tile2DFlags.WaveAnimation);
            tile2DIconData.RandomAnimation = tileFlags.HasFlag(Tile2DFlags.RandomAnimationStart);
            tile2DIconData.AutoPoison = tileFlags.HasFlag(Tile2DFlags.AutoPoison);
            tile2DIconData.HidePlayer = tileFlags.HasFlag(Tile2DFlags.HidePlayer);
            tile2DIconData.UseBackgroundTileFlags = tileFlags.HasFlag(Tile2DFlags.UseBackgroundTileFlags);
            if (tileFlags.HasFlag(Tile2DFlags.CustomRenderOrder))
            {
                tile2DIconData.RenderOrder = tileFlags.HasFlag(Tile2DFlags.CustomRenderOrderMode)
                    ? Tile2DRenderOrder.AlwaysBelowPlayer
                    : Tile2DRenderOrder.AlwaysAbovePlayer;
            }
            else
                tile2DIconData.RenderOrder = Tile2DRenderOrder.Normal;
            uint sitSleepValue = (flags & (uint)Tile2DFlags.SitSleepMask) >> 23;
            tile2DIconData.Type = sitSleepValue switch
            {
                >= 1 and <= 5 => (Tile2DType)sitSleepValue,
                _ => tileFlags.HasFlag(Tile2DFlags.AllowMovementSwim) ? Tile2DType.Water : Tile2DType.Normal
            };

            return tile2DIconData;
        }

        #endregion


        #region Equality

        public bool Equals(Tile2DIconData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return WaveAnimation == other.WaveAnimation &&
                   RandomAnimation == other.RandomAnimation &&
                   AutoPoison == other.AutoPoison &&
                   HidePlayer == other.HidePlayer &&
                   UseBackgroundTileFlags == other.UseBackgroundTileFlags &&
                   RenderOrder == other.RenderOrder &&
                   Type == other.Type &&
                   AllowedCollisionClasses == other.AllowedCollisionClasses &&
                   GraphicIndex == other.GraphicIndex &&
                   CombatBackgroundIndex == other.CombatBackgroundIndex &&
                   ColorIndex == other.ColorIndex &&
                   NumberOfFrames == other.NumberOfFrames;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Tile2DIconData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(Tile2DIconData? left, Tile2DIconData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tile2DIconData? left, Tile2DIconData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public Tile2DIconData Copy()
        {
            var copy = new Tile2DIconData()
            {
                AllowedCollisionClasses = AllowedCollisionClasses,
                AutoPoison = AutoPoison,
                ColorIndex = ColorIndex,
                CombatBackgroundIndex = CombatBackgroundIndex,
                GraphicIndex = GraphicIndex,
                HidePlayer = HidePlayer,
                NumberOfFrames = NumberOfFrames,
                RandomAnimation = RandomAnimation,
                RenderOrder = RenderOrder,
                Type = Type,
                UseBackgroundTileFlags = UseBackgroundTileFlags,
                WaveAnimation = WaveAnimation
            };

            (copy as IMutableIndex).Index = Index;

            return copy;
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

    public sealed class Tileset2DData : IMutableIndex, IIndexedData, IEquatable<Tileset2DData>, INotifyPropertyChanged
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public DictionaryList<Tile2DIconData> Icons { get; private set; } = new();

        #endregion


        #region Constructors

        public Tileset2DData()
        {
            Icons.ItemChanged += IconChanged;
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((ushort)Icons.Count);

            foreach (var icon in Icons)
            {
                icon.Serialize(dataWriter, advanced);
            }
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var tileset2DData = (Tileset2DData)Deserialize(dataReader, advanced);
            (tileset2DData as IMutableIndex).Index = index;
            return tileset2DData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var tileset2DData = new Tileset2DData();

            int iconCount = dataReader.ReadWord();

            tileset2DData.Icons.ItemChanged -= tileset2DData.IconChanged;
            tileset2DData.Icons = new DictionaryList<Tile2DIconData>(iconCount);

            for (int i = 0; i < iconCount; ++i)
            {
                tileset2DData.Icons.Add((Tile2DIconData)Tile2DIconData.Deserialize(dataReader, (uint)i + 1, advanced));
            }

            tileset2DData.Icons.ItemChanged += tileset2DData.IconChanged;

            return tileset2DData;
        }

        #endregion


        #region Equality

        public bool Equals(Tileset2DData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Icons.Equals(other.Icons);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Tileset2DData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(Tileset2DData? left, Tileset2DData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tileset2DData? left, Tileset2DData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public Tileset2DData Copy()
        {
            return new()
            {
                Icons = Icons.Select(icon => icon.Copy()).ToDictionaryList()
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

        private void IconChanged(uint index)
        {
            OnPropertyChanged(nameof(Icons));
        }

        #endregion

    }
}
