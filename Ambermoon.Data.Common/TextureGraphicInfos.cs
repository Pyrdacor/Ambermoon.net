using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    public static class TextureGraphicInfos
    {
        public static readonly GraphicInfo WallGraphicInfo = new()
        {
            Alpha = false,
            GraphicFormat = GraphicFormat.Texture4Bit,
            Width = 128,
            Height = 80
        };

        public static GraphicInfo[] ObjectGraphicInfos => [.. ObjectGraphicFrameCountsAndSizes.Select(size =>
            new GraphicInfo
            {
                Alpha = true,
                GraphicFormat = GraphicFormat.Texture4Bit,
                Width = size.Key * size.Value.Width,
                Height = size.Value.Height,
            })];

        public static GraphicInfo[] OverlayGraphicInfos => [.. OverlayGraphicSizes.Select(size =>
            new GraphicInfo
            {
                Alpha = true,
                GraphicFormat = GraphicFormat.Texture4Bit,
                Width = size.Width,
                Height = size.Height,
            })];

        public static readonly Size[] OverlayGraphicSizes =
        [
            new(16, 80), // 1
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80),
            new(16, 80), // 10
            new(48, 80),
            new(48, 80),
            new(16, 80),
            new(16, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(16, 80),
            new(16, 80), // 20
            new(16, 80),
            new(16, 80),
            new(16, 16),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80),
            new(32, 80), // 30
            new(32, 80),
            new(64, 61),
            new(16, 32),
            new(16, 26),
            new(16, 16),
            new(16, 16),
            new(32, 32),
            new(32, 32),
            new(64, 45),
            new(32, 19), // 40
            new(16, 27),
            new(16, 80),
            new(16, 80),
            new(96, 69),
            new(64, 50),
            new(64, 46),
            new(32, 80),
            new(32, 80),
            new(48, 80),
            new(32, 80), // 50
            new(48, 80),
            new(64, 80),
            new(16, 23),
            new(16, 23),
            new(32, 15),
            new(16, 13),
            new(16, 13),
            new(64, 60),
            new(64, 60),
            new(64, 60), // 60
            new(64, 60),
            new(64, 60),
            new(64, 60),
            new(64, 60),
            new(64, 25),
            new(64, 25),
            new(32, 48),
            new(32, 32),
            new(32, 32),
            new(48, 48), // 70
            new(64, 64),
            new(32, 58),
            new(32, 32),
            new(64, 52),
            new(64, 80),
            new(64, 64),
            new(64, 64),
            new(64, 64),
            new(64, 64),
            new(64, 25), // 80
            new(64, 25),
            new(64, 25),
            new(64, 25),
            new(32, 30),
            new(32, 30),
            new(64, 80),
            new(64, 36),
            new(64, 36),
            new(64, 41),
            new(64, 41), // 90
            new(64, 41),
            // Advanced graphics
            new(16, 80),
            new(16, 80),
            new(64, 41),
            new(72, 80),
            new(32, 80),
            new(32, 80),
            new(64, 45),
            new(96, 72),
            new(16, 8), // 100
            new(16, 8),
            new(128, 13),
            new(64, 60),
			new(32, 16),
			new(64, 41),
			new(64, 41),
			new(64, 41),
			new(64, 25),
			new(16, 41),
			new(16, 41), // 110
		];
        /* NOTE: You can find these values programmatically like this:
            using Ambermoon.Data.Legacy;

            var gameData = new GameData();

            gameData.Load(@"path/to/advanced/Amberfiles/folder");

            var allInfos = new Dictionary<uint, List<Size>>();
            gameData.MapManager.Labdata.ToList().ForEach(labData =>
            {
                labData.Walls.SelectMany(wall => wall.Overlays ?? []).ToList().ForEach((overlay) =>
                {
                    if (!allInfos.ContainsKey(overlay.TextureIndex))
                        allInfos[overlay.TextureIndex] = [new((int)overlay.TextureWidth, (int)overlay.TextureHeight)];
                    else
                        allInfos[overlay.TextureIndex].Add(new((int)overlay.TextureWidth, (int)overlay.TextureHeight));
                });
            });

            var sortedInfos = allInfos.OrderBy(z => z.Key).ToList();

            foreach (var item in sortedInfos)
            {
                Console.WriteLine(item.Key);

                foreach (var info in item.Value.Distinct())
                    Console.WriteLine($"\t{info.Width}, {info.Height}");
            }
         */

        public static readonly KeyValuePair<int, Size>[] ObjectGraphicFrameCountsAndSizes =
        [
            new(1, new(80, 80)), // 1
            new(1, new(80, 76)),
            new(1, new(48, 69)),
            new(1, new(32, 36)),
            new(1, new(48, 36)),
            new(1, new(32, 26)),
            new(1, new(32, 30)),
            new(1, new(16, 12)),
            new(1, new(16, 11)),
            new(1, new(16, 42)), // 10
            new(1, new(32, 32)),
            new(1, new(16, 8)),
            new(1, new(32, 65)),
            new(1, new(32, 68)),
            new(1, new(64, 83)),
            new(8, new(16, 36)),
            new(1, new(16, 32)),
            new(1, new(16, 41)),
            new(1, new(64, 80)),
            new(1, new(64, 80)), // 20
            new(1, new(32, 46)),
            new(1, new(48, 50)),
            new(1, new(16, 30)),
            new(1, new(16, 28)),
            new(1, new(16, 34)),
            new(1, new(16, 25)),
            new(1, new(64, 80)),
            new(1, new(96, 80)),
            new(1, new(96, 80)),
            new(1, new(48, 80)), // 30
            new(3, new(96, 80)),
            new(1, new(80, 21)),
            new(1, new(48, 39)),
            new(1, new(80, 16)),
            new(1, new(32, 77)),
            new(1, new(48, 23)),
            new(3, new(64, 83)),
            new(7, new(96, 67)),
            new(3, new(80, 84)),
            new(3, new(64, 71)), // 40
            new(3, new(64, 68)),
            new(3, new(96, 80)),
            new(3, new(64, 86)),
            new(3, new(64, 83)),
            new(3, new(48, 78)),
            new(3, new(64, 67)),
            new(3, new(64, 73)),
            new(3, new(80, 54)),
            new(3, new(64, 67)),
            new(3, new(64, 69)), // 50
            new(3, new(64, 86)),
            new(3, new(80, 54)),
            new(3, new(48, 69)),
            new(3, new(80, 77)),
            new(3, new(48, 72)),
            new(3, new(32, 70)),
            new(3, new(64, 67)),
            new(1, new(16, 51)),
            new(1, new(32, 21)),
            new(1, new(32, 26)), // 60
            new(4, new(32, 20)),
            new(1, new(16, 19)),
            new(1, new(16, 22)),
            new(1, new(16, 18)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)), // 70
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(16, 16)), // 80
            new(1, new(16, 16)),
            new(1, new(32, 21)),
            new(1, new(48, 31)),
            new(1, new(16, 16)),
            new(1, new(16, 16)),
            new(1, new(64, 60)),
            new(1, new(32, 30)),
            new(1, new(32, 31)),
            new(1, new(64, 60)),
            new(1, new(47, 43)), // 90
            new(1, new(32, 29)),
            new(1, new(16, 11)),
            new(1, new(16, 19)),
            new(1, new(16, 18)),
            new(1, new(16, 18)),
            new(1, new(80, 78)),
            new(1, new(80, 78)),
            new(1, new(64, 64)),
            new(1, new(64, 63)),
            new(1, new(16, 80)), // 100
            new(1, new(16, 80)),
            new(1, new(16, 80)),
            new(1, new(16, 80)),
            new(1, new(16, 19)),
            new(1, new(16, 34)),
            new(1, new(16, 21)),
            new(1, new(16, 25)),
            new(1, new(64, 64)),
            new(1, new(32, 51)),
            new(1, new(32, 51)), // 110
            new(1, new(16, 17)),
            new(1, new(16, 18)),
            new(1, new(16, 23)),
            new(1, new(16, 22)),
            new(1, new(32, 48)),
            new(1, new(32, 66)),
            new(1, new(32, 80)),
            new(1, new(32, 81)),
            new(1, new(96, 39)),
            new(1, new(32, 35)), // 120
            new(1, new(48, 25)),
            new(8, new(128, 80)),
            new(6, new(128, 29)),
            new(3, new(48, 47)),
            new(1, new(48, 24)),
            new(1, new(64, 34)),
            new(1, new(32, 45)),
            new(1, new(32, 45)),
            new(1, new(32, 45)),
            new(1, new(32, 45)), // 130
            new(1, new(32, 49)),
            new(1, new(32, 45)),
            new(1, new(32, 67)),
            new(1, new(32, 49)),
            new(1, new(32, 32)),
            new(1, new(32, 88)),
            new(1, new(32, 32)),
            new(6, new(48, 48)),
            new(1, new(48, 32)),
            new(1, new(48, 32)), // 140
            new(4, new(48, 29)),
            new(4, new(16, 86)),
            new(1, new(32, 74)),
            new(8, new(16, 36)),
            new(1, new(48, 50)),
            new(1, new(48, 50)),
            new(1, new(16, 26)),
            new(1, new(16, 46)),
            new(4, new(16, 25)),
            new(1, new(32, 41)), // 150
            new(4, new(16, 18)),
            new(1, new(16, 69)),
            new(1, new(32, 23)),
            new(1, new(48, 26)),
            new(1, new(32, 35)),
            new(1, new(48, 35)),
            new(1, new(48, 35)),
            new(1, new(48, 35)),
            new(1, new(32, 26)),
            new(1, new(16, 22)), // 160
            new(1, new(16, 17)),
            new(1, new(32, 36)),
            new(1, new(32, 22)),
            new(1, new(32, 28)),
            new(1, new(32, 28)),
            new(1, new(48, 25)),
            new(1, new(16, 28)),
            new(1, new(16, 28)),
            new(1, new(16, 28)),
            new(1, new(16, 28)), // 170
            new(1, new(16, 28)),
            new(1, new(48, 41)),
            new(1, new(48, 39)),
            new(1, new(16, 19)),
            new(1, new(16, 28)),
            new(1, new(32, 32)),
            new(1, new(48, 45)),
            new(1, new(32, 22)),
            new(1, new(32, 18)),
            new(1, new(32, 80)), // 180
            new(7, new(64, 63)),
            new(1, new(32, 61)),
            new(1, new(32, 38)),
            new(1, new(16, 31)),
            new(1, new(16, 17)),
            new(1, new(16, 48)),
            new(1, new(16, 48)),
            new(1, new(16, 20)),
            new(1, new(16, 33)),
            new(1, new(16, 31)), // 190
            new(1, new(16, 22)),
            new(1, new(32, 20)),
            new(1, new(32, 15)),
            new(1, new(16, 13)),
            new(1, new(16, 11)),
            new(1, new(16, 11)),
            new(1, new(16, 11)),
            new(1, new(16, 14)),
            new(1, new(16, 7)),
            new(1, new(16, 7)), // 200
            new(1, new(80, 80)),
            new(5, new(32, 37)),
            new(3, new(16, 72)),
            new(3, new(16, 51)),
            new(3, new(16, 51)),
            new(1, new(32, 42)),
            new(1, new(16, 7)),
            new(1, new(48, 50)),
            new(1, new(48, 48)),
            new(1, new(16, 31)), // 210
            new(1, new(16, 33)),
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(32, 30)),
            new(1, new(32, 27)),
            new(1, new(32, 80)),
            new(1, new(32, 80)),
            new(1, new(32, 32)),
            new(1, new(32, 80)),
            new(1, new(32, 32)), // 220
            new(1, new(32, 66)),
            new(1, new(64, 64)),
            new(1, new(64, 64)),
            new(1, new(32, 50)),
            new(1, new(16, 56)),
            new(1, new(32, 84)),
            new(1, new(32, 84)),
            new(1, new(32, 45)),
            new(1, new(48, 80)),
            new(1, new(32, 79)), // 230
            new(1, new(16, 47)),
            new(1, new(32, 60)),
            new(1, new(32, 32)),
            new(1, new(32, 55)),
            new(1, new(32, 44)),
            new(1, new(32, 36)),
            new(1, new(32, 36)),
            new(1, new(16, 22)),
            new(1, new(16, 23)),
            new(1, new(16, 24)), // 240
            new(1, new(16, 16)),
            new(1, new(16, 26)),
            new(1, new(32, 22)),
            new(1, new(16, 17)),
            new(1, new(32, 28)),
            new(1, new(16, 21)),
            new(1, new(16, 22)),
            new(1, new(16, 29)),
            new(1, new(48, 50)),
            new(1, new(48, 48)), // 250
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(16, 41)),
            new(1, new(16, 41)),
            new(1, new(32, 30)),
            new(1, new(32, 27)),
            new(4, new(64, 64)),
            new(5, new(32, 48)),
            new(1, new(64, 64)),
            new(1, new(32, 32)), // 260
            new(1, new(32, 32)),
            new(1, new(32, 32)),
            new(1, new(32, 32)),
            new(1, new(32, 32)),
            new(1, new(48, 57)),
            new(3, new(48, 59)),
            new(1, new(32, 24)),
            new(1, new(32, 19)),
            new(1, new(32, 21)),
            new(1, new(16, 15)), // 270
            new(1, new(16, 12)),
            new(1, new(16, 9)),
            new(1, new(16, 7)),
            new(1, new(16, 6)),
            new(5, new(48, 82)),
            new(3, new(48, 48)),
            new(3, new(48, 48)),
            new(1, new(32, 27)),
            new(1, new(32, 19)),
            new(1, new(32, 29)), // 280
            new(1, new(16, 16)),
            new(1, new(16, 13)),
            new(1, new(16, 10)),
            new(1, new(48, 48)),
            new(1, new(32, 32)),
            new(1, new(32, 32)),
            new(1, new(16, 12)),
            new(1, new(16, 11)),
            new(1, new(16, 11)),
            new(1, new(16, 17)), // 290
            new(1, new(16, 21)),
            new(1, new(16, 11)),
            new(1, new(16, 12)),
            new(1, new(16, 11)),
            new(1, new(16, 17)),
            new(1, new(16, 21)),
            new(1, new(16, 11)),
            new(1, new(16, 11)),
            new(1, new(32, 28)),
            new(1, new(32, 23)), // 300
            new(1, new(32, 25)),
            new(1, new(16, 19)),
            new(1, new(16, 16)),
            new(1, new(16, 12)),
            new(1, new(16, 10)),
            new(1, new(16, 8)),
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(48, 48)), // 310
            new(1, new(48, 48)),
            new(1, new(32, 32)),
            new(1, new(16, 80)),
            new(1, new(16, 80)),
            new(1, new(80, 55)),
            new(3, new(48, 69)),
            new(3, new(48, 74)),
            new(3, new(64, 72)),
            new(3, new(48, 73)),
            new(3, new(64, 69)), // 320
            new(3, new(48, 68)),
            new(3, new(48, 69)),
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(48, 48)),
            new(1, new(48, 43)),
            new(1, new(64, 63)),
            new(1, new(32, 35)),
            new(1, new(32, 35)),
            new(1, new(16, 16)), // 330
            new(1, new(32, 34)),
            new(3, new(48, 70)),
            new(1, new(48, 68)),
            new(3, new(64, 70)),
            new(3, new(48, 70)),
            new(3, new(48, 66)),
            new(3, new(48, 70)),
            new(3, new(48, 69)),
            new(3, new(48, 68)),
            new(3, new(48, 68)), // 340
            new(3, new(48, 68)),
            new(3, new(48, 78)),
            new(3, new(48, 78)),
            new(3, new(48, 78)),
            new(3, new(48, 78)),
            new(3, new(32, 52)),
            new(1, new(48, 29)),
            new(1, new(32, 25)),
            new(1, new(32, 21)),
            new(1, new(32, 16)), // 350
            new(1, new(48, 23)),
            new(1, new(16, 17)),
            new(1, new(16, 18)),
            new(1, new(48, 48)),
            new(3, new(64, 73)),
            new(4, new(16, 61)),
            new(1, new(64, 26)),
            new(1, new(32, 58)),
            new(1, new(32, 58)),
            new(1, new(32, 33)), // 360
            new(1, new(32, 32)),
            new(1, new(32, 32)),
            new(8, new(16, 11)),
            new(8, new(16, 11)),
            new(8, new(16, 11)),
            new(1, new(48, 57)),
            new(1, new(48, 59)),
            new(3, new(32, 50)),
            new(3, new(32, 50)),
            new(1, new(48, 69)), // 370
            new(3, new(96, 88)),
            new(3, new(96, 75)),
            new(1, new(80, 66)),
            // Advanced graphics
            new(3, new(48, 78)),
            new(3, new(40, 42)),
            new(1, new(32, 29)),
            new(8, new(16, 36)),
            new(1, new(48, 28)),
            new(1, new(32, 77)),
            new(2, new(64, 48)), // 380
            new(1, new(32, 38)),
            new(1, new(32, 38)),
            new(1, new(64, 60)),
            new(4, new(16, 80)),
            new(8, new(128, 80)),
            new(1, new(48, 70)),
            new(4, new(64, 80)),
            new(1, new(32, 32)),
            new(4, new(32, 32)),
            new(1, new(32, 32)), // 390
            new(1, new(32, 79)),
            new(1, new(128, 117)),
            new(1, new(48, 78)),
            new(1, new(32, 61)),
            new(1, new(80, 80)),
            new(1, new(80, 78)),
            new(1, new(32, 55)),
            new(1, new(64, 60)),
            new(1, new(80, 78)),
            new(1, new(48, 78)), // 400
        ];

        /* NOTE: You can find these values programmatically like this:
            using Ambermoon.Data.Legacy;

            var gameData = new GameData();

            gameData.Load(@"path/to/advanced/Amberfiles/folder");

            var allInfos = new Dictionary<uint, List<GraphicInfo>>();
            gameData.MapManager.Labdata.ToList().ForEach(labData =>
            {
                labData.ObjectInfos.ToList().ForEach((info) =>
                {
                    if (!allInfos.ContainsKey(info.TextureIndex))
                        allInfos[info.TextureIndex] = [new(info.TextureWidth, info.TextureHeight, info.NumAnimationFrames)];
                    else
                        allInfos[info.TextureIndex].Add(new(info.TextureWidth, info.TextureHeight, info.NumAnimationFrames));
                });
            });

            var sortedInfos = allInfos.OrderBy(z => z.Key).ToList();

            foreach (var item in sortedInfos)
            {
                Console.WriteLine(item.Key);

                foreach (var info in item.Value.Distinct())
                    Console.WriteLine($"\t{info.Width}, {info.Height} (Frames: {info.Frames})");
            }

            record GraphicInfo(uint Width, uint Height, uint Frames);
        */
    }
}
