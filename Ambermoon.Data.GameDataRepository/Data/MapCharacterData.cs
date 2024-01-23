using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ambermoon.Data.GameDataRepository.Collections;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;
    using Util;

    public enum MapCharacterMovementType
    {
        /// <summary>
        /// No movement (fixed position)
        /// 
        /// This was possible for monsters only in the original.
        /// It was also possible for NPCs but by using a path where
        /// all positions were the same.
        /// 
        /// In the advanced version this can be set as an explicit
        /// character flag for NPCs and party members.
        /// </summary>
        Stationary,
        /// <summary>
        /// Random movement (monsters will also chase the player if they see him)
        /// </summary>
        Random,
        /// <summary>
        /// Character follows a given path (NPCs and party members only)
        /// </summary>
        Path
    }

    
    public sealed class MapCharacterData : IMutableIndex, IIndexedDependentData<MapData>, IEquatable<MapCharacterData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _collisionClass = 0;
        private uint _allowedCollisionClasses = 0;
        private bool _waveAnimation;
        private bool _randomAnimation;
        private uint _graphicIndex;
        private MapPositionData _position = MapPositionData.Invalid;
        private uint? _combatBackgroundIndex;
        private uint _eventIndex;
        private MapCharacterMovementType _movementType;
        private bool _onlyMoveWhenSeePlayer;
        private bool _npcStartsConversation;
        private bool _isTextPopup;
        private bool _useTilesetGraphic;
        private CharacterType? _characterType;
        private uint? _monsterGroupIndex;
        private uint? _textIndex;
        private uint? _npcIndex;
        private uint? _partyMemberIndex;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public uint? PartyMemberIndex
        {
            get => _partyMemberIndex;
            private set => SetField(ref _partyMemberIndex, value);
        }

        public uint? NpcIndex
        {
            get => _npcIndex;
            private set => SetField(ref _npcIndex, value);
        }

        public uint? MonsterGroupIndex
        {
            get => _monsterGroupIndex;
            private set => SetField(ref _monsterGroupIndex, value);
        }

        public uint? TextIndex
        {
            get => _textIndex;
            private set => SetField(ref _textIndex, value);
        }

        public CharacterType? CharacterType
        {
            get => _characterType;
            private set => SetField(ref _characterType, value);
        }

        /// <summary>
        /// There can be up to 15 collision classes.
        /// 
        /// Each map tile can specify which collision class
        /// is blocked by the tile or can move through.
        /// In Ambermoon the player has always collision class 0
        /// in 3D and on 2D non-world maps.
        /// 
        /// On world maps the collision class becomes the
        /// travel type like moving, swimming, riding a horse,
        /// flying on an eagle, etc. But there should be no
        /// map character on those maps.
        /// </summary>
        [Range(0, 14)]
        public uint CollisionClass
        {
            get => _collisionClass;
            set
            {
                ValueChecker.Check(value, 0, 14);
                SetField(ref _collisionClass, value);
            }
        }

        /// <summary>
        /// The collision classes this map character blocks on its own
        /// so that some other characters or the player might be blocked by it.
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
        /// This is only used for characters on 2D non-world maps.
        /// The <see cref="GraphicIndex"/> will reference a map tile
        /// graphic instead of a NPC or monster graphic in this case.
        /// </summary>
        public bool UseTilesetGraphic
        {
            get => _useTilesetGraphic;
            private set => SetField(ref _useTilesetGraphic, value);
        }

        /// <summary>
        /// This is only used if the character type is an NPC.
        /// Text popup NPCs are just referencing a map text
        /// and simply display it when the player talks to them.
        /// </summary>
        public bool IsTextPopup
        {
            get => _isTextPopup;
            private set => SetField(ref _isTextPopup, value);
        }

        /// <summary>
        /// If active, the NPC window will open automatically
        /// when the player approaches the NPC by movement.
        /// This is only used if the character is a non-popup
        /// NPC.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool NpcStartsConversation
        {
            get => _npcStartsConversation;
            private set => SetField(ref _npcStartsConversation, value);
        }

        /// <summary>
        /// This is only used for monsters. If active the monster
        /// will not move when it does not see the player.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool OnlyMoveWhenSeePlayer
        {
            get => _onlyMoveWhenSeePlayer;
            private set => SetField(ref _onlyMoveWhenSeePlayer, value);
        }

        /// <summary>
        /// Movement type of the character. It depends on some
        /// flags but also on the character type.
        /// </summary>
        public MapCharacterMovementType MovementType
        {
            get => _movementType;
            private set => SetField(ref _movementType, value);
        }

        /// <summary>
        /// Graphic index for the character. The meaning depends on the
        /// map type and the <see cref="UseTilesetGraphic"/> flag:
        /// - 3D characters: 3D object index (2Lab_data.amb)
        /// - 2D character without <see cref="UseTilesetGraphic"/> flag: NPC graphic index (NPC_gfx.amb)
        /// - 2D character with <see cref="UseTilesetGraphic"/> flag: Tileset tile index (Icon_data.amb)
        /// 
        /// Note that NPC_gfx.amb stores (as of now) 2 sub-files. And the index
        /// of the used sub-file is specified inside the map data in <see cref="MapData.NpcGraphicFileIndex"/>.
        /// Inside the file all graphics are stored in a sequence and they all have the
        /// same size. The graphic index is then the index for the n-th graphic.
        /// </summary>
        public uint GraphicIndex
        {
            get => _graphicIndex;
            set => SetField(ref _graphicIndex, value);
        }

        /// <summary>
        /// Event that should be triggered on contact. This should
        /// only be used for map object characters and is ignored
        /// for all other character types.
        /// 
        /// The default value of 0 means (no event).
        /// </summary>
        public uint EventIndex
        {
            get => _eventIndex;
            private set => SetField(ref _eventIndex, value);
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
        /// In original this is called the "Distort_bit".
        /// If active the 3D object is display in a way
        /// that it looks like a layer above ground.
        /// 
        /// For example a table top or a hole in the
        /// floor or ceiling.
        /// 
        /// Otherwise objects are displayed normally
        /// as a 2D billboard looking at the player.
        /// 
        /// This is only used for 3D objects and
        /// ignored otherwise.
        /// </summary>
        //public bool FloorObject { get; set; }
        // TODO: Use this in 3D objects but not characters

        /// <summary>
        /// If active, the wall is rendered with
        /// fully transparent parts where the
        /// palette index is 0. Otherwise those
        /// areas would just use that palette color
        /// which is in general pure black.
        /// 
        /// For example this is used for open doors,
        /// gates, windows or cob webs.
        /// 
        /// This is only used for 3D walls and
        /// ignored otherwise.
        /// </summary>
        //public bool TransparentWall { get; set; }
        // TODO: Use this in 3D walls but not characters

        /// <summary>
        /// Combat background index which is used when
        /// fighting the monster (group).
        /// </summary>
        [Range(0, 15)]
        public uint? CombatBackgroundIndex
        {
            get => _combatBackgroundIndex;
            private set
            {
                if (CharacterType != Ambermoon.Data.CharacterType.Monster && value is not null)
                    throw new InvalidOperationException($"{nameof(CombatBackgroundIndex)} can only be set for monsters.");
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, 15);
                SetField(ref _combatBackgroundIndex, value);
            }
        }

        public DataCollection<MapPositionData>? Path { get; internal set; }

        public MapPositionData Position
        {
            get => _position;
            set
            {
                if (value.IsInvalid)
                    throw new InvalidOperationException("The given position is invalid.");
                SetField(ref _position, value);
                if (Path is not null)
                    Path[0] = value;
            }
        }

        #endregion


        #region Methods

        public void SetEmpty()
        {
            CharacterType = null;
            GraphicIndex = 0;
            // Note: For serialization the default character type is PartyMember
            // and for this type, movement type path is the default of 0.
            MovementType = MapCharacterMovementType.Path;
            MonsterGroupIndex = null;
            PartyMemberIndex = null;
            NpcIndex = null;
            TextIndex = null;
            EventIndex = 0;
            IsTextPopup = false;
            NpcStartsConversation = false;
            OnlyMoveWhenSeePlayer = false;
            CombatBackgroundIndex = null;
            RemovePath();
            Position = MapPositionData.Invalid;
        }

        private void SetMonster([Range(1, byte.MaxValue)] uint monsterGroupIndex,
            [Range(0, 15)] uint combatBackgroundIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool onlyMoveWhenSeePlayer = false)
        {
            ValueChecker.Check(monsterGroupIndex, 1, byte.MaxValue);
            ValueChecker.Check(combatBackgroundIndex, 0, 15);
            ValueChecker.Check(graphicIndex, 0, ushort.MaxValue);

            if (movementType == MapCharacterMovementType.Path)
                throw new ArgumentException("Movement type must not be path for monsters.");

            MonsterGroupIndex = monsterGroupIndex;
            CombatBackgroundIndex = combatBackgroundIndex;
            CharacterType = Ambermoon.Data.CharacterType.Monster;
            GraphicIndex = graphicIndex;
            MovementType = movementType;
            OnlyMoveWhenSeePlayer = onlyMoveWhenSeePlayer;

            PartyMemberIndex = null;
            NpcIndex = null;
            TextIndex = null;
            EventIndex = 0;
            IsTextPopup = false;
            NpcStartsConversation = false;
            RemovePath();
        }

        public void SetMonster2D([Range(1, byte.MaxValue)] uint monsterGroupIndex,
            [Range(0, 15)] uint combatBackgroundIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool useTilesetGraphic = false,
            bool onlyMoveWhenSeePlayer = false)
        {
            SetMonster(monsterGroupIndex, combatBackgroundIndex, graphicIndex, movementType, onlyMoveWhenSeePlayer);
            UseTilesetGraphic = useTilesetGraphic;
        }

        public void SetMonster3D([Range(1, byte.MaxValue)] uint monsterGroupIndex,
            [Range(0, 15)] uint combatBackgroundIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool onlyMoveWhenSeePlayer = false)
        {
            SetMonster(monsterGroupIndex, combatBackgroundIndex, graphicIndex, movementType, onlyMoveWhenSeePlayer);
            UseTilesetGraphic = false;
        }

        private void SetPartyMember([Range(0, byte.MaxValue)] uint partyMemberIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool npcStartsConversation = false)
        {
            ValueChecker.Check(partyMemberIndex, 1, byte.MaxValue);
            ValueChecker.Check(graphicIndex, 0, ushort.MaxValue);

            PartyMemberIndex = partyMemberIndex;
            CharacterType = Ambermoon.Data.CharacterType.PartyMember;
            GraphicIndex = graphicIndex;
            MovementType = movementType;
            NpcStartsConversation = npcStartsConversation;

            MonsterGroupIndex = null;
            NpcIndex = null;
            TextIndex = null;
            CombatBackgroundIndex = null;
            EventIndex = 0;
            IsTextPopup = false;
            OnlyMoveWhenSeePlayer = false;
            if (movementType == MapCharacterMovementType.Path)
                InitPath();
            else
                RemovePath();
        }

        public void SetPartyMember2D([Range(1, byte.MaxValue)] uint partyMemberIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool useTilesetGraphic = false,
            bool npcStartsConversation = false)
        {
            SetPartyMember(partyMemberIndex, graphicIndex, movementType, npcStartsConversation);
            UseTilesetGraphic = useTilesetGraphic;
        }

        public void SetPartyMember3D([Range(1, byte.MaxValue)] uint partyMemberIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool npcStartsConversation = false)
        {
            SetPartyMember(partyMemberIndex, graphicIndex, movementType, npcStartsConversation);
            UseTilesetGraphic = false;
        }

        private void SetNpc([Range(1, byte.MaxValue)] uint npcIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool npcStartsConversation = false)
        {
            ValueChecker.Check(npcIndex, 1, byte.MaxValue);
            ValueChecker.Check(graphicIndex, 0, ushort.MaxValue);

            NpcIndex = npcIndex;
            CharacterType = Ambermoon.Data.CharacterType.NPC;
            GraphicIndex = graphicIndex;
            MovementType = movementType;
            NpcStartsConversation = npcStartsConversation;

            MonsterGroupIndex = null;
            PartyMemberIndex = null;
            TextIndex = null;
            CombatBackgroundIndex = null;
            EventIndex = 0;
            IsTextPopup = false;
            OnlyMoveWhenSeePlayer = false;
            if (movementType == MapCharacterMovementType.Path)
                InitPath();
            else
                RemovePath();
        }

        public void SetNpc2D([Range(1, byte.MaxValue)] uint npcIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool useTilesetGraphic = false,
            bool npcStartsConversation = false)
        {
            SetNpc(npcIndex, graphicIndex, movementType, npcStartsConversation);
            UseTilesetGraphic = useTilesetGraphic;
        }

        public void SetNpc3D([Range(1, byte.MaxValue)] uint npcIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool npcStartsConversation = false)
        {
            SetNpc(npcIndex, graphicIndex, movementType, npcStartsConversation);
            UseTilesetGraphic = false;
        }

        // Note: There is no 3D version of this.
        public void SetTextPopupNpc2D([Range(1, byte.MaxValue)] uint textIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool useTilesetGraphic = false)
        {
            ValueChecker.Check(textIndex, 1, byte.MaxValue);
            ValueChecker.Check(graphicIndex, 0, ushort.MaxValue);

            TextIndex = textIndex;
            CharacterType = Ambermoon.Data.CharacterType.NPC;
            GraphicIndex = graphicIndex;
            MovementType = movementType;
            IsTextPopup = true;

            MonsterGroupIndex = null;
            PartyMemberIndex = null;
            NpcIndex = null;
            CombatBackgroundIndex = null;
            EventIndex = 0;
            OnlyMoveWhenSeePlayer = false;
            NpcStartsConversation = false;
            if (movementType == MapCharacterMovementType.Path)
                InitPath();
            else
                RemovePath();
        }

        private void SetMapObject([Range(0, byte.MaxValue)] uint eventIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random)
        {
            ValueChecker.Check(eventIndex, 0, byte.MaxValue);
            ValueChecker.Check(graphicIndex, 0, ushort.MaxValue);

            CharacterType = Ambermoon.Data.CharacterType.MapObject;
            GraphicIndex = graphicIndex;
            MovementType = movementType;

            MonsterGroupIndex = null;
            PartyMemberIndex = null;
            NpcIndex = null;
            TextIndex = null;
            CombatBackgroundIndex = null;
            EventIndex = 0;
            IsTextPopup = false;
            OnlyMoveWhenSeePlayer = false;
            NpcStartsConversation = false;
            if (movementType == MapCharacterMovementType.Path)
                InitPath();
            else
                RemovePath();
        }

        public void SetMapObject2D([Range(0, byte.MaxValue)] uint eventIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random,
            bool useTilesetGraphic = false)
        {
            SetMapObject(eventIndex, graphicIndex, movementType);
            UseTilesetGraphic = useTilesetGraphic;
        }

        public void SetMapObject3D([Range(0, byte.MaxValue)] uint eventIndex,
            [Range(0, ushort.MaxValue)] uint graphicIndex,
            MapCharacterMovementType movementType = MapCharacterMovementType.Random)
        {
            SetMapObject(eventIndex, graphicIndex, movementType);
            UseTilesetGraphic = false;
        }

        private void RemovePath()
        {
            if (Path != null)
            {
                Path.ItemChanged -= PathItemChanged;
                Path = null;
                OnPropertyChanged(nameof(Path));
            }
        }

        internal void InitPath(DataCollection<MapPositionData>? path = null)
        {
            if (Path is null)
            {
                Path = new DataCollection<MapPositionData>(288, _ => Position.Copy());
                Path.ItemChanged += PathItemChanged;
                OnPropertyChanged(nameof(Path));
            }
        }

        #endregion


        #region Serialization

        /// <inheritdoc/>
        public void Serialize(IDataWriter dataWriter, bool advanced)
        {

            if (CharacterType is null)
            {
                dataWriter.Write(Enumerable.Repeat((byte)0, 10).ToArray());
            }
            else
            {
                uint tileFlags = (AllowedCollisionClasses << 8) & 0x7fff00;

                if (tileFlags == 0) // no collision classes allowed?
                    tileFlags = 0x80; // shortcut (= block all movement)

                if (CharacterType == Ambermoon.Data.CharacterType.Monster)
                    tileFlags |= ((CombatBackgroundIndex!.Value & 0xf) << 28);
                tileFlags |= WaveAnimation ? 0x1u : 0x0u;
                tileFlags |= RandomAnimation ? 0x10u : 0x10u;

                uint characterIndex = CharacterType switch
                {
                    Ambermoon.Data.CharacterType.Monster => MonsterGroupIndex!.Value,
                    Ambermoon.Data.CharacterType.MapObject => 1u,
                    Ambermoon.Data.CharacterType.PartyMember => PartyMemberIndex!.Value,
                    _ => IsTextPopup ? TextIndex!.Value : NpcIndex!.Value,
                };

                uint typeAndFlags = (uint)CharacterType;
                
                switch (CharacterType)
                {
                    case Ambermoon.Data.CharacterType.Monster:
                        if (MovementType == MapCharacterMovementType.Random)
                            typeAndFlags |= 0x04;
                        break;
                    default:
                        if (MovementType == MapCharacterMovementType.Stationary && advanced)
                            typeAndFlags |= 0x80;
                        else if (MovementType == MapCharacterMovementType.Random)
                            typeAndFlags |= 0x04;
                        break;
                }

                if (CharacterType == Ambermoon.Data.CharacterType.NPC && IsTextPopup)
                    typeAndFlags |= 0x10;

                if (UseTilesetGraphic)
                    typeAndFlags |= 0x08;

                if (!IsTextPopup && CharacterType <= Ambermoon.Data.CharacterType.NPC && advanced && NpcStartsConversation)
                    typeAndFlags |= 0x20;

                dataWriter.Write((byte)characterIndex);
                dataWriter.Write((byte)CollisionClass);
                dataWriter.Write((byte)typeAndFlags);
                dataWriter.Write((byte)EventIndex);
                dataWriter.Write((ushort)GraphicIndex);
                dataWriter.Write(tileFlags);
            }
        }

        /// <inheritdoc/>
        public static IIndexedDependentData<MapData> Deserialize(IDataReader dataReader, uint index, MapData providedData, bool advanced)
        {
            var mapCharacterData = (MapCharacterData)Deserialize(dataReader, providedData, advanced);
            (mapCharacterData as IMutableIndex).Index = index;
            return mapCharacterData;
        }

        /// <inheritdoc/>
        public static IDependentData<MapData> Deserialize(IDataReader dataReader, MapData providedData, bool advanced)
        {
            var mapCharacterData = new MapCharacterData();

            uint characterIndex = dataReader.ReadByte();
            mapCharacterData.CollisionClass = dataReader.ReadByte();
            uint typeAndFlags = dataReader.ReadByte();
            uint eventIndex = dataReader.ReadByte();
            mapCharacterData.GraphicIndex = dataReader.ReadWord();
            uint tileFlags = dataReader.ReadDword();

            if (characterIndex == 0)
            {
                mapCharacterData.CharacterType = null;
                return mapCharacterData;
            }

            mapCharacterData.CharacterType = (CharacterType)(typeAndFlags & 3);
            typeAndFlags >>= 2;

            uint baseMovementType = typeAndFlags & 1;

            switch (mapCharacterData.CharacterType.Value)
            {
                case Ambermoon.Data.CharacterType.Monster:
                    mapCharacterData.MovementType = baseMovementType == 0
                        ? MapCharacterMovementType.Stationary
                        : MapCharacterMovementType.Random;
                    break;
                default:
                {
                    bool stationary = advanced && (typeAndFlags & 0x20) != 0;
                    mapCharacterData.MovementType = baseMovementType == 0
                        ? (stationary ? MapCharacterMovementType.Stationary : MapCharacterMovementType.Path)
                        : MapCharacterMovementType.Random;
                    break;
                }
            }

            switch (mapCharacterData.CharacterType)
            {
                case Ambermoon.Data.CharacterType.Monster:
                    mapCharacterData.MonsterGroupIndex = characterIndex;
                    mapCharacterData.CombatBackgroundIndex = tileFlags >> 28;
                    mapCharacterData.OnlyMoveWhenSeePlayer = advanced && (typeAndFlags & 0x20) != 0;
                    break;
                case Ambermoon.Data.CharacterType.PartyMember:
                    mapCharacterData.PartyMemberIndex = characterIndex;
                    mapCharacterData.NpcStartsConversation = advanced && (typeAndFlags & 0x08) != 0;
                    break;
                case Ambermoon.Data.CharacterType.NPC:
                    mapCharacterData.IsTextPopup = (typeAndFlags & 0x04) != 0;
                    mapCharacterData.NpcStartsConversation = advanced && (typeAndFlags & 0x08) != 0;
                    if (mapCharacterData.IsTextPopup)
                        mapCharacterData.TextIndex = characterIndex;
                    else
                        mapCharacterData.NpcIndex = characterIndex;
                    break;
                case Ambermoon.Data.CharacterType.MapObject:
                    mapCharacterData.EventIndex = eventIndex;
                    break;
            }

            mapCharacterData.UseTilesetGraphic = providedData.Type == MapType.Map2D && (typeAndFlags & 0x02) != 0;
            mapCharacterData.WaveAnimation = (tileFlags & 0x01) != 0;
            mapCharacterData.RandomAnimation = (tileFlags & 0x10) != 0;
            mapCharacterData.AllowedCollisionClasses = (tileFlags & 0x80) != 0 ? 0 : (tileFlags >> 8) & 0x7fff;

            if (mapCharacterData.MovementType == MapCharacterMovementType.Path)
            {
                mapCharacterData.InitPath();
            }

            return mapCharacterData;
        }

        #endregion


        #region Equality

        public bool Equals(MapCharacterData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _collisionClass == other._collisionClass &&
                   _allowedCollisionClasses == other._allowedCollisionClasses &&
                   Index == other.Index &&
                   PartyMemberIndex == other.PartyMemberIndex &&
                   NpcIndex == other.NpcIndex &&
                   MonsterGroupIndex == other.MonsterGroupIndex &&
                   TextIndex == other.TextIndex &&
                   CharacterType == other.CharacterType &&
                   UseTilesetGraphic == other.UseTilesetGraphic &&
                   IsTextPopup == other.IsTextPopup &&
                   NpcStartsConversation == other.NpcStartsConversation &&
                   OnlyMoveWhenSeePlayer == other.OnlyMoveWhenSeePlayer &&
                   MovementType == other.MovementType &&
                   GraphicIndex == other.GraphicIndex &&
                   EventIndex == other.EventIndex &&
                   WaveAnimation == other.WaveAnimation &&
                   RandomAnimation == other.RandomAnimation &&
                   CombatBackgroundIndex == other.CombatBackgroundIndex &&
                   Position == other.Position &&
                   Path == other.Path;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapCharacterData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MapCharacterData? left, MapCharacterData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapCharacterData? left, MapCharacterData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapCharacterData Copy()
        {
            // TODO
            return new();
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

        private void PathItemChanged(int index)
        {
            if (Path is not null && index == 0)
                Position = Path[0];

            OnPropertyChanged(nameof(Path));
        }

        #endregion

    }
}
