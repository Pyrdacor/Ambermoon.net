using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ambermoon.Data.GameDataRepository.Enumerations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Ambermoon.Data.Enumerations;
    using Collections;
    using Serialization;
    using System.ComponentModel.DataAnnotations;
    using Util;
    using static Ambermoon.Data.Tileset;

    /// <summary>
    /// Represents an overlay which can be drawn
    /// on walls. A wall can have up to 255 overlays.
    /// </summary>
    public sealed class LabyrinthOverlayData : IIndexedData, IMutableIndex, IEquatable<LabyrinthOverlayData>, INotifyPropertyChanged
    {

        #region Fields

        private bool _blend;
        private uint _textureIndex;
        private uint _x;
        private uint _y;
        private uint _width;
        private uint _height;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// If false, the overlay is drawn on top of the wall
        /// and will just replace the wall texture pixels.
        ///
        /// If true, transparent pixels of the overlay will
        /// allow the wall texture to be visible.
        /// </summary>
        public bool Blend
        {
            get => _blend;
            set => SetField(ref _blend, value);
        }

        /// <summary>
        /// Texture file index inside XOverlay3D.amb.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint TextureIndex
        {
            get => _textureIndex;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _textureIndex, value);
            }
        }

        /// <summary>
        /// Horizontal location of the overlay in pixels.
        /// </summary>
        [Range(0, 128 - 16)]
        public uint X
        {
            get => _x;
            set
            {
                ValueChecker.Check(value, 0, 128 - 16);
                SetField(ref _x, value);
            }
        }

        /// <summary>
        /// Vertical location of the overlay in pixels.
        /// </summary>
        [Range(0, 80 - 1)]
        public uint Y
        {
            get => _y;
            set
            {
                ValueChecker.Check(value, 0, 80 - 1);
                SetField(ref _y, value);
            }
        }

        /// <summary>
        /// Width of the overlay in pixels.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint Width
        {
            get => _width;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _width, value);
            }
        }

        /// <summary>
        /// Height of the overlay in pixels.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint Height
        {
            get => _height;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _height, value);
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Overlay data
            dataWriter.Write((byte)(Blend ? 1 : 0));
            dataWriter.Write((byte)TextureIndex);
            dataWriter.Write((byte)X);
            dataWriter.Write((byte)Y);
            dataWriter.Write((byte)Width);
            dataWriter.Write((byte)Height);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var overlayData = new LabyrinthOverlayData();

            overlayData.Blend = dataReader.ReadByte() != 0;
            overlayData.TextureIndex = dataReader.ReadByte();
            overlayData.X = dataReader.ReadByte();
            overlayData.Y = dataReader.ReadByte();
            overlayData.Width = dataReader.ReadByte();
            overlayData.Height = dataReader.ReadByte();

            return overlayData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var overlayData = (LabyrinthOverlayData)Deserialize(dataReader, advanced);
            (overlayData as IMutableIndex).Index = index;
            return overlayData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthOverlayData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   Blend == other.Blend &&
                   TextureIndex == other.TextureIndex &&
                   X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthOverlayData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthOverlayData? left, LabyrinthOverlayData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthOverlayData? left, LabyrinthOverlayData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthOverlayData Copy()
        {
            var copy = new LabyrinthOverlayData();

            copy.Blend = Blend;
            copy.TextureIndex = TextureIndex;
            copy.X = X;
            copy.Y = Y;
            copy.Width = Width;
            copy.Height = Height;
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

    /// <summary>
    /// Represents a 3D wall in a labyrinth.
    /// </summary>
    public sealed class LabyrinthWallData : IIndexedData, IMutableIndex, IEquatable<LabyrinthWallData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _textureIndex;
        private AutomapType _automapType;
        private uint _colorIndex;
        private uint _allowedCollisionClasses;
        private uint _combatBackgroundIndex;
        private bool _blockSight;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// Texture file index inside XWall3D.amb.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint TextureIndex
        {
            get => _textureIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _textureIndex, value);
            }
        }

        /// <summary>
        /// Icon type to show on the automap (dungeon map) for the wall.
        /// </summary>
        public AutomapType AutomapType
        {
            get => _automapType;
            set => SetField(ref _automapType, value);
        }

        /// <summary>
        /// Color index inside the map palette to show on the minimap (not dungeon map!).
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
        /// Overlays to display on the wall.
        /// </summary>
        public DictionaryList<LabyrinthOverlayData> Overlays { get; private set; } = new();

        /// <summary>
        /// If active, the wall is rendered with
        /// fully transparent parts where the
        /// palette index is 0. Otherwise, those
        /// areas would just use that palette color
        /// which is in general pure black.
        /// 
        /// For example this is used for open doors,
        /// gates, windows or cobwebs.
        /// </summary>
        public bool Transparent { get; set; }

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

        /// <summary>
        /// The collision classes this wall allows passing.
        /// </summary>
        [Range(0, (1 << 15) - 1)]
        public uint AllowedCollisionClasses
        {
            get => _allowedCollisionClasses;
            set
            {
                ValueChecker.Check(value, 0, (1 << 15) - 1);
                SetField(ref _allowedCollisionClasses, value);
            }
        }

        /// <summary>
        /// If this is active the object will block sight.
        /// Monsters can't see through it.
        /// </summary>
        public bool BlockSight
        {
            get => _blockSight;
            set => SetField(ref _blockSight, value);
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            uint wallFlags = (AllowedCollisionClasses << 8) & 0x7fff00;

            if (wallFlags == 0) // no collision classes allowed?
                wallFlags = 0x80; // shortcut (= block all movement)

            wallFlags |= (CombatBackgroundIndex & 0xf) << 28;
            if (BlockSight)
                wallFlags |= (uint)Wall3DFlags.BlockSight;
            if (Transparent)
                wallFlags |= (uint)Wall3DFlags.Transparency;

            // Wall data
            dataWriter.Write(wallFlags);
            dataWriter.Write((byte)TextureIndex);
            dataWriter.Write((byte)AutomapType);
            dataWriter.Write((byte)ColorIndex);
            dataWriter.Write((byte)Overlays.Count);

            // Overlays
            foreach (var overlay in Overlays)
                overlay.Serialize(dataWriter, advanced);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var wallData = new LabyrinthWallData();

            uint flags = dataReader.ReadDword();
            var wallFlags = (Wall3DFlags)flags & Wall3DFlags.CombatBackgroundRemoveMask;
            wallData.CombatBackgroundIndex = flags >> 28;
            wallData.BlockSight = wallFlags.HasFlag(Wall3DFlags.BlockSight);
            wallData.Transparent = wallFlags.HasFlag(Wall3DFlags.Transparency);
            wallData.AllowedCollisionClasses = wallFlags.HasFlag(Wall3DFlags.BlockAllMovement)
                ? 0
                : (flags >> 8) & 0x7fff;
            wallData.TextureIndex = dataReader.ReadByte();
            wallData.AutomapType = (AutomapType)dataReader.ReadByte();
            wallData.ColorIndex = dataReader.ReadByte();

            // Overlays
            int numberOfOverlays = dataReader.ReadByte();
            var overlays = DataCollection<LabyrinthOverlayData>.Deserialize(dataReader, numberOfOverlays, advanced);
            wallData.Overlays = new DictionaryList<LabyrinthOverlayData>(overlays);
            // TODO: change detection

            return wallData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var wallData = (LabyrinthWallData)Deserialize(dataReader, advanced);
            (wallData as IMutableIndex).Index = index;
            return wallData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthWallData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   Transparent == other.Transparent &&
                   BlockSight == other.BlockSight &&
                   AllowedCollisionClasses == other.AllowedCollisionClasses &&
                   CombatBackgroundIndex == other.CombatBackgroundIndex &&
                   TextureIndex == other.TextureIndex &&
                   AutomapType == other.AutomapType &&
                   ColorIndex == other.ColorIndex &&
                   Overlays.Equals(other.Overlays);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthWallData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthWallData? left, LabyrinthWallData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthWallData? left, LabyrinthWallData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthWallData Copy()
        {
            var copy = new LabyrinthWallData();

            copy.Transparent = Transparent;
            copy.BlockSight = BlockSight;
            copy.AllowedCollisionClasses = AllowedCollisionClasses;
            copy.CombatBackgroundIndex = CombatBackgroundIndex;
            copy.TextureIndex = TextureIndex;
            copy.AutomapType = AutomapType;
            copy.ColorIndex = ColorIndex;
            copy.Overlays = new(Overlays.Select(e => e.Copy()));
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

    /// <summary>
    /// Describes a single 3D object. Each object in a labyrinth
    /// can be built from up to 8 such single objects.
    /// </summary>
    public sealed class LabyrinthObjectDescriptionData : IIndexedData, IMutableIndex, IEquatable<LabyrinthObjectDescriptionData>, INotifyPropertyChanged
    {

        #region Fields

        private bool _waveAnimation;
        private bool _blockSight;
        private bool _isFloorObject;
        private bool _randomAnimation;
        private uint _allowedCollisionClasses;
        private uint _textureIndex;
        private uint _numberOfFrames;
        private uint _originalWidth;
        private uint _originalHeight;
        private uint _displayWidth;
        private uint _displayHeight;
        private uint _combatBackgroundIndex;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// Texture file index inside XObject3D.amb.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public uint TextureIndex
        {
            get => _textureIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _textureIndex, value);
            }
        }

        /// <summary>
        /// Number of frames the object has.
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
        /// Original width of the object in pixels.
        /// This is used to load the correct amount of data
        /// from the texture file.
        ///
        /// This should be a multiple of 16. But in the original
        /// texture 90 had a width of 47 for some reason.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint OriginalWidth
        {
            get => _originalWidth;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _originalWidth, value);
            }
        }


        /// <summary>
        /// Original height of the object in pixels.
        /// This is used to load the correct amount of data
        /// from the texture file.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint OriginalHeight
        {
            get => _originalHeight;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _originalHeight, value);
            }
        }

        /// <summary>
        /// Display width of the object in pixels.
        /// For reference: Tiles in 3D have a width of 512 pixels.
        /// </summary>
        [Range(1, ushort.MaxValue)]
        public uint DisplayWidth
        {
            get => _displayWidth;
            set
            {
                ValueChecker.Check(value, 1, ushort.MaxValue);
                SetField(ref _displayWidth, value);
            }
        }

        /// <summary>
        /// Display height of the object in pixels.
        /// For reference: Walls in 3D have a height of 341 pixels.
        /// </summary>
        [Range(1, ushort.MaxValue)]
        public uint DisplayHeight
        {
            get => _displayHeight;
            set
            {
                ValueChecker.Check(value, 1, ushort.MaxValue);
                SetField(ref _displayHeight, value);
            }
        }

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
        /// In original this is called the "Distort_bit".
        /// If active the 3D object is displayed in a way
        /// that it looks like a layer above ground.
        /// 
        /// For example a tabletop or a hole in the
        /// ground or ceiling.
        /// 
        /// Otherwise, objects are displayed normally
        /// as a 2D billboard facing the player.
        /// </summary>
        public bool IsFloorObject
        {
            get => _isFloorObject;
            set => SetField(ref _isFloorObject, value);
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
        /// The collision classes this object allows passing.
        /// </summary>
        [Range(0, (1 << 15) - 1)]
        public uint AllowedCollisionClasses
        {
            get => _allowedCollisionClasses;
            set
            {
                ValueChecker.Check(value, 0, (1 << 15) - 1);
                SetField(ref _allowedCollisionClasses, value);
            }
        }

        /// <summary>
        /// If this is active the object will block sight.
        /// Monsters can't see through it.
        /// </summary>
        public bool BlockSight
        {
            get => _blockSight;
            set => SetField(ref _blockSight, value);
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            uint objectFlags = (AllowedCollisionClasses << 8) & 0x7fff00;

            if (objectFlags == 0) // no collision classes allowed?
                objectFlags = 0x80; // shortcut (= block all movement)

            objectFlags |= (CombatBackgroundIndex & 0xf) << 28;
            if (BlockSight)
                objectFlags |= (uint)Object3DFlags.BlockSight;
            if (WaveAnimation)
                objectFlags |= (uint)Object3DFlags.WaveAnimation;
            if (RandomAnimation)
                objectFlags |= (uint)Object3DFlags.RandomAnimationStart;
            if (IsFloorObject)
                objectFlags |= (uint)Object3DFlags.Floor;

            // Object description data
            dataWriter.Write(objectFlags);
            dataWriter.Write((ushort)TextureIndex);
            dataWriter.Write((byte)NumberOfFrames);
            dataWriter.Write((byte)0);
            dataWriter.Write((byte)OriginalWidth);
            dataWriter.Write((byte)OriginalHeight);
            dataWriter.Write((ushort)DisplayWidth);
            dataWriter.Write((ushort)DisplayHeight);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var objectDescriptionData = new LabyrinthObjectDescriptionData();

            uint flags = dataReader.ReadDword();
            var objectFlags = (Object3DFlags)flags & Object3DFlags.CombatBackgroundRemoveMask;
            objectDescriptionData.CombatBackgroundIndex = flags >> 28;
            objectDescriptionData.BlockSight = objectFlags.HasFlag(Object3DFlags.BlockSight);
            objectDescriptionData.WaveAnimation = objectFlags.HasFlag(Object3DFlags.WaveAnimation);
            objectDescriptionData.RandomAnimation = objectFlags.HasFlag(Object3DFlags.RandomAnimationStart);
            objectDescriptionData.IsFloorObject = objectFlags.HasFlag(Object3DFlags.Floor);
            objectDescriptionData.AllowedCollisionClasses = objectFlags.HasFlag(Object3DFlags.BlockAllMovement)
                ? 0
                : (flags >> 8) & 0x7fff;
            objectDescriptionData.TextureIndex = dataReader.ReadWord();
            objectDescriptionData.NumberOfFrames = dataReader.ReadByte();
            dataReader.Position++; // Unused / padding byte
            objectDescriptionData.OriginalWidth = dataReader.ReadByte();
            objectDescriptionData.OriginalHeight = dataReader.ReadByte();
            objectDescriptionData.DisplayWidth = dataReader.ReadWord();
            objectDescriptionData.DisplayHeight = dataReader.ReadWord();

            return objectDescriptionData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var objectDescriptionData = (LabyrinthObjectDescriptionData)Deserialize(dataReader, advanced);
            (objectDescriptionData as IMutableIndex).Index = index;
            return objectDescriptionData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthObjectDescriptionData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   WaveAnimation == other.WaveAnimation &&
                   BlockSight == other.BlockSight &&
                   IsFloorObject == other.IsFloorObject &&
                   RandomAnimation == other.RandomAnimation &&
                   AllowedCollisionClasses == other.AllowedCollisionClasses &&
                   CombatBackgroundIndex == other.CombatBackgroundIndex &&
                   TextureIndex == other.TextureIndex &&
                   NumberOfFrames == other.NumberOfFrames &&
                   OriginalWidth == other.OriginalWidth &&
                   OriginalHeight == other.OriginalHeight &&
                   DisplayWidth == other.DisplayWidth &&
                   DisplayHeight == other.DisplayHeight;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthObjectDescriptionData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthObjectDescriptionData? left, LabyrinthObjectDescriptionData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthObjectDescriptionData? left, LabyrinthObjectDescriptionData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthObjectDescriptionData Copy()
        {
            var copy = new LabyrinthObjectDescriptionData();

            copy.BlockSight = BlockSight;
            copy.WaveAnimation = WaveAnimation;
            copy.RandomAnimation = RandomAnimation;
            copy.IsFloorObject = IsFloorObject;
            copy.AllowedCollisionClasses = AllowedCollisionClasses;
            copy.CombatBackgroundIndex = CombatBackgroundIndex;
            copy.TextureIndex = TextureIndex;
            copy.NumberOfFrames = NumberOfFrames;
            copy.OriginalWidth = OriginalWidth;
            copy.OriginalHeight = OriginalHeight;
            copy.DisplayWidth = DisplayWidth;
            copy.DisplayHeight = DisplayHeight;
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

    /// <summary>
    /// Represents a reference to a 3D object description and its location.
    /// </summary>
    public sealed class LabyrinthObjectReferenceData : IIndexedData, IMutableIndex, IEquatable<LabyrinthObjectReferenceData>, INotifyPropertyChanged
    {

        #region Fields

        private int _x;
        private int _y;
        private int _z;
        private uint _objectDescriptionIndex;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// X coordinate of the object.
        ///
        /// This is the relative position to the
        /// left (west) of the tile going right (to the east).
        /// 0 is left, 512 is right and 255 is considered center.
        /// The object's origin is placed at this position.
        /// </summary>
        [Range(short.MinValue, short.MaxValue)]
        public int X
        {
            get => _x;
            set
            {
                ValueChecker.Check(value, short.MinValue, short.MaxValue);
                SetField(ref _x, value);
            }
        }

        /// <summary>
        /// Y coordinate of the object.
        ///
        /// This is the relative position to the
        /// bottom (south) of the tile going upwards (to the north).
        /// 0 is bottom/front, 512 is top/back and 255 is considered center.
        /// The object's origin is placed at this position.
        /// </summary>
        [Range(short.MinValue, short.MaxValue)]
        public int Y
        {
            get => _y;
            set
            {
                ValueChecker.Check(value, short.MinValue, short.MaxValue);
                SetField(ref _y, value);
            }
        }

        /// <summary>
        /// Z coordinate of the object.
        ///
        /// The lower bound of the object is placed at this height.
        /// A value of 0 means, that the object is on the floor.
        /// To exactly position the object at the ceiling,
        /// use 341 - <see cref="LabyrinthObjectDescriptionData.OriginalHeight"/>.
        /// This seems to be a special value to snap it to the ceiling.
        ///
        /// But in general the display height is added to this value.
        /// And this would be the upper position of the object.
        /// </summary>
        [Range(short.MinValue, short.MaxValue)]
        public int Z
        {
            get => _z;
            set
            {
                ValueChecker.Check(value, short.MinValue, short.MaxValue);
                SetField(ref _z, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint ObjectDescriptionIndex
        {
            get => _objectDescriptionIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _objectDescriptionIndex, value);
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Object reference data
            dataWriter.Write(Util.SignedToUnsignedWord(X));
            dataWriter.Write(Util.SignedToUnsignedWord(Y));
            dataWriter.Write(Util.SignedToUnsignedWord(Z));
            dataWriter.Write((ushort)ObjectDescriptionIndex);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var objectReferenceData = new LabyrinthObjectReferenceData();

            objectReferenceData.X = Util.UnsignedWordToSigned(dataReader.ReadWord());
            objectReferenceData.Y = Util.UnsignedWordToSigned(dataReader.ReadWord());
            objectReferenceData.Z = Util.UnsignedWordToSigned(dataReader.ReadWord());
            objectReferenceData.ObjectDescriptionIndex = dataReader.ReadWord();

            return objectReferenceData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var wallData = (LabyrinthObjectReferenceData)Deserialize(dataReader, advanced);
            (wallData as IMutableIndex).Index = index;
            return wallData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthObjectReferenceData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z &&
                   ObjectDescriptionIndex == other.ObjectDescriptionIndex;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthObjectReferenceData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthObjectReferenceData? left, LabyrinthObjectReferenceData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthObjectReferenceData? left, LabyrinthObjectReferenceData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthObjectReferenceData Copy()
        {
            var copy = new LabyrinthObjectReferenceData();

            copy.X = X;
            copy.Y = Y;
            copy.Z = Z;
            copy.ObjectDescriptionIndex = ObjectDescriptionIndex;
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


    /// <summary>
    /// Represents a 3D object in a labyrinth.
    ///
    /// It consists of up to 8 subobjects.
    /// </summary>
    public sealed class LabyrinthObjectData : IIndexedData, IMutableIndex, IEquatable<LabyrinthObjectData>, INotifyPropertyChanged
    {

        #region Fields

        private AutomapType _automapType;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// Icon type to show on the automap (dungeon map) for the object.
        /// </summary>
        public AutomapType AutomapType
        {
            get => _automapType;
            set => SetField(ref _automapType, value);
        }

        /// <summary>
        /// Subobjects of this object.
        /// </summary>
        public DataCollection<LabyrinthObjectReferenceData> Objects { get; private set; } = new(8);

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Object data
            dataWriter.Write((ushort)AutomapType);

            // Objects
            Objects.Serialize(dataWriter, advanced);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var objectData = new LabyrinthObjectData();

            objectData.AutomapType = (AutomapType)dataReader.ReadWord();

            // Objects
            objectData.Objects = DataCollection<LabyrinthObjectReferenceData>.Deserialize(dataReader, 8, advanced);
            objectData.Objects.ItemChanged += objectData.ObjectsChanged;

            return objectData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var wallData = (LabyrinthObjectData)Deserialize(dataReader, advanced);
            (wallData as IMutableIndex).Index = index;
            return wallData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthObjectData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   AutomapType == other.AutomapType &&
                   Objects.Equals(other.Objects);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthObjectData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthObjectData? left, LabyrinthObjectData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthObjectData? left, LabyrinthObjectData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthObjectData Copy()
        {
            var copy = new LabyrinthObjectData();

            copy.AutomapType = AutomapType;
            copy.Objects = Objects.Copy();
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

        private void ObjectsChanged(int index)
        {
            OnPropertyChanged(nameof(Objects));
        }

        #endregion

    }


    public sealed class LabyrinthData : IMutableIndex, IIndexedData, IEquatable<LabyrinthData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _wallHeight;
        private uint _defaultCombatBackgroundIndex;
        private uint _ceilingColorIndex;
        private uint _floorColorIndex;
        private uint? _ceilingTextureIndex;
        private uint _floorTextureIndex;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        /// <summary>
        /// Wall height in cm.
        ///
        /// Usually between 250 and 400.
        /// 
        /// 250 is very low and is used for Luminor's tower.
        /// </summary>
        [Range(200, 1000)]
        public uint WallHeight
        {
            get => _wallHeight;
            set
            {
                ValueChecker.Check(value, 200, 1000);
                SetField(ref _wallHeight, value);
            }
        }

        /// <summary>
        /// This is only used if a fight is started by a
        /// map event. Monsters on the map have their own
        /// value. Also map objects and wall have their
        /// own value, so this will only apply for fights
        /// which are started from empty map tiles.
        /// </summary>
        [Range(0, GameDataRepository.CombatBackgroundCount - 1)]
        public uint DefaultCombatBackgroundIndex
        {
            get => _defaultCombatBackgroundIndex;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.CombatBackgroundCount - 1);
                SetField(ref _defaultCombatBackgroundIndex, value);
            }
        }

        /// <summary>
        /// Color index inside the map palette. The ceiling color is
        /// shown if the ceiling texture is disabled and there is no sky.
        ///
        /// Note: For outdoor maps this specifies the color which is replaced
        /// by the current sky color. For example the upper border of the
        /// Spannenberg town park hedge uses this color. You will see the sky
        /// color there instead of the color which is specified in the palette.
        /// </summary>
        [Range(0, GameDataRepository.PaletteSize - 1)]
        public uint CeilingColorIndex
        {
            get => _ceilingColorIndex;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.PaletteSize - 1);
                SetField(ref _ceilingColorIndex, value);
            }
        }

        /// <summary>
        /// Color index inside the map palette. The floor color is
        /// shown if the floor texture is disabled.
        /// </summary>
        [Range(0, GameDataRepository.PaletteSize - 1)]
        public uint FloorColorIndex
        {
            get => _floorColorIndex;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.PaletteSize - 1);
                SetField(ref _floorColorIndex, value);
            }
        }

        /// <summary>
        /// Texture file index inside Floors.amb to use for the ceiling.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint? CeilingTextureIndex
        {
            get => _ceilingTextureIndex;
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, byte.MaxValue);
                SetField(ref _ceilingTextureIndex, value);
            }
        }

        /// <summary>
        /// Texture file index inside Floors.amb to use for the floor.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint FloorTextureIndex
        {
            get => _floorTextureIndex;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _floorTextureIndex, value);
            }
        }

        /// <summary>
        /// List of all available objects in the labyrinth data.
        /// </summary>
        public DictionaryList<LabyrinthObjectData> Objects { get; set; } = new();

        /// <summary>
        /// List of all available object descriptions in the labyrinth data.
        /// </summary>
        public DictionaryList<LabyrinthObjectDescriptionData> ObjectDescriptions { get; set; } = new();

        /// <summary>
        /// List of all available walls in the labyrinth data.
        /// </summary>
        public DictionaryList<LabyrinthWallData> Walls { get; set; } = new();

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Header
            dataWriter.Write((ushort)WallHeight);
            dataWriter.Write((ushort)DefaultCombatBackgroundIndex);
            dataWriter.Write((byte)CeilingColorIndex);
            dataWriter.Write((byte)FloorColorIndex);
            dataWriter.Write((byte)CeilingColorIndex);
            dataWriter.Write((byte)FloorTextureIndex);

            // Objects
            dataWriter.Write((ushort)Objects.Count);
            foreach (var entry in Objects)
                entry.Serialize(dataWriter, advanced);

            // Object descriptions
            dataWriter.Write((ushort)ObjectDescriptions.Count);
            foreach (var entry in ObjectDescriptions)
                entry.Serialize(dataWriter, advanced);

            // Walls
            dataWriter.Write((ushort)Walls.Count);
            foreach (var entry in Walls)
                entry.Serialize(dataWriter, advanced);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var labyrinthData = new LabyrinthData();

            labyrinthData.WallHeight = dataReader.ReadWord();
            labyrinthData.DefaultCombatBackgroundIndex = dataReader.ReadWord();
            labyrinthData.CeilingColorIndex = dataReader.ReadByte();
            labyrinthData.FloorColorIndex = dataReader.ReadByte();
            labyrinthData.CeilingColorIndex = dataReader.ReadByte();
            labyrinthData.FloorTextureIndex = dataReader.ReadByte();

            // Objects
            int numberOfObjects = dataReader.ReadWord();
            var objectList = DataCollection<LabyrinthObjectData>.Deserialize(dataReader, numberOfObjects, advanced);
            labyrinthData.Objects = new DictionaryList<LabyrinthObjectData>(objectList);
            // TODO: change detection

            // Object descriptions
            int numberOfObjectDescriptions = dataReader.ReadWord();
            var objectDescriptionList = DataCollection<LabyrinthObjectDescriptionData>.Deserialize(dataReader, numberOfObjectDescriptions, advanced);
            labyrinthData.ObjectDescriptions = new DictionaryList<LabyrinthObjectDescriptionData>(objectDescriptionList);
            // TODO: change detection

            // Walls
            int numberOfWalls = dataReader.ReadWord();
            var wallList = DataCollection<LabyrinthWallData>.Deserialize(dataReader, numberOfWalls, advanced);
            labyrinthData.Walls = new DictionaryList<LabyrinthWallData>(wallList);
            // TODO: change detection

            return labyrinthData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var labyrinthData = (LabyrinthData)Deserialize(dataReader, advanced);
            (labyrinthData as IMutableIndex).Index = index;
            return labyrinthData;
        }

        #endregion


        #region Equality

        public bool Equals(LabyrinthData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   WallHeight == other.WallHeight &&
                   DefaultCombatBackgroundIndex == other.DefaultCombatBackgroundIndex &&
                   CeilingColorIndex == other.CeilingColorIndex &&
                   FloorColorIndex == other.FloorColorIndex &&
                   CeilingTextureIndex == other.CeilingTextureIndex &&
                   FloorTextureIndex == other.FloorTextureIndex &&
                   Objects.Equals(other.Objects) &&
                   ObjectDescriptions.Equals(other.ObjectDescriptions) &&
                   Walls.Equals(other.Walls);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LabyrinthData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(LabyrinthData? left, LabyrinthData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LabyrinthData? left, LabyrinthData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public LabyrinthData Copy()
        {
            var copy = new LabyrinthData();

            copy.WallHeight = WallHeight;
            copy.DefaultCombatBackgroundIndex = DefaultCombatBackgroundIndex;
            copy.CeilingColorIndex = CeilingColorIndex;
            copy.FloorColorIndex = FloorColorIndex;
            copy.CeilingTextureIndex = CeilingTextureIndex;
            copy.FloorTextureIndex = FloorTextureIndex;
            copy.Objects = new(Objects.Select(e => e.Copy()));
            copy.ObjectDescriptions = new(ObjectDescriptions.Select(e => e.Copy()));
            copy.Walls = new(Walls.Select(e => e.Copy()));

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
}
