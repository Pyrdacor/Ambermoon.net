using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using System.ComponentModel.DataAnnotations;
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

    public class MapCharacterData : IIndexedDependentData<MapData>
    {
        private uint _collisionClass = 0;
        private uint _blockedCollisionClasses = 0;

        public uint Index { get; private set; }

        public uint? PartyMemberIndex { get; private set; }

        public uint? NpcIndex { get; private set; }

        public uint? MonsterGroupIndex { get; private set; }

        public uint? TextIndex { get; private set; }

        public CharacterType? CharacterType { get; private set; }

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
                if (value > 14)
                    throw new ArgumentOutOfRangeException(nameof(CollisionClass), "Collision classes are limited to the range 0 to 14.");

                _collisionClass = value;
            }
        }

        /// <summary>
        /// The collision classes this map character blocks on its own
        /// so that some other characters or the player might be blocked by it.
        /// </summary>
        [Range(0, (1 << 15) - 1)]
        public uint AllowedCollisionClasses
        {
            get => _blockedCollisionClasses;
            set
            {
                if (value >= (1 << 15))
                    throw new ArgumentOutOfRangeException(nameof(AllowedCollisionClasses), $"Blocked collision classes are limited to the range 0 to {(1 << 15) - 1}.");

                _blockedCollisionClasses = value;
            }
        }

        /// <summary>
        /// This is only used for characters on 2D non-world maps.
        /// The <see cref="GraphicIndex"/> will reference a map tile
        /// graphic instead of a NPC or monster graphic in this case.
        /// </summary>
        public bool UseTilesetGraphic { get; private set; }

        /// <summary>
        /// This is only used if the character type is an NPC.
        /// Text popup NPCs are just referencing a map text
        /// and simply display it when the player talks to them.
        /// </summary>
        public bool IsTextPopup { get; private set; }

        /// <summary>
        /// If active, the NPC window will open automatically
        /// when the player approaches the NPC by movement.
        /// This is only used if the character is a non-popup
        /// NPC.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool NpcStartsConversation { get; private set; }

        /// <summary>
        /// This is only used for monsters. If active the monster
        /// will not move when it does not see the player.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool OnlyMoveWhenSeePlayer { get; private set; }

        /// <summary>
        /// Movement type of the character. It depends on some
        /// flags but also on the character type.
        /// </summary>
        public MapCharacterMovementType MovementType { get; private set; }

        /// <summary>
        /// Graphic index for the character. The meaning depends on the
        /// map type and the <see cref="UseTilesetGraphic"/> flag:
        /// - 3D characters: 3D object index (2Lab_data.amb)
        /// - 2D character without <see cref="UseTilesetGraphic"/> flag: NPC graphic index (NPC_gfx.amb)
        /// - 2D character with <see cref="UseTilesetGraphic"/> flag: Tileset tile index (Icon_data.amb)
        /// 
        /// Note that NPC_gfx.amb stores (as of now) 2 subfiles. And the index
        /// of the used subfile is specified inside the map data in <see cref="MapData.NPCGraphicFileIndex"/>.
        /// Inside the file all graphics are stored in a sequence and they all have the
        /// same size. The graphic index is then the index for the n-th graphic.
        /// </summary>
        public uint GraphicIndex { get; private set; }

        /// <summary>
        /// Event that should be triggered on contact. This should
        /// only be used for map object characters and is ignored
        /// for all other character types.
        /// 
        /// The default value of 0 means (no event).
        /// </summary>
        public uint EventIndex { get; set; }

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
        public bool WaveAnimation { get; set; }

        /// <summary>
        /// Normally animations will run continously and
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
        /// the animation continously.
        /// </summary>
        public bool RandomAnimation { get; set; }

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
        public uint? CombatBackgroundIndex { get; private set; }

        public static MapCharacterData Create(DictionaryList<MapCharacterData> list, uint? index)
        {
            var mapCharacterData = new MapCharacterData { Index = index ?? list.Keys.Max() + 1 };
            list.Add(mapCharacterData);
            return mapCharacterData;
        }

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

                // TODO
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

                if (CharacterType == Ambermoon.Data.CharacterType.NPC)
                    typeAndFlags |= 0x10;

                if (UseTilesetGraphic)
                    typeAndFlags |= 0x08;

                if (CharacterType <= Ambermoon.Data.CharacterType.NPC && advanced && NpcStartsConversation)
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
            mapCharacterData.Index = index;
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

            return mapCharacterData;
        }
    }
}
