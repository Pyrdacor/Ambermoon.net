using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    internal class CombatGraphicData
    {
        public ImageList BattleFieldIcons { get; private set; } = new();
        public Dictionary<CombatGraphicIndex, Image> Images { get; private set; } = new();

        #region Serialization

        public static CombatGraphicData Deserialize(IDataReader dataReader)
        {
            var combatGraphicData = new CombatGraphicData();
            var graphicReader = new GraphicReader();

            foreach (var combatGraphic in CombatGraphics.Info)
            {
                var info = combatGraphic.Value;

                if (combatGraphic.Key == CombatGraphicIndex.BattleFieldIcons)
                {
                    var battleFieldIcons = new List<Image>(36);
                    var iconGraphicInfo = new GraphicInfo
                    {
                        Width = 16,
                        Height = 14,
                        GraphicFormat = GraphicFormat.Palette5Bit,
                        Alpha = true,
                        PaletteOffset = 0
                    };
                    uint imageIndex = 0;

                    while (dataReader.Position < dataReader.Size)
                    {
                        var graphic = new Graphic();
                        graphicReader.ReadGraphic(graphic, dataReader, iconGraphicInfo);
                        battleFieldIcons.Add(new Image(imageIndex++, new[] { graphic }));
                    }

                    combatGraphicData.BattleFieldIcons = new(battleFieldIcons);
                }
                else
                {
                    var graphic = new Graphic();
                    var image = new Image((uint)combatGraphic.Key, ReadGraphics());

                    IEnumerable<Graphic> ReadGraphics()
                    {
                        for (int i = 0; i < info.FrameCount; ++i)
                        {
                            graphicReader.ReadGraphic(graphic, dataReader, info.GraphicInfo);
                            yield return graphic;
                        }
                    }

                    combatGraphicData.Images.Add(combatGraphic.Key, image);
                }
            }

            return combatGraphicData;
        }

        #endregion

    }
}
