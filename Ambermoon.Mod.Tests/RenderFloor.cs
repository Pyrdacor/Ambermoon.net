using Ambermoon.Mod.AmbermoonDC;

namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    /// <summary>
    /// Prints a floor layout as text (for debugging the generator).
    /// </summary>
    static void RenderFloor(int floor, int seed)
    {
        var layout = FloorGenerator.Generate(seed, floor);

        Console.WriteLine($"Floor {floor} ({layout.Width}x{layout.Height}), rooms {layout.Rooms.Count}, monsters {layout.Monsters.Count}, " +
            $"chests {layout.Chests.Count}, doors {layout.Doors.Count}, secret walls {layout.SecretWalls.Count}");
        Console.WriteLine("Legend: # wall, . floor, D door, d locked door, I illusory wall, S secret wall, @ start, > stairs, " +
            "C chest, $ secret chest, T treasure chest, m monster, w monster which waits, B boss, R recruit, o decoration");

        for (int y = 0; y < layout.Height; y++)
        {
            var line = new char[layout.Width];

            for (int x = 0; x < layout.Width; x++)
            {
                var pos = new Pos(x, y);
                char c = layout[pos] switch
                {
                    CellType.Wall => '#',
                    CellType.Door => layout.Doors.First(d => d.Position == pos).Locked ? 'd' : 'D',
                    CellType.IllusoryWall => 'I',
                    CellType.SecretWall => 'S',
                    _ => layout.Decorations.ContainsKey(pos) ? 'o' : '.'
                };

                if (layout.Chests.FirstOrDefault(ch => ch.Position == pos) is ChestSpot chest)
                    c = chest.Kind switch { ChestKind.Secret => '$', ChestKind.Treasure => 'T', _ => 'C' };
                if (layout.Monsters.FirstOrDefault(m => m.Position == pos) is MonsterSpawn monster)
                    c = monster.OnlyMoveWhenSeeingPlayer ? 'w' : 'm';
                if (layout.Boss == pos)
                    c = 'B';
                if (layout.Recruit == pos)
                    c = 'R';
                if (layout.Start == pos)
                    c = '@';
                if (layout.Stairs == pos)
                    c = '>';

                line[x] = c;
            }

            Console.WriteLine(new string(line));
        }
    }
}
