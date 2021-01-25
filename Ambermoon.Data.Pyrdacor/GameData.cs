using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Pyrdacor.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LegacyTextReader = Ambermoon.Data.Legacy.Serialization.TextReader;

namespace Ambermoon.Data.Pyrdacor
{
    class GameData : IGameData
    {
        public Dictionary<string, IFileContainer> Files { get; } = new Dictionary<string, IFileContainer>();
        public Dictionary<string, IDataReader> Dictionaries { get; } = new Dictionary<string, IDataReader>();
        public Dictionary<StationaryImage, GraphicInfo> StationaryImageInfos => Legacy.GameData.StationaryImageInformation;
        public Character2DAnimationInfo PlayerAnimationInfo => Legacy.GameData.PlayerAnimationInformation;
        private readonly List<TravelGraphicInfo> travelGraphicInfos = new List<TravelGraphicInfo>(44);
        internal List<Graphic> TravelGraphics { get; } = new List<Graphic>(44);

        public TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction) => travelGraphicInfos[(int)type * 4 + (int)direction];

        public void Load(Stream stream)
        {

        }

        /// <summary>
        /// Saves a whole savegame.
        /// </summary>
        /// <param name="index">0-10 and -1 for initial savegame.</param>
        void SaveSavegame(int index, Legacy.GameData legacyData, ContainerWriter writer, Dictionary<string, uint> fileDictionary)
        {
            /*{ "Initial/Automap.amb", 'A' },
            { "Initial/Chest_data.amb", 'A' },
            { "Initial/Merchant_data.amb", 'A' },
            { "Initial/Party_char.amb", 'A' },
            { "Initial/Party_data.sav", 'A' },
            { "Save.00/Automap.amb", 'J' },
            { "Save.00/Chest_data.amb", 'J' },
            { "Save.00/Merchant_data.amb", 'J' },
            { "Save.00/Party_char.amb", 'J' },
            { "Save.00/Party_data.sav", 'J' },*/
        }

