using System.Collections.Frozen;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    internal static class Files
    {
        public static readonly string[] RawFiles = new string[]
        {
            "AM2_BLIT",
            "AM2_CPU",
            "Ambermoon_extro",
            "Ambermoon_intro",
            "Extro_music",
            "Intro_music",
            "Saves",
            "Party_data.sav",
            "Fantasy_intro",
            "Keymap"
        };

        public static readonly FrozenDictionary<string, char> AmigaSaveFiles = new Dictionary<string, char>
        {
            // Disk A
            { "Initial/Automap.amb", 'A' },
            { "Initial/Chest_data.amb", 'A' },
            { "Initial/Merchant_data.amb", 'A' },
            { "Initial/Party_char.amb", 'A' },
            { "Initial/Party_data.sav", 'A' },
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
            { "Save.10/Party_data.sav", 'J' }
        }.ToFrozenDictionary();

        public static FrozenDictionary<string, char> New114Files = new Dictionary<string, char>
        {
            { "Dict.amb", 'G' },
            { "Button_graphics", 'A' },
            { "Objects.amb", 'A' },
            { "Text.amb", 'A' },
            { "Monster_char.amb", 'H' }
        }.ToFrozenDictionary();

        public static FrozenDictionary<string, string> Renamed114Files = new Dictionary<string, string>
        {
            { "Monster_char_data.amb", "Monster_char.amb" }
        }.ToFrozenDictionary();

        public static List<string> Removed114Files = new()
        {
            "Dictionary.english",
            "Dictionary.german",
            "Monster_char_data.amb",
            "AM2_BLIT"
        };

        // Key: Filename, Value: Disk letter
        public static readonly FrozenDictionary<string, char> AmigaFiles = new Dictionary<string, char>
        {
            // Disk A
            { "AM2_BLIT", 'A' },
            { "AM2_CPU", 'A' },
            { "Keymap", 'A' },
            { "Initial/Automap.amb", 'A' },
            { "Initial/Chest_data.amb", 'A' },
            { "Initial/Merchant_data.amb", 'A' },
            { "Initial/Party_char.amb", 'A' },
            { "Initial/Party_data.sav", 'A' },
            // Disk B
            { "Ambermoon_intro", 'B' },
            { "Fantasy_intro", 'B' },
            { "Intro_music", 'B' },
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
            { "Save.10/Party_data.sav", 'J' }
        }.ToFrozenDictionary();
    }
}
