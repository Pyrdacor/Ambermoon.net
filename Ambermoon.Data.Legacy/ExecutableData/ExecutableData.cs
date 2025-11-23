using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Ambermoon.Data.Legacy.Serialization.AmigaExecutable;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// This contains all relevant data from the executable.
    /// 
    /// The last RELOC32 hunk contains some offset mappings for the last
    /// data hunk which contains the relevant data. With the help of
    /// these offsets, we can safely locate the data.
    /// 
    /// There should be 15 relocation offsets for hunk 5 (which should
    /// be the last data hunk 0-based without END hunks).
    /// 
    /// If the first 12 offsets are added together the resulting
    /// offset (in bytes) points to the beginning of the section
    /// with codepage mappings which should be the first important
    /// data section.
    /// 
    /// The 13th and 15th offsets are longer ones. The 14th should
    /// be a small 4 byte offset.
    /// 
    /// If the 13th offset is added this points right after the
    /// rune code mapping and those additional 10 bytes:
    /// 03 00 1C 1C 1A 1A 19 1B 18 18
    /// 
    /// The next section starts with two identical dwords and then
    /// the first text section starts. The mentioned 14th offset
    /// points to the second dword.
    /// 
    /// After adding all 15 offsets together and adding 262 we
    /// are at the glyph data section. But if this address is not
    /// dword-aligned we have to do so (offset += (4 - offset % 4)).
    /// 
    /// =====================
    /// The first data hunk contains some UI graphics.
    /// 
    /// The second data hunk contains palettes, texts,
    /// cursors, glyphs and their mappings,
    /// button graphics and items.
    /// </summary>
    public class ExecutableData
    {
        // TODO: at offste 0x79be in data hunk 1 (german 1.05) there is the info about combat backgrounds
        // descibed here https://gitlab.com/ambermoon/research/-/wikis/Amberfiles/Combat_background.
        // there are 128 bytes (32 * 4). 4 bytes for 15 2D backgrounds and 4 bytes for 15 3D backgrounds.
        // first byte is the combat background graphic index (1-based) and then 3 palette indices (daytime dependent).

        // Before that there are 3*14 bytes. A 14 byte chunk for each world (Lyramion, Forest Moon, Morag).
        // - ushort mapsPerRow
        // - ushort mapsPerColumn
        // - ushort tilesPerMapRow (= mapWidth)
        // - ushort tilesPerMapColumn (= mapHeight)
        // - ushort firstWorldMapIndex (1, 300, 513)
        // - ushort nightEndTime (6)
        // - ushort dayEndTime (18)

        public const int DigitGlyphOffset = 48; // Glyph 48 is '0'
        public string DataVersionString { get; }
        public string DataInfoString { get; }
        public UIGraphics UIGraphics { get; }
        public DigitGlyphs DigitGlyphs { get; }
        public Glyphs Glyphs { get; }
        public Cursors Cursors { get; }
        public FileList FileList { get; }
        public WorldNames WorldNames { get; }
        public Messages Messages { get; }
        public AutomapNames AutomapNames { get; }
        public OptionNames OptionNames { get; }
        public SongNames SongNames { get; }
        public SpellTypeNames SpellTypeNames { get; }
        public SpellNames SpellNames { get; }
        public LanguageNames LanguageNames { get; }
        public ClassNames ClassNames { get; }
        public RaceNames RaceNames { get; }
        public SkillNames SkillNames { get; }
        public AttributeNames AttributeNames { get; }
        public ItemTypeNames ItemTypeNames { get; }
        public ConditionNames ConditionNames { get; }
        public UITexts UITexts { get; }
        public Buttons Buttons { get; }
        public ItemManager ItemManager { get; }
        public Graphic[] BuiltinPalettes { get; } = new Graphic[3];
        public Graphic[] SkyGradients { get; } = new Graphic[9];
        public Graphic[] DaytimePaletteReplacements { get; } = new Graphic[6];

        static T Read<T>(IDataReader[] dataReaders, int readerIndex)
        {
            var type = typeof(T);
            var dataReader = dataReaders[readerIndex];

            return (T)Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance,
                null, [dataReader], null);
        }

        static uint ReadOffsetAfterByteSequence(IDataReader dataReader, params byte[] sequence)
        {
            long index = dataReader.FindByteSequence(sequence, dataReader.Position);

            if (index == -1)
                throw new AmbermoonException(ExceptionScope.Data, "Could not find byte sequence.");

            dataReader.Position = (int)index + sequence.Length;

            return dataReader.ReadDword();
        }

        /* Some interesting offsets:
         * 
         * 2nd data hunk
         * =============
         * 
         * Offsets are for German 1.05.
         * 
         * 0x7AA0: Palette indices for event pix (only 8 of 9)
         * 0x8085: Name of the dictionary file. From here on the relative
         *         text offsets will differ between german and english version!
         */

        public static ExecutableData FromGameData(ILegacyGameData gameData)
        {
            var hunks = gameData.Files.TryGetValue("AM2_CPU", out var exe)
                ? AmigaExecutable.Read(exe.Files[1])
                : [];

            gameData.Files.TryGetValue("Text.amb", out var textAmb);
            gameData.Files.TryGetValue("Objects.amb", out var objectsAmb);
            gameData.Files.TryGetValue("Button_graphics", out var buttonGraphics);

            int textAmbPosition = textAmb == null ? 0 : textAmb.Files[1].Position;
            int objectsAmbPosition = objectsAmb == null ? 0 : objectsAmb.Files[1].Position;
            int buttonGraphicsPosition = buttonGraphics == null ? 0 : buttonGraphics.Files[1].Position;

            try
            {
                return new ExecutableData(hunks, textAmb?.Files[1], objectsAmb?.Files[1], buttonGraphics?.Files[1]);
            }
            finally
            {
                if (textAmb != null)
                    textAmb.Files[1].Position = textAmbPosition;
                if (objectsAmb != null)
                    objectsAmb.Files[1].Position = objectsAmbPosition;
                if (buttonGraphics != null)
                    buttonGraphics.Files[1].Position = buttonGraphicsPosition;
            }
        }

        public ExecutableData(List<AmigaExecutable.IHunk> hunks)
            : this(hunks, null, null, null)
        {

        }

        public ExecutableData(List<AmigaExecutable.IHunk> hunks, IDataReader textAmbReader, IDataReader objectsAmbReader,
            IDataReader buttonGraphicsReader)
        {
            var firstCodeHunk = hunks?.FirstOrDefault(h => h.Type == AmigaExecutable.HunkType.Code);

            if (firstCodeHunk == null && textAmbReader == null)
            {
                DataInfoString = "Unknown data version";
                return;
            }

            var codeReader = firstCodeHunk == null ? null : new DataReader((firstCodeHunk as AmigaExecutable.Hunk?)?.Data);
            TextContainer textContainer = null;

            if (textAmbReader != null)
            {
                textAmbReader.Position = 0;
                textContainer = new TextContainer();
                var textContainerReader = new TextContainerReader();
                textContainerReader.ReadTextContainer(textContainer, textAmbReader, true);
                DataVersionString = textContainer.VersionString;
                DataInfoString = textContainer.DateAndLanguageString;
            }
            else
            {
                codeReader.Position = 6;
                DataVersionString = codeReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                DataInfoString = codeReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
            }

            DataReader[] dataHunkReaders = null;
            int dataHunkIndex = 0;

            if (hunks.Count > 0)
            {
                dataHunkReaders = [.. hunks.Where(h => h.Type == HunkType.Data).Select(h => new DataReader(((Hunk)h).Data))];

                // Note: First 160 bytes are copper commands which can be dynamically filled
                // to move data to some Amiga registers. The area is permanently used by the
                // copper.
                dataHunkReaders[dataHunkIndex].Position = 160;

                UIGraphics = Read<UIGraphics>(dataHunkReaders, dataHunkIndex);
                // Here follows the note period table for Sonic Arranger (110 words)
                // Then the vibrato table (258 bytes)
                // Then track data and many more SA tables

                dataHunkIndex = 1;
                var reader = dataHunkReaders[1];
                codeReader.Position = 115000;
                reader.Position = (int)ReadOffsetAfterByteSequence(codeReader, 0x34, 0x3c, 0x03, 0xe7, 0x41, 0xf9);
                DigitGlyphs = Read<DigitGlyphs>(dataHunkReaders, dataHunkIndex);

                // TODO ...

                // NOTE: There is a new AM2_CPU format introduced to ease supporting new languages.
                // Instead of storing the glyph data and character mappings somewhere inside data
                // hunks which contain a lot of other stuff (including arbitrary sized texts), there
                // is a new data hunk at the end solely for glyph-related data.
                //
                // Original AM2_CPUs have 3 data hunks, now there are 4.
                bool newExeFormat = dataHunkReaders.Length == 4;

                if (newExeFormat)
                {
                    codeReader.Position += 27500;

                    codeReader.Position = (int)codeReader.FindByteSequence([0x48, 0xe7, 0x00, 0xc0, 0x41, 0xf9], codeReader.Position) + 180;
                    reader.Position = (int)ReadOffsetAfterByteSequence(codeReader, 0x48, 0xe7, 0x00, 0xc0, 0x41, 0xf9);
                    Cursors = Read<Cursors>(dataHunkReaders, dataHunkIndex);

                    var glyphHunkReader = dataHunkReaders[^1];

                    glyphHunkReader.Position = 2 * 224; // 224 chars (256 - 32) are mapped for normal and rune glyphs

                    Glyphs = Read<Glyphs>(dataHunkReaders, dataHunkReaders.Length - 1);
                }
                else
                {
                    codeReader.Position += 29000;

                    reader.Position = (int)ReadOffsetAfterByteSequence(codeReader, 0x22, 0x48, 0x41, 0xf9);
                    Glyphs = Read<Glyphs>(dataHunkReaders, dataHunkIndex);
                    Cursors = Read<Cursors>(dataHunkReaders, dataHunkIndex);
                }

                // Here are the 3 builtin palettes for primary UI, automap and secondary UI.
                for (int i = 0; i < 3; ++i)
                {
                    BuiltinPalettes[i] = GraphicProvider.ReadPalette(dataHunkReaders[dataHunkIndex]);
                }

                // Then 9 vertical color gradients used for skies are stored. They are stored
                // as 16 bit XRGB colors and not color indices!
                // The first 3 skies are for Lyramion, the next 3 for the forest moon and the last
                // 3 for Morag. The first sky is night, the second twilight and the third day.
                // Transitions blend night with twilight or day with twilight.
                var skyGraphicInfo = new GraphicInfo
                {
                    Alpha = false,
                    GraphicFormat = GraphicFormat.XRGB16,
                    Width = 1,
                    Height = 72
                };
                var graphicReader = new GraphicReader();

                for (int i = 0; i < 9; ++i)
                {
                    var sky = SkyGradients[i] = new Graphic();
                    graphicReader.ReadGraphic(sky, dataHunkReaders[dataHunkIndex], skyGraphicInfo);
                }

                // After the 9 sky gradients there are 6 partial palettes (16 colors).
                // Two of them per world (first for night, second for twilight).
                // They are also blended together (the first 16 colors of the map's palette is
                // used for day) and then replaces the first 16 colors of the map's palette.
                var daytimePaletteReplacementInfo = new GraphicInfo
                {
                    Alpha = false,
                    GraphicFormat = GraphicFormat.XRGB16,
                    Width = 1,
                    Height = 16
                };
                for (int i = 0; i < 6; ++i)
                {
                    var replacement = DaytimePaletteReplacements[i] = new Graphic();
                    graphicReader.ReadGraphic(replacement, dataHunkReaders[dataHunkIndex], daytimePaletteReplacementInfo);
                }

                // TODO: Here the spell infos for all 210 possible spells follow (5 byte each).
                // TODO: Then 1024 words follow. Most likely some 3D stuff (cos/sin values). 101, 201, 302, 402, 503, ...
                // TODO: Then 1025 words follow. Also 3D stuff I guess. 0, 1, 1, 2, 3, 3, 4, 4, 5, ...
                // TODO: Then the class exp factors follow (11 words).
                // TODO: Then for each travel type a number of additional ticks per step follows (11 bytes). 0 means move directly, 1 means pause for 1 additional tick after movement, etc.
                // TODO: Then for each travel type a number follows which specifies how many steps are needed to increase the time by 5 minutes (11 bytes).
                // TODO: Then for each travel type the music index follows (11 bytes).
                // TODO: Then a fill byte to get to a even word boundary.
                // TODO: Then there are 3 world infos. They contain 7 words each: MapsPerRow, MapsPerCol, MapWidth, MapHeight, MapIndexOffset, DayBeginHour, DayEndHour (not sure about the latter two).
                // TODO: Then 2x16 combat background infos follow. First 16 for 2D, then 16 for 3D. Each info has 4 bytes. Image index and then 3 palette indices for day, twilight and night.
                // TODO: Then the 9 character heights for the races follow (word each). The first (human -> 180) is also the reference height.

                // TODO ...

                const string search = "Amberfiles/";
                dataHunkReaders[1].Position = (int)dataHunkReaders[1].FindString(search, dataHunkReaders[1].Position) + search.Length + 54;
            }

            if (textContainer == null)
            {
                FileList = Read<FileList>(dataHunkReaders, dataHunkIndex);
                WorldNames = Read<WorldNames>(dataHunkReaders, dataHunkIndex);
                Messages = Read<Messages>(dataHunkReaders, dataHunkIndex);
                AutomapNames = Read<AutomapNames>(dataHunkReaders, dataHunkIndex);
                OptionNames = Read<OptionNames>(dataHunkReaders, dataHunkIndex);
                SongNames = Read<SongNames>(dataHunkReaders, dataHunkIndex);
                SpellTypeNames = Read<SpellTypeNames>(dataHunkReaders, dataHunkIndex);
                SpellNames = Read<SpellNames>(dataHunkReaders, dataHunkIndex);
                LanguageNames = Read<LanguageNames>(dataHunkReaders, dataHunkIndex);
                ClassNames = Read<ClassNames>(dataHunkReaders, dataHunkIndex);
                RaceNames = Read<RaceNames>(dataHunkReaders, dataHunkIndex);
                SkillNames = Read<SkillNames>(dataHunkReaders, dataHunkIndex);
                AttributeNames = Read<AttributeNames>(dataHunkReaders, dataHunkIndex);
                SkillNames.AddShortNames(dataHunkReaders[dataHunkIndex]);
                AttributeNames.AddShortNames(dataHunkReaders[dataHunkIndex]);
                ItemTypeNames = Read<ItemTypeNames>(dataHunkReaders, dataHunkIndex);
                ConditionNames = Read<ConditionNames>(dataHunkReaders, dataHunkIndex);
                UITexts = Read<UITexts>(dataHunkReaders, dataHunkIndex);
            }
            else
            {
                if (dataHunkReaders != null)
                {
                    var hunkReader = dataHunkReaders[1];
                    int dataHunks = 0;
                    AmigaExecutable.Reloc32Hunk? relocHunk = null;
                    foreach (var hunk in hunks)
                    {
                        if (hunk.Type == AmigaExecutable.HunkType.Data)
                            ++dataHunks;

                        if (hunk.Type == AmigaExecutable.HunkType.RELOC32 && dataHunks == 2)
                        {
                            relocHunk = (AmigaExecutable.Reloc32Hunk)hunk;
                            break;
                        }
                    }

                    if (relocHunk == null)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid executable file.");

                    int FindHunk(uint offset)
                    {
                        foreach (var relocTable in relocHunk.Value.Entries)
                        {
                            if (relocTable.Value.Contains(offset))
                                return (int)relocTable.Key;
                        }

                        return -1;
                    }

                    FileList = new FileList();

                    while (hunkReader.PeekWord() != 0)
                    {
                        int hunkIndex = FindHunk((uint)hunkReader.Position);

                        if (hunkIndex == -1)
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid executable file.");

                        uint offset = hunkReader.ReadDword();

                        var fileNameReader = new DataReader(((Hunk)hunks[hunkIndex]).Data);
                        fileNameReader.Position = (int)offset;
                        FileList.ReadFileEntry(fileNameReader);
                    }
                }

                WorldNames = new WorldNames(textContainer.WorldNames);
                Messages = new Messages(textContainer.FormatMessages, textContainer.Messages);
                AutomapNames = new AutomapNames(textContainer.AutomapTypeNames);
                OptionNames = new OptionNames(textContainer.OptionNames);
                SongNames = new SongNames(textContainer.MusicNames);
                SpellTypeNames = new SpellTypeNames(textContainer.SpellClassNames);
                SpellNames = new SpellNames(textContainer.SpellNames);
                LanguageNames = new LanguageNames(textContainer.LanguageNames);
                ClassNames = new ClassNames(textContainer.ClassNames);
                RaceNames = new RaceNames(textContainer.RaceNames);
                SkillNames = new SkillNames(textContainer.SkillNames, textContainer.SkillShortNames);
                AttributeNames = new AttributeNames(textContainer.AttributeNames, textContainer.AttributeShortNames);
                ItemTypeNames = new ItemTypeNames(textContainer.ItemTypeNames);
                ConditionNames = new ConditionNames(textContainer.ConditionNames);
                UITexts = new UITexts(textContainer.UITexts);
            }

            if (buttonGraphicsReader != null)
            {
                buttonGraphicsReader.Position = 0;
                Buttons = new Buttons(buttonGraphicsReader);
            }
            else
            {
                Buttons = Read<Buttons>(dataHunkReaders, dataHunkIndex);
            }

            if (objectsAmbReader == null)
            {
                int itemCount = dataHunkReaders[dataHunkIndex].ReadWord();
                if (dataHunkReaders[dataHunkIndex].ReadWord() != itemCount)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid item data.");

                var itemReader = new ItemReader();
                var items = new Dictionary<uint, Item>();

                for (uint i = 1; i <= itemCount; ++i) // in original Ambermoon there are 402 items
                    items.Add(i, Item.Load(i, itemReader, dataHunkReaders[dataHunkIndex]));

                ItemManager = new ItemManager(items);
            }
            else
            {
                objectsAmbReader.Position = 0;
                int itemCount = objectsAmbReader.ReadWord();
                var itemReader = new ItemReader();
                var items = new Dictionary<uint, Item>();

                for (uint i = 1; i <= itemCount; ++i) // in original Ambermoon there are 402 items
                    items.Add(i, Item.Load(i, itemReader, objectsAmbReader));

                ItemManager = new ItemManager(items);
            }
        }
    }
}
