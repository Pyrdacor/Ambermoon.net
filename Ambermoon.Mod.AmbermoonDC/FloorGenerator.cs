using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Generates the layout of a floor. The result only depends on the seed and the floor
/// number, so a floor can be recreated exactly when a savegame is loaded.
/// </summary>
internal static class FloorGenerator
{
    // Note: HashCode.Combine can't be used as it is randomized per process.
    public static System.Random CreateRandom(int seed, int floor, int salt) => new(unchecked(seed * 73856093 ^ floor * 19349663 ^ salt * 83492791));

    public static FloorLayout Generate(int seed, int floor)
    {
        // Retry with other random values in the unlikely case that a layout is not usable.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var layout = TryGenerate(CreateRandom(seed, floor, 1000 + attempt), floor);

            if (layout != null)
                return layout;
        }

        throw new InvalidOperationException($"Unable to generate floor {floor}.");
    }

    static FloorLayout? TryGenerate(System.Random random, int floor)
    {
        int size = Math.Min(44, 22 + floor);
        var layout = new FloorLayout
        {
            Floor = floor,
            Width = size,
            Height = size,
            Cells = new CellType[size, size]
        };

        // Rooms
        int roomCount = Math.Min(14, 5 + floor / 3);

        for (int attempt = 0; attempt < 300 && layout.Rooms.Count < roomCount; attempt++)
        {
            int width = random.Next(3, 8);
            int height = random.Next(3, 8);
            var room = new Room(random.Next(2, size - width - 1), random.Next(2, size - height - 1), width, height);

            if (layout.Rooms.Any(r => r.Intersects(room, 2)))
                continue;

            layout.Rooms.Add(room);
        }

        if (layout.Rooms.Count < 3)
            return null;

        foreach (var cell in layout.Rooms.SelectMany(r => r.Cells()))
            layout[cell] = CellType.Floor;

        // Corridors: connect all rooms (minimum spanning tree) and add some loops.
        var connected = new List<Room> { layout.Rooms[0] };
        var remaining = layout.Rooms.Skip(1).ToList();

        while (remaining.Count != 0)
        {
            var (from, to) = connected
                .SelectMany(c => remaining.Select(r => (c, r)))
                .MinBy(pair => pair.c.Center.DistanceTo(pair.r.Center));
            CarveCorridor(layout, from.Center, to.Center, random);
            connected.Add(to);
            remaining.Remove(to);
        }

        for (int i = 0; i < layout.Rooms.Count / 3; i++)
        {
            var a = layout.Rooms[random.Next(layout.Rooms.Count)];
            var b = layout.Rooms.Where(r => r != a).MinBy(r => r.Center.DistanceTo(a.Center) + random.Next(8))!;
            CarveCorridor(layout, a.Center, b.Center, random);
        }

        // Start and stairs
        var startRoom = layout.Rooms[0];
        layout.Start = startRoom.Center;
        var distances = layout.Distances(layout.Start);
        var stairsRoom = layout.Rooms.Skip(1).MaxBy(r => distances[r.Center.X, r.Center.Y])!;
        layout.Stairs = stairsRoom.Center;

        if (distances[layout.Stairs.X, layout.Stairs.Y] < 6)
            return null;

        for (int d = 0; d < 4; d++)
        {
            if (layout.IsWalkable(layout.Start + Pos.Directions[d]))
            {
                layout.StartDirection = (CharacterDirection)d;
                break;
            }
        }

        var reserved = new HashSet<Pos> { layout.Start, layout.Stairs };

        // Doors at the entrances of rooms
        int maxDoors = Math.Min(8, 2 + floor / 4);

        foreach (var room in layout.Rooms.OrderBy(_ => random.Next()))
        {
            if (layout.Doors.Count >= maxDoors)
                break;

            foreach (var entrance in FindEntrances(layout, room).OrderBy(_ => random.Next()))
            {
                if (layout.Doors.Count >= maxDoors || random.Next(100) >= 35 || reserved.Contains(entrance))
                    continue;

                layout[entrance] = CellType.Door;
                layout.Doors.Add(new DoorSpot(entrance, floor >= 4 && random.Next(100) < 15));
                reserved.Add(entrance);
            }
        }

        // Secret rooms with treasure
        int secretRooms = 1 + (floor % 3 == 0 ? 1 : 0) + (layout.IsBossFloor ? 1 : 0);

        for (int i = 0; i < secretRooms; i++)
            TryAddSecretRoom(layout, random, reserved);

        // Chests (they block movement, so they are only put into room corners)
        int chestCount = 2 + random.Next(3) + (layout.IsBossFloor ? 3 : 0);
        var chestCandidates = layout.Rooms
            .Where(r => r != startRoom && r.Width >= 3 && r.Height >= 3)
            .SelectMany(r => r.Corners())
            .Where(p => !reserved.Contains(p) && Pos.Directions.Count(d => layout.IsWalkable(p + d)) == 2)
            .OrderBy(_ => random.Next())
            .ToList();
        var chestIndex = 0;

        foreach (var candidate in chestCandidates)
        {
            if (chestIndex >= chestCount)
                break;

            var blocked = new HashSet<Pos>(layout.Chests.Select(c => c.Position)) { candidate };

            if (!AllReachable(layout, blocked))
                continue;

            var kind = layout.IsBossFloor && chestIndex >= chestCount - 3 ? ChestKind.Treasure
                : layout.IsGearFloor && chestIndex == 0 ? ChestKind.Gear : ChestKind.Normal;
            layout.Chests.Add(new ChestSpot(candidate, kind, floor >= 6 && random.Next(100) < 20));
            reserved.Add(candidate);
            chestIndex++;
        }

        if (layout.IsGearFloor && layout.Chests.Count == 0)
            return null;

        // Boss (stands next to the stairs and does not move)
        if (layout.IsBossFloor)
        {
            var bossPosition = Pos.Directions.Select(d => layout.Stairs + d)
                .Where(p => layout[p] == CellType.Floor && !reserved.Contains(p))
                .OrderBy(_ => random.Next())
                .Cast<Pos?>()
                .FirstOrDefault();

            if (bossPosition == null)
                return null;

            layout.Boss = bossPosition;
            reserved.Add(bossPosition.Value);
        }

        var freeCells = new List<Pos>();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var pos = new Pos(x, y);

                if (layout[pos] == CellType.Floor && !reserved.Contains(pos))
                    freeCells.Add(pos);
            }
        }

        // A possible companion
        if (layout.IsRecruitFloor)
        {
            var recruitRooms = layout.Rooms.Where(r => r != startRoom && r != stairsRoom).ToList();
            var recruitCells = recruitRooms.SelectMany(r => r.Cells()).Where(freeCells.Contains).ToList();

            if (recruitCells.Count == 0)
                return null;

            layout.Recruit = recruitCells[random.Next(recruitCells.Count)];
            freeCells.Remove(layout.Recruit.Value);
        }

        // A pair of teleporters which connects two rooms (only used if the theme has a teleporter)
        if (floor >= 3 && random.Next(100) < 50)
            TryAddTeleporters(layout, random, freeCells, distances, startRoom);

        // Obstacles (decoration which blocks movement, only used if the theme has some)
        int obstacleCount = 2 + random.Next(4);
        var chestPositions = layout.Chests.Select(c => c.Position).ToHashSet();
        var obstacleCandidates = layout.Rooms
            .SelectMany(r => r.Cells())
            .Where(p => freeCells.Contains(p) && distances[p.X, p.Y] >= 2 && Pos.Directions.Count(d => layout.InBounds(p + d) && layout[p + d] == CellType.Wall) == 1)
            .OrderBy(_ => random.Next())
            .ToList();

        foreach (var candidate in obstacleCandidates)
        {
            if (layout.Obstacles.Count >= obstacleCount)
                break;

            var blocked = new HashSet<Pos>(chestPositions.Concat(layout.Obstacles.Keys)) { candidate };

            // Obstacles must not block anything and must not stand in front of doors.
            if (Pos.Directions.Any(d => layout.InBounds(candidate + d) && layout[candidate + d] is CellType.Door or CellType.IllusoryWall or CellType.SecretWall) ||
                !AllReachable(layout, blocked))
                continue;

            layout.Obstacles[candidate] = random.Next(1000);
            freeCells.Remove(candidate);
        }

        // Monsters
        int monsterCount = Math.Min(24, 3 + floor / 2 + layout.Rooms.Count / 2);
        var monsterCells = freeCells.Where(p => distances[p.X, p.Y] >= 6).OrderBy(_ => random.Next()).Take(monsterCount).ToList();

        foreach (var cell in monsterCells)
        {
            layout.Monsters.Add(new MonsterSpawn(cell, random.Next(2) == 0));
            freeCells.Remove(cell);
        }

        // Decoration
        foreach (var cell in freeCells)
        {
            if (random.Next(100) < 8)
                layout.Decorations[cell] = random.Next(1000);
        }

        return layout;
    }

    static void TryAddTeleporters(FloorLayout layout, System.Random random, List<Pos> freeCells, int[,] distances, Room startRoom)
    {
        // The party arrives next to the other teleporter (looking away from it).
        (Pos Arrival, CharacterDirection Direction)? FindArrival(Pos teleporter)
        {
            for (int d = 0; d < 4; d++)
            {
                var arrival = teleporter + Pos.Directions[d];

                if (freeCells.Contains(arrival))
                    return (arrival, (CharacterDirection)d);
            }

            return null;
        }

        var rooms = layout.Rooms.Where(r => r != startRoom).OrderBy(_ => random.Next()).ToList();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (rooms[i].Center.DistanceTo(rooms[j].Center) < 12)
                    continue;

                var a = rooms[i].Cells().Where(p => freeCells.Contains(p) && distances[p.X, p.Y] >= 4).OrderBy(_ => random.Next()).Cast<Pos?>().FirstOrDefault();
                var b = rooms[j].Cells().Where(p => freeCells.Contains(p) && distances[p.X, p.Y] >= 4).OrderBy(_ => random.Next()).Cast<Pos?>().FirstOrDefault();

                if (a == null || b == null)
                    continue;

                freeCells.Remove(a.Value);
                freeCells.Remove(b.Value);
                var arrivalA = FindArrival(a.Value);
                var arrivalB = FindArrival(b.Value);

                if (arrivalA == null || arrivalB == null)
                {
                    freeCells.Add(a.Value);
                    freeCells.Add(b.Value);
                    continue;
                }

                freeCells.Remove(arrivalA.Value.Arrival);
                freeCells.Remove(arrivalB.Value.Arrival);
                layout.Teleporters.Add(new TeleporterSpot(a.Value, arrivalB.Value.Arrival, arrivalB.Value.Direction));
                layout.Teleporters.Add(new TeleporterSpot(b.Value, arrivalA.Value.Arrival, arrivalA.Value.Direction));
                return;
            }
        }
    }

    static void CarveCorridor(FloorLayout layout, Pos from, Pos to, System.Random random)
    {
        var pos = from;
        bool horizontalFirst = random.Next(2) == 0;

        void Step(int dx, int dy)
        {
            pos = new Pos(pos.X + dx, pos.Y + dy);

            if (layout[pos] == CellType.Wall)
                layout[pos] = CellType.Floor;
        }

        void Horizontal()
        {
            while (pos.X != to.X)
                Step(Math.Sign(to.X - pos.X), 0);
        }

        void Vertical()
        {
            while (pos.Y != to.Y)
                Step(0, Math.Sign(to.Y - pos.Y));
        }

        if (horizontalFirst)
        {
            Horizontal();
            Vertical();
        }
        else
        {
            Vertical();
            Horizontal();
        }
    }

    /// <summary>
    /// Corridor cells directly next to the room which have walls on both sides.
    /// </summary>
    static IEnumerable<Pos> FindEntrances(FloorLayout layout, Room room)
    {
        var result = new HashSet<Pos>();

        foreach (var cell in room.Cells())
        {
            foreach (var direction in Pos.Directions)
            {
                var outside = cell + direction;

                if (room.Contains(outside) || !layout.InBounds(outside) || layout[outside] != CellType.Floor)
                    continue;

                if (layout.Rooms.Any(r => r.Contains(outside)))
                    continue;

                var side1 = new Pos(direction.Y, direction.X);
                var side2 = new Pos(-direction.Y, -direction.X);

                if (layout.InBounds(outside + side1) && layout.InBounds(outside + side2) &&
                    layout[outside + side1] == CellType.Wall && layout[outside + side2] == CellType.Wall)
                    result.Add(outside);
            }
        }

        return result;
    }

    static void TryAddSecretRoom(FloorLayout layout, System.Random random, HashSet<Pos> reserved)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            var floorCell = new Pos(random.Next(1, layout.Width - 1), random.Next(1, layout.Height - 1));

            if (layout[floorCell] != CellType.Floor || reserved.Contains(floorCell))
                continue;

            var direction = Pos.Directions[random.Next(4)];
            var secretWall = floorCell + direction;
            int width = random.Next(2, 4);
            int height = random.Next(2, 4);
            var first = secretWall + direction;
            // The room extends away from the secret wall.
            int x = direction.X < 0 ? first.X - width + 1 : direction.X > 0 ? first.X : first.X - random.Next(width);
            int y = direction.Y < 0 ? first.Y - height + 1 : direction.Y > 0 ? first.Y : first.Y - random.Next(height);
            var room = new Room(x, y, width, height);

            if (x < 2 || y < 2 || x + width > layout.Width - 2 || y + height > layout.Height - 2)
                continue;

            if (!room.Contains(first) || layout[secretWall] != CellType.Wall)
                continue;

            // The room and its surrounding must be solid rock (except the secret wall).
            bool free = true;

            for (int ry = y - 1; ry <= y + height && free; ry++)
            {
                for (int rx = x - 1; rx <= x + width && free; rx++)
                {
                    var pos = new Pos(rx, ry);

                    if (pos != secretWall && layout[pos] != CellType.Wall)
                        free = false;
                }
            }

            // The secret wall must only touch the floor cell and the secret room.
            if (!free || Pos.Directions.Count(d => layout.IsWalkable(secretWall + d)) != 1)
                continue;

            foreach (var cell in room.Cells())
                layout[cell] = CellType.Floor;

            bool illusory = random.Next(2) == 0;
            layout[secretWall] = illusory ? CellType.IllusoryWall : CellType.SecretWall;

            if (!illusory)
                layout.SecretWalls.Add(secretWall);

            reserved.Add(secretWall);
            reserved.Add(floorCell);

            // The treasure is in the farthest corner of the secret room.
            var chest = room.Corners().MaxBy(c => c.DistanceTo(secretWall));
            layout.Chests.Add(new ChestSpot(chest, ChestKind.Secret, false));
            reserved.UnionWith(room.Cells());
            return;
        }
    }

    static bool AllReachable(FloorLayout layout, ISet<Pos> blocked)
    {
        var distances = layout.Distances(layout.Start, blocked);

        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                var pos = new Pos(x, y);

                if (layout.IsWalkable(pos) && !blocked.Contains(pos) && distances[x, y] == -1)
                {
                    // Cells behind secret walls (which are not walkable) are fine.
                    if (layout.SecretWalls.Count != 0 && IsReachableIgnoringSecretWalls(layout, pos, blocked))
                        continue;

                    return false;
                }
            }
        }

        return true;
    }

    static bool IsReachableIgnoringSecretWalls(FloorLayout layout, Pos target, ISet<Pos> blocked)
    {
        // Treat secret walls as walkable for this check.
        var visited = new HashSet<Pos> { layout.Start };
        var queue = new Queue<Pos>();
        queue.Enqueue(layout.Start);

        while (queue.Count != 0)
        {
            var pos = queue.Dequeue();

            if (pos == target)
                return true;

            foreach (var direction in Pos.Directions)
            {
                var next = pos + direction;

                if (!layout.InBounds(next) || visited.Contains(next) || blocked.Contains(next))
                    continue;

                if (!layout.IsWalkable(next) && layout[next] != CellType.SecretWall)
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }
}
