using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

internal enum CellType
{
    Wall,
    Floor,
    Door,
    /// <summary>
    /// Looks like a wall but can be walked through.
    /// </summary>
    IllusoryWall,
    /// <summary>
    /// A wall which disappears when touched (hand cursor).
    /// </summary>
    SecretWall
}

internal enum ChestKind
{
    Normal,
    Secret,
    Treasure // extra chests on boss floors
}

internal record struct Pos(int X, int Y)
{
    public static Pos operator +(Pos a, Pos b) => new(a.X + b.X, a.Y + b.Y);
    public int DistanceTo(Pos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    public static readonly Pos[] Directions = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)]; // up, right, down, left
}

internal record Room(int X, int Y, int Width, int Height)
{
    public Pos Center => new(X + Width / 2, Y + Height / 2);
    public bool Contains(Pos pos) => pos.X >= X && pos.X < X + Width && pos.Y >= Y && pos.Y < Y + Height;
    public bool Intersects(Room other, int padding) =>
        X - padding < other.X + other.Width && other.X - padding < X + Width &&
        Y - padding < other.Y + other.Height && other.Y - padding < Y + Height;
    public IEnumerable<Pos> Cells()
    {
        for (int y = Y; y < Y + Height; y++)
            for (int x = X; x < X + Width; x++)
                yield return new(x, y);
    }
    public IEnumerable<Pos> Corners()
    {
        yield return new(X, Y);
        yield return new(X + Width - 1, Y);
        yield return new(X, Y + Height - 1);
        yield return new(X + Width - 1, Y + Height - 1);
    }
}

internal record ChestSpot(Pos Position, ChestKind Kind, bool Locked);

internal record DoorSpot(Pos Position, bool Locked);

/// <summary>
/// A teleporter which moves the party to the target position.
/// </summary>
internal record TeleporterSpot(Pos Position, Pos Target, CharacterDirection Direction);

internal record MonsterSpawn(Pos Position, bool OnlyMoveWhenSeeingPlayer);

/// <summary>
/// The layout of one dungeon floor (0-based positions).
/// </summary>
internal class FloorLayout
{
    public required int Floor { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required CellType[,] Cells { get; init; }
    public List<Room> Rooms { get; } = [];
    public Pos Start { get; set; }
    public CharacterDirection StartDirection { get; set; }
    public Pos Stairs { get; set; }
    public List<ChestSpot> Chests { get; } = [];
    public List<DoorSpot> Doors { get; } = [];
    public List<Pos> SecretWalls { get; } = [];
    public List<MonsterSpawn> Monsters { get; } = [];
    public Pos? Boss { get; set; }
    public Pos? Recruit { get; set; }
    /// <summary>
    /// Walkable decoration. The value is a random number which selects one of the decorations of the theme.
    /// </summary>
    public Dictionary<Pos, int> Decorations { get; } = [];
    /// <summary>
    /// Decoration which blocks movement. The value is a random number which selects one of the obstacles of the theme.
    /// </summary>
    public Dictionary<Pos, int> Obstacles { get; } = [];
    public List<TeleporterSpot> Teleporters { get; } = [];

    public bool IsBossFloor => Floor % 5 == 0;
    public bool IsBossWithMinionsFloor => Floor % 10 == 0;
    public bool IsRecruitFloor => Floor % 12 == 0;

    public bool InBounds(Pos pos) => pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

    public CellType this[Pos pos]
    {
        get => Cells[pos.X, pos.Y];
        set => Cells[pos.X, pos.Y] = value;
    }

    public bool IsWalkable(Pos pos) => InBounds(pos) && this[pos] is CellType.Floor or CellType.Door or CellType.IllusoryWall;

    /// <summary>
    /// Distances from the given position (walking through doors and illusory walls).
    /// Unreachable cells have the distance -1. Blocked positions are not entered.
    /// </summary>
    public int[,] Distances(Pos from, ISet<Pos>? blocked = null)
    {
        var distances = new int[Width, Height];

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                distances[x, y] = -1;

        var queue = new Queue<Pos>();
        distances[from.X, from.Y] = 0;
        queue.Enqueue(from);

        while (queue.Count != 0)
        {
            var pos = queue.Dequeue();

            foreach (var direction in Pos.Directions)
            {
                var next = pos + direction;

                if (!IsWalkable(next) || distances[next.X, next.Y] != -1 || blocked?.Contains(next) == true)
                    continue;

                distances[next.X, next.Y] = distances[pos.X, pos.Y] + 1;
                queue.Enqueue(next);
            }
        }

        return distances;
    }
}
