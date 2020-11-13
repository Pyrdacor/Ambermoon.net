using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Linq;

namespace Ambermoon.Data.Legacy.Characters
{
    public class MonsterReader : CharacterReader, IMonsterReader
    {
        readonly IGameData gameData;
        readonly IGraphicProvider graphicProvider;

        public MonsterReader(IGameData gameData, IGraphicProvider graphicProvider)
        {
            this.gameData = gameData;
            this.graphicProvider = graphicProvider;
        }

        public void ReadMonster(Monster monster, IDataReader dataReader)
        {
            ReadCharacter(monster, dataReader);

            // Attributes, abilities, LP and SP is special for monsters.
            // If the max value is 99, the max value is set to current value.
            foreach (var attribute in Enum.GetValues<Attribute>())
            {
                var value = monster.Attributes[attribute];

                if (value.MaxValue == 99)
                    value.MaxValue = value.CurrentValue;
            }
            foreach (var ability in Enum.GetValues<Ability>())
            {
                var value = monster.Abilities[ability];

                if (value.MaxValue == 99)
                    value.MaxValue = value.CurrentValue;
            }
            if (monster.HitPoints.MaxValue == 99)
                monster.HitPoints.MaxValue = monster.HitPoints.CurrentValue;
            if (monster.SpellPoints.MaxValue == 99)
                monster.SpellPoints.MaxValue = monster.SpellPoints.CurrentValue;
            // TODO: maybe this should be done in game and not here
            // TODO: some values seem to be a bit different (use monster knowledge on skeleton for examples)

            for (int i = 0; i < 8; ++i)
            {
                monster.Animations[i] = new Monster.Animation
                {
                    FrameIndices = dataReader.ReadBytes(32)
                };
            }

            foreach (var animation in monster.Animations)
                animation.UsedAmount = dataReader.ReadByte();

            monster.UnknownAdditionalBytes1 = dataReader.ReadBytes(16); // TODO
            monster.MonsterPalette = dataReader.ReadBytes(32);
            monster.UnknownAdditionalBytes2 = dataReader.ReadBytes(2); // TODO
            monster.FrameWidth = dataReader.ReadWord();
            monster.FrameHeight = dataReader.ReadWord();
            monster.MappedFrameWidth = dataReader.ReadWord();
            monster.MappedFrameHeight = dataReader.ReadWord();

            monster.CombatGraphic = LoadGraphic(monster);
        }

        Graphic LoadGraphic(Monster monster)
        {
            var file = gameData.Files["Monster_gfx.amb"].Files[(int)monster.CombatGraphicIndex];
            file.Position = 0;
            var graphic = new Graphic();
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                Width = (int)monster.FrameWidth,
                Height = (int)monster.FrameHeight,
                GraphicFormat = GraphicFormat.Palette5Bit,
                Alpha = true,
                PaletteOffset = 0
            };
            int numFrames = file.Size / ((graphicInfo.Width * graphicInfo.Height * 5 + 7) / 8); // TODO: is this inside monster data?
            var compoundGraphic = new Graphic(numFrames * (int)monster.MappedFrameWidth, (int)monster.MappedFrameHeight, 0);
            for (int i = 0; i < numFrames; ++i)
            {
                graphicReader.ReadGraphic(graphic, file, graphicInfo);
                compoundGraphic.AddOverlay((uint)i * monster.MappedFrameWidth, 0, graphic.CreateScaled((int)monster.MappedFrameWidth, (int)monster.MappedFrameHeight));
            }
            for (int i = 0; i < compoundGraphic.Data.Length; ++i)
                compoundGraphic.Data[i] = monster.MonsterPalette[compoundGraphic.Data[i] & 0x1f];
            return compoundGraphic;
        }
    }
}
