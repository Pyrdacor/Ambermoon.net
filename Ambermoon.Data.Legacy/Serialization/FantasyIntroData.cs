using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class FantasyIntroData : IFantasyIntroData
    {
        static readonly Position[] WandSparkPositions = new[]
        {
            new Position(38 + 125, 13 + 70), new Position(40 + 125, 14 + 70),
			new Position(41 + 125, 13 + 70), new Position(43 + 125, 14 + 70),
			new Position(45 + 125, 16 + 70), new Position(41 + 125, 13 + 70),
			new Position(39 + 125, 14 + 70), new Position(37 + 125, 14 + 70),
			new Position(38 + 125, 17 + 70), new Position(44 + 125, 16 + 70),
			new Position(53 + 125, 22 + 70), new Position(56 + 125, 28 + 70),
			new Position(58 + 125, 33 + 70), new Position(58 + 125, 36 + 70),
			new Position(58 + 125, 37 + 70), new Position(55 + 125, 30 + 70),
			new Position(54 + 125, 29 + 70), new Position(50 + 125, 23 + 70),
			new Position(45 + 125, 18 + 70)
        };

        static readonly Position[] WandPositions = new[]
        {
            new Position(38, 13), new Position(40, 14), new Position(41, 13),
            new Position(43, 14), new Position(45, 16), new Position(41, 13),
            new Position(39, 14), new Position(37, 14), new Position(38, 17),
            new Position(44, 16), new Position(53, 22), new Position(56, 28),
            new Position(58, 33), new Position(58, 36), new Position(58, 37),
            new Position(55, 30), new Position(54, 29), new Position(50, 23),
            new Position(45, 18)
        };

        static readonly int[] WritingSparkImageIndex = new[]
        {
            6, 3, 0, 3, 6, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 1, 4, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            8, 5, 2, 5, 8, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 6, 3, 3, 0, 0, 3, 3, 3, 6, 6, 6, -1, 0, 0, 0,
            7, 7, 4, 4, 0, 0, 0, 4, 4, 7, 7, -1, 0, 0, 0, 0,
            8, 8, 8, 5, 5, 5, 0, 0, 0, 5, 5, 5, 8, 8, 8, -1,
            3, 0, 3, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 0, 5, -1, 0,64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        static readonly int[] Spark0ColorIndices = new[]
        {
            9, 11, 13, 14, 13, 12, 12, 11, 10, 10, 9, 9, 10, 14, 12, 10,
            26, 28, 24, 26, 28, 14, 27, 26, 25, 14, 20, 9, -1
        };

        static readonly int[] Spark1ColorIndices = new[]
        {
            9, 10, 11, 12, 11, 10, 9, 9, 9, 11, 14, 13, 12, 11, 10, 0,
            9, 10, 11, 12, 11, 10, 9, 9, 9, 11, 14, -1, -2, -3, 0
        };

        record SparkSpriteDummy
        {
            public FantasyIntroCommand UpdateCommand { get; init; }
            public int X { get; set; }
            public int Y { get; set; }
            public int YStep { get; set; } // 1/16th of a line
            public uint ColorIndex { get; set; }
            public int CurrentStep { get; set; }

            public bool NextStep(uint frames, Queue<FantasyIntroAction> actions, int index)
            {
                CurrentStep += YStep;

                if (CurrentStep == 16)
                {
                    if (++Y >= 256)
                        X = -1;

                    Update(frames, actions, index);
                }

                return X >= 0;
            }

            public void Update(uint frames, Queue<FantasyIntroAction> actions, int index)
            {
                actions.Enqueue(new FantasyIntroAction(frames, UpdateCommand, index, X, Y));
            }
        }

        readonly Queue<FantasyIntroAction> actions = new();
        readonly List<Graphic> fantasyIntroPalettes = new();
        readonly Dictionary<FantasyIntroGraphic, Graphic> graphics = new();
        static readonly Dictionary<FantasyIntroGraphic, byte> graphicPalettes = new()
        {
            { FantasyIntroGraphic.FairySparks, 0 },
            { FantasyIntroGraphic.Fairy, 0 },
            { FantasyIntroGraphic.Background, 1 },
            { FantasyIntroGraphic.WritingSparks, 0 },
            { FantasyIntroGraphic.Writing, 0 },
        };
        static GraphicInfo paletteGraphicInfo = new()
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };

        public Queue<FantasyIntroAction> Actions => new(actions);
        public IReadOnlyList<Graphic> FantasyIntroPalettes => fantasyIntroPalettes.AsReadOnly();
        public static IReadOnlyDictionary<FantasyIntroGraphic, byte> GraphicPalettes => graphicPalettes;
        public IReadOnlyDictionary<FantasyIntroGraphic, Graphic> Graphics => graphics;

        public FantasyIntroData(GameData gameData)
        {
            var fantasyIntroHunks = AmigaExecutable.Read(gameData.Files["Fantasy_intro"].Files[1])
                .Where(h => h.Type == AmigaExecutable.HunkType.Data).Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var graphicReader = new GraphicReader();

            #region Hunk 0 - Palettes and graphics

            var hunk0 = fantasyIntroHunks[0];

            Graphic LoadPalette()
            {
                var paletteGraphic = new Graphic();
                graphicReader.ReadGraphic(paletteGraphic, hunk0, paletteGraphicInfo);
                return paletteGraphic;
            }

            for (int i = 0; i < 2; ++i)
                fantasyIntroPalettes.Add(LoadPalette());

            AddGraphic(FantasyIntroGraphic.FairySparks, 32, 15, true);

            var fairyGraphic = new Graphic(384, 284, 0);

            for (uint i = 0; i < 23; ++i) // 23 frames
            {
                var graphic = LoadGraphic(64, 71, true, GraphicFormat.AttachedSprite);
                fairyGraphic.AddOverlay((i % 6) * 64, (i / 6) * 71, graphic, false);
            }

            graphics.Add(FantasyIntroGraphic.Fairy, fairyGraphic);

            // unknown bytes (maybe copper instructions?)

            hunk0.Position = 0xD38C;
            AddGraphic(FantasyIntroGraphic.Background, 320, 256, false);
            AddGraphic(FantasyIntroGraphic.WritingSparks, 32, 108, true);
            AddGraphic(FantasyIntroGraphic.Writing, 208, 83, true);

            Graphic LoadGraphic(int width, int height, bool alpha, GraphicFormat graphicFormat = GraphicFormat.Palette5Bit)
            {
                var graphicInfo = new GraphicInfo
                {
                    Width = width,
                    Height = height,
                    GraphicFormat = graphicFormat,
                    PaletteOffset = 0,
                    Alpha = alpha
                };
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, hunk0, graphicInfo);
                return graphic;
            }

            void AddGraphic(FantasyIntroGraphic fantasyIntroGraphic, int width, int height, bool alpha)
            {
                var graphic = LoadGraphic(width, height, alpha);
                graphics.Add(fantasyIntroGraphic, graphic);
            }

            // unknown data

            hunk0.Position = 0x1CE22;
            var positionData = new DataReader(hunk0.ReadBytes(761 * 4)); // 761 entries, each entry has x and y as word each

            #endregion

            #region Progress

            actions.Enqueue(new(0, FantasyIntroCommand.FadeIn));

            uint frames = 0;
            int fairyMode = 0; // 0: Move, 1: Special animation, 2: Wait in place (idle animation)
            int endTimer = 0;
            int spriteDelay = 0;
            int spriteIndex = 0;
            int writingPosition = -1;
            int spark0Delay = 0;
            int spriteSparkPosIndex = 0; // -1?
            int currentFairyX = -1;
            int currentFairyY = -1;
            int sparkPosX = 0;
            int sparkPosY = 0;
            ushort randomState = 17;
            var spark0Sprites = new SparkSpriteDummy[96];
            var spark1Sprites = new SparkSpriteDummy[512];

            for (int i = 0; i < 96; ++i)
            {
                spark0Sprites[i] = new SparkSpriteDummy() { X = -1, UpdateCommand = FantasyIntroCommand.UpdateSparkLine };
                spark1Sprites[i] = new SparkSpriteDummy() { X = -1, UpdateCommand = FantasyIntroCommand.UpdateSparkStar };
            }

            for (int i = 96; i < 512; ++i)
            {
                spark1Sprites[i] = new SparkSpriteDummy() { X = -1, UpdateCommand = FantasyIntroCommand.UpdateSparkStar };
            }

            uint Random()
            {
                uint next = randomState * 53527u;
                randomState = (ushort)(next & 0xFFFF);
                return (next >> 8) & 0x7FFFu;
            }

            void AddSpark0(int x, int y)
            {
                if (x >= 0 && x < 312 && y >= 134 && y < 256)
                {
                    int index = spark0Sprites.ToList().FindIndex(s => s.X < 0);
                    
                    if (index != -1)
                    {
                        var sparkSprite = spark0Sprites[index];
                        sparkSprite.X = x;
                        sparkSprite.Y = y;
                        sparkSprite.CurrentStep = 0;

                        var r = Random();
                        sparkSprite.YStep = (int)((2 * 16) + (r & 0x7));
                        r = Random();
                        sparkSprite.ColorIndex = (r % 11) << 1;

                        sparkSprite.Update(frames, actions, index);

                        var color = Spark0ColorIndices[sparkSprite.ColorIndex];
                        if (color < 0)
                            color = Spark0ColorIndices[0];
                        actions.Enqueue(new(frames, FantasyIntroCommand.SetSparkLineColor, index, color));
                    }
                }
            }

            void AddSpark1(int x, int y)
            {
                int index = spark1Sprites.ToList().FindIndex(s => s.X < 0);

                if (index != -1)
                {
                    var sparkSprite = spark0Sprites[index];
                    var r = Random();
                    sparkSprite.X = x + (int)((r & 0x60) >> 5);
                    sparkSprite.Y = y;
                    sparkSprite.YStep = 8 + (int)(r & 0xf);
                    sparkSprite.ColorIndex = (r & 0xe00) != 0 ? 0u : 16u;
                    sparkSprite.CurrentStep = 0;

                    sparkSprite.Update(frames, actions, index);

                    var color = Spark1ColorIndices[sparkSprite.ColorIndex];
                    if (color < 0)
                        color = Spark1ColorIndices[0];
                    actions.Enqueue(new(frames, FantasyIntroCommand.SetSparkStarColor, index, color));
                }
            }

            void DrawFairySparks0()
            {
                var r = Random();
                var spawnDelay = r & 0x3;
                int index = 0;

                foreach (var spark in spark0Sprites)
                {
                    if (spark.NextStep(frames, actions, index))
                    {
                        var color = Spark0ColorIndices[spark.ColorIndex];
                        if (color < 0)
                        {
                            color = Spark0ColorIndices[0];
                            spark.ColorIndex = 1;
                        }
                        else
                            ++spark.ColorIndex;
                        spark.Update(frames, actions, index);
                        actions.Enqueue(new(frames, FantasyIntroCommand.SetSparkLineColor, index, color));
                        actions.Enqueue(new(frames, FantasyIntroCommand.DrawSparkLine, index));
                        if (--spawnDelay < 0)
                        {
                            spawnDelay += 3;
                            AddSpark1(spark.X, spark.Y);
                        }
                    }

                    ++index;
                }
            }

            void DrawFairySparks1()
            {
                int index = 0;

                foreach (var spark in spark1Sprites)
                {
                    if (spark.NextStep(frames, actions, index))
                    {
                        var color = Spark1ColorIndices[spark.ColorIndex];
                        if (color == 0)
                        {
                            spark.X = -1;
                            spark.Update(frames, actions, index);
                            continue;
                        }
                        ++spark.ColorIndex;
                        if (color < 0)
                        {
                            if (spark.Y <= 250)
                            {
                                actions.Enqueue(new(frames, FantasyIntroCommand.SetSparkStarColor, index, color));
                                actions.Enqueue(new(frames, FantasyIntroCommand.DrawSparkStar, index));
                            }
                        }
                        else
                        {
                            actions.Enqueue(new(frames, FantasyIntroCommand.DrawSparkDot, index));
                        }
                    }

                    ++index;
                }
            }

            void DrawWriting()
            {
                if (writingPosition >= 0)
                {
                    if (writingPosition >= 64 && writingPosition < 272)
                    {
                        actions.Enqueue(new(frames, FantasyIntroCommand.AddWritingPart));
                    }

                    for (int i = 0; i < 24; ++i)
                    {
                        var r = Random();
                        long y = 140 + (r % 88);
                        r = Random();
                        long x = writingPosition + (r & 0x7) - 4;
                        //long i = r & 0xe0;
                        /*find_if (s : WritingSparkItem, s.x < 0) {
                        s.x = x
                        s.y = y
                        s.i = i*/
                    }

                    writingPosition += 4;

                    if (writingPosition >= 276)
                    {
                        writingPosition = -1;
                        fairyMode = 0;

                    }
                }
            }

            int cycleCounter = 0;

            void Cycle()
            {
                ++cycleCounter;

                if (fairyMode == 0 && endTimer <= 0)
                {
                    int x = unchecked((short)positionData.ReadWord());
                    int y = unchecked((short)positionData.ReadWord());

                    if (x == 12345)
                    {
                        if (y >= 0)
                        {
                            fairyMode = 1;
                            spriteSparkPosIndex = -1;
                            actions.Enqueue(new(frames, FantasyIntroCommand.PlayFairyAnimation));
                        }
                        else
                        {
                            endTimer = 133; // 133 frames till end
                        }
                    }
                    else
                    {
                        currentFairyX = x;
                        currentFairyY = y;
                        actions.Enqueue(new(frames, FantasyIntroCommand.MoveFairy, x, y));
                    }
                }

                if (--spriteDelay < 0)
                {
                    spriteDelay += 3;
                    if (++spriteIndex == 4)
                        spriteIndex = 0;
                    else if (spriteIndex == 23)
                    {
                        spriteIndex = 0;
                        fairyMode = 2;
                        writingPosition = -110;
                    }
                    if (fairyMode == 1)
                    {
                        if (spriteSparkPosIndex >= 0)
                            ++spriteSparkPosIndex;
                        if (spriteIndex == 3)
                        {
                            spriteIndex = 4;
                            spriteSparkPosIndex = 0;
                        }
                    }
                }

                if (--spark0Delay < 0)
                {
                    if (fairyMode == 0)
                        spark0Delay += 2;
                    else
                        spark0Delay += 32;
                    AddSpark0(currentFairyX + 19, currentFairyY + 17);
                }

                if (fairyMode == 1 && spriteSparkPosIndex >= 0 && spriteDelay == 0)
                {
                    var sparkPosition = WandSparkPositions[spriteSparkPosIndex];
                    int sparkX = currentFairyX + sparkPosition.X - 125;
                    int sparkY = currentFairyY + sparkPosition.Y - 70;
                    if (spriteIndex == 16)
                    {
                        sparkPosX = sparkX;
                        sparkPosY = sparkY;
                    }
                    AddSpark0(sparkX, sparkY);
                }

                if (sparkPosX >= 0)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        uint r = Random();
                        int x = sparkPosX + (int)(r & 0xf);
                        int y = sparkPosY + (int)((r & 0xf0) >> 4);
                        AddSpark0(x, y);
                    }
                    sparkPosX += 6;
                    sparkPosY += 1;
                    if (sparkPosX >= 160)
                        sparkPosX = -1;
                }

                if (writingPosition < -1)
                {
                    if (++writingPosition == -1)
                        writingPosition = 60;
                }
            }

            for (int i = 0; i < 32; ++i)
            {
                Cycle();
                ++frames;
            }

            while (true)
            {
                Cycle();
                cycleCounter = 0;
                DrawFairySparks0();
                DrawFairySparks1();
                DrawWriting();
                while (cycleCounter < 2)
                {
                    ++frames;
                    Cycle();
                }

                ++frames;

                // TODO: Remove later
                if (frames == 2 * 761)
                    break;

                if (endTimer > 0)
                {
                    // TODO: Maybe decrease by 2 here? Won't reach exactly 0 then as it starts at 133 at the end.
                    endTimer -= 2;
                    if (endTimer >= 16 && endTimer < 8)
                        actions.Enqueue(new(frames, FantasyIntroCommand.FadeOut));
                    else if (endTimer <= 0)
                        break; // Finished
                }
            }

            #endregion
        }
    }
}
