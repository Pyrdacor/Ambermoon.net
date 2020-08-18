namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// This contains all relevant data from the executable.
    /// 
    /// TODO: This is work in progress!
    /// </summary>
    public class ExecutableData
    {
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
        public AbilityNames AbilityNames { get; }
        public AttributeNames AttributeNames { get; }
        public ItemTypeNames ItemTypeNames { get; }
        public AilmentNames AilmentNames { get; }
        public UITexts UITexts { get; }

        /// <summary>
        /// Loads all data. Should be positioned on the start
        /// if the second data hunk.
        /// </summary>
        /// <param name="dataReader"></param>
        public ExecutableData(IDataReader dataReader)
        {
            // TODO: For now we search the offset of the filelist manually
            //       until we decode all of the data.
            dataReader.Position = (int)dataReader.FindString("0Map_data.amb", 0) - 184;

            // TODO ...
            FileList = new FileList(dataReader);
            WorldNames = new WorldNames(dataReader);
            Messages = new Messages(dataReader);
            if (dataReader.ReadDword() != 0)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid executable data.");
            AutomapNames = new AutomapNames(dataReader);
            OptionNames = new OptionNames(dataReader);
            SongNames = new SongNames(dataReader);
            SpellTypeNames = new SpellTypeNames(dataReader);
            SpellNames = new SpellNames(dataReader);
            LanguageNames = new LanguageNames(dataReader);
            ClassNames = new ClassNames(dataReader);
            RaceNames = new RaceNames(dataReader);
            AbilityNames = new AbilityNames(dataReader);
            AttributeNames = new AttributeNames(dataReader);
            AbilityNames.AddShortNames(dataReader);
            AttributeNames.AddShortNames(dataReader);
            ItemTypeNames = new ItemTypeNames(dataReader);
            AilmentNames = new AilmentNames(dataReader);
            UITexts = new UITexts(dataReader);

            // TODO: There is a bunch of binary data (gfx maybe?)

            // TODO: Then finally the item data comes ...

            // TODO ...
        }
    }
}