        void WriteTextContainer(string name, string legacyName, Legacy.GameData legacyData, ContainerWriter writer, Dictionary<string, uint> fileDictionary)
        {
            fileDictionary.Add(name, (uint)writer.BaseStream.Position);
            var subFiles = legacyData.Files[legacyName].Files;

            subFiles.ToDictionary(f => f.Key, f => LegacyTextReader.ReadTexts(f.Value)
        }

        public void SaveFromLegacy(Legacy.GameData legacyData, Stream stream)
        {
            var fileDictionary = new Dictionary<string, uint>();
            using var writer = new ContainerWriter(stream, Encoding.UTF8, true, Endianness.Little);

            for (int i = -1; i <= 10; ++i)
                SaveSavegame(i, legacyData, writer, fileDictionary);
            // TODO:
            /*
            { "Ambermoon_intro", 'B' },
            { "Fantasy_intro", 'B' },
            { "Intro_music", 'B' },
            */
            /*// Disk A
            // Disk C
            { "1Icon_gfx.amb", 'C' },
            { "1Map_data.amb", 'C' },
            { "1Map_texts.amb", 'C' },
            // Disk D
            { "2Icon_gfx.amb", 'D' },
            { "2Lab_data.amb", 'D' },
            { "2Map_data.amb", 'D' },
            { "2Map_texts.amb", 'D' },
            { "2Object3D.amb", 'D' },
            // Disk E
            { "2Overlay3D.amb", 'E' },
            { "2Wall3D.amb", 'E' },
            // Disk F
            { "3Icon_gfx.amb", 'F' },
            { "3Lab_data.amb", 'F' },
            { "3Map_data.amb", 'F' },
            { "3Map_texts.amb", 'F' },
            { "3Object3D.amb", 'F' },
            { "3Overlay3D.amb", 'F' },
            { "3Wall3D.amb", 'F' },
            // Disk G
            { "Automap_graphics", 'G' },
            { "Combat_graphics", 'G' },
            { "Dictionary.english", 'G' },
            { "Dictionary.german", 'G' },
            { "Event_pix.amb", 'G' },
            { "Floors.amb", 'G' },
            { "Icon_data.amb", 'G' },
            { "Lab_background.amb", 'G' },
            { "Layouts.amb", 'G' },
            { "NPC_char.amb", 'G' },
            { "NPC_gfx.amb", 'G' },
            { "NPC_texts.amb", 'G' },
            { "Object_icons", 'G' },
            { "Object_texts.amb", 'G' },
            { "Palettes.amb", 'G' },
            { "Party_gfx.amb", 'G' },
            { "Party_texts.amb", 'G' },
            { "Pics_80x80.amb", 'G' },
            { "Place_data", 'G' },
            { "Portraits.amb", 'G' },
            { "Riddlemouth_graphics", 'G' },
            { "Stationary", 'G' },
            { "Travel_gfx.amb", 'G' },
            // Disk H
            { "Combat_background.amb", 'H' },
            { "Monster_char_data.amb", 'H' },
            { "Monster_gfx.amb", 'H' },
            { "Monster_groups.amb", 'H' },
            // Disk I
            { "Ambermoon_extro", 'I' },
            { "Extro_music", 'I' },
            { "Music.amb", 'I' },
            // Disk J
            { "Automap.amb", 'J' },
            { "Chest_data.amb", 'J' },
            { "Merchant_data.amb", 'J' },
            { "Party_char.amb", 'J' },
            { "Party_data.sav", 'J' },
            { "Saves", 'J' },
            { "Save.00/Automap.amb", 'J' },
            { "Save.00/Chest_data.amb", 'J' },
            { "Save.00/Merchant_data.amb", 'J' },
            { "Save.00/Party_char.amb", 'J' },
            { "Save.00/Party_data.sav", 'J' },
            { "Save.01/Automap.amb", 'J' },
            { "Save.01/Chest_data.amb", 'J' },
            { "Save.01/Merchant_data.amb", 'J' },
            { "Save.01/Party_char.amb", 'J' },
            { "Save.01/Party_data.sav", 'J' },
            { "Save.02/Automap.amb", 'J' },
            { "Save.02/Chest_data.amb", 'J' },
            { "Save.02/Merchant_data.amb", 'J' },
            { "Save.02/Party_char.amb", 'J' },
            { "Save.02/Party_data.sav", 'J' },
            { "Save.03/Automap.amb", 'J' },
            { "Save.03/Chest_data.amb", 'J' },
            { "Save.03/Merchant_data.amb", 'J' },
            { "Save.03/Party_char.amb", 'J' },
            { "Save.03/Party_data.sav", 'J' },
            { "Save.04/Automap.amb", 'J' },
            { "Save.04/Chest_data.amb", 'J' },
            { "Save.04/Merchant_data.amb", 'J' },
            { "Save.04/Party_char.amb", 'J' },
            { "Save.04/Party_data.sav", 'J' },
            { "Save.05/Automap.amb", 'J' },
            { "Save.05/Chest_data.amb", 'J' },
            { "Save.05/Merchant_data.amb", 'J' },
            { "Save.05/Party_char.amb", 'J' },
            { "Save.05/Party_data.sav", 'J' },
            { "Save.06/Automap.amb", 'J' },
            { "Save.06/Chest_data.amb", 'J' },
            { "Save.06/Merchant_data.amb", 'J' },
            { "Save.06/Party_char.amb", 'J' },
            { "Save.06/Party_data.sav", 'J' },
            { "Save.07/Automap.amb", 'J' },
            { "Save.07/Chest_data.amb", 'J' },
            { "Save.07/Merchant_data.amb", 'J' },
            { "Save.07/Party_char.amb", 'J' },
            { "Save.07/Party_data.sav", 'J' },
            { "Save.08/Automap.amb", 'J' },
            { "Save.08/Chest_data.amb", 'J' },
            { "Save.08/Merchant_data.amb", 'J' },
            { "Save.08/Party_char.amb", 'J' },
            { "Save.08/Party_data.sav", 'J' },
            { "Save.09/Automap.amb", 'J' },
            { "Save.09/Chest_data.amb", 'J' },
            { "Save.09/Merchant_data.amb", 'J' },
            { "Save.09/Party_char.amb", 'J' },
            { "Save.09/Party_data.sav", 'J' },
            { "Save.10/Automap.amb", 'J' },
            { "Save.10/Chest_data.amb", 'J' },
            { "Save.10/Merchant_data.amb", 'J' },
            { "Save.10/Party_char.amb", 'J' },
            { "Save.10/Party_data.sav", 'J' }*/
        }
    }
}
