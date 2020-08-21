using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    // TODO: Check all disks in WinUAE to ensure we have all we need
    // TODO: The initial save game is on disk A in the folder "Initial":
    //       Automap.amb, Chest_data.amb, Merchant_data.amb,
    //       Party_char.amb, Party_data.sav

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

        // Key: Filename, Value: Disk letter
        public static readonly Dictionary<string, char> AmigaFiles = new Dictionary<string, char>
        {
            // Disk A
            { "AM2_BLIT", 'A' },
            { "AM2_CPU", 'A' },
            { "Keymap", 'A' },
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
            { "Saves", 'J' }
            // TODO: Save slots like Save00/...
        };
    }
}
