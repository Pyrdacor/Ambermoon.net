namespace Ambermoon.Data.GameDataRepository.Data
{
    public static class DataExtensions
    {
        private static readonly Dictionary<GameDataInfo, Dictionary<uint, Palette[]>> _monsterDefaultPalettesCache = new();

        private static Dictionary<uint, Palette[]> GetDefaultMonsterImagePalettes(GameDataRepository repository)
        {
            if (!_monsterDefaultPalettesCache.TryGetValue(repository.Info, out var palettes))
            {
                palettes = repository.GetDefaultMonsterImagePalettes();
                _monsterDefaultPalettesCache.Add(repository.Info, palettes);
            }

            return palettes;
        }

        public static byte[] GetImage(this GameDataRepository repository, IImageProvidingData imageProvidingData, out int width, out int height)
        {
            switch (imageProvidingData)
            {
                case ItemData item:
                    width = 16;
                    height = 16;
                    return repository.ItemImages[item.GraphicIndex].Frames[0].GetData(repository.ItemPalette);
                case PartyMemberData partyMember:
                    width = 32;
                    height = 34;
                    return repository.Portraits[partyMember.GraphicIndex].Frames[0].GetData(repository.PortraitPalette);
                case NpcData npc:
                    width = 32;
                    height = 34;
                    return repository.Portraits[npc.GraphicIndex].Frames[0].GetData(repository.PortraitPalette);
                case MonsterData monster:
                {
                    var palettes = GetDefaultMonsterImagePalettes(repository);
                    var palette = palettes[imageProvidingData.Index][0];
                    var monsterImage = repository.MonsterImages[monster.GraphicIndex].Frames[0];
                    width = monsterImage.Width;
                    height = monsterImage.Height;
                    return monsterImage.GetData(palette);
                }
                default:
                    throw new NotImplementedException("This is not implemented yet.");
            }
        }
    }
}
