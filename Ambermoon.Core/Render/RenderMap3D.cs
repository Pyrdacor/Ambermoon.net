/*
 * RenderMap3D.cs - Handles 3D map rendering
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static Ambermoon.Data.Map.CharacterReference;

namespace Ambermoon.Render
{
    internal class RenderMap3D
    {
        class MapObject
        {
            readonly RenderMap3D map;
            readonly ISurface3D surface;
            readonly uint objectIndex;
            readonly uint numFrames;
            readonly uint ticksPerFrame;

            public MapObject(RenderMap3D map, ISurface3D surface,
                uint objectIndex, uint numFrames, float fps = 1.0f)
            {
                this.surface = surface;
                this.map = map;
                this.objectIndex = objectIndex;
                this.numFrames = numFrames;
                ticksPerFrame = Math.Max(1, (uint)Util.Round(Game.TicksPerSecond / Math.Max(0.001f, fps)));
            }

            public void Destroy()
            {
                surface?.Delete();
            }

            public void Update(uint ticks)
            {
                if (numFrames <= 1 || !surface.Visible)
                    return;

                uint frame = (ticks / ticksPerFrame) % numFrames;
                surface.TextureAtlasOffset = map.GetObjectTextureOffset(objectIndex) +
                    new Position((int)(frame * surface.TextureWidth), 0);
            }
        }

        class MapCharacter : IMapCharacter
        {
            const uint TimePerMovement = 2000; // in ms
            const uint TicksPerMovement = TimePerMovement * Game.TicksPerSecond / 1000;
            readonly Game game;
            readonly RenderMap3D map;
            readonly ISurface3D surface;
            readonly uint characterIndex;
            readonly uint numFrames;
            readonly uint ticksPerFrame;
            readonly Map.CharacterReference characterReference;
            readonly uint textureIndex;
            readonly Labdata.ObjectPosition objectPosition;
            Position position = new Position();
            Position targetPosition = new Position();
            uint nextMoveTimeSlot = uint.MaxValue;
            uint lastMoveTicks = 0;
            bool moving = false;
            bool active = true;
            float movedX = 0.0f;
            float movedY = 0.0f;
            uint movedTicks = 0;

            public MapCharacter(Game game, RenderMap3D map, ISurface3D surface,
                uint characterIndex, Map.CharacterReference characterReference,
                Labdata.ObjectPosition objectPosition, uint textureIndex,
                uint numFrames, float fps = 1.0f)
            {
                this.game = game;
                this.surface = surface;
                this.map = map;
                this.characterIndex = characterIndex;
                this.numFrames = numFrames;
                ticksPerFrame = Math.Max(1, (uint)Util.Round(Game.TicksPerSecond / Math.Max(0.001f, fps)));
                this.characterReference = characterReference;
                this.textureIndex = textureIndex;
                this.objectPosition = objectPosition;
                ResetPosition(game.GameTime);
            }

            public void Destroy()
            {
                surface?.Delete();
            }

            public bool Active
            {
                get => active;
                set
                {
                    if (active == value)
                        return;

                    active = value;
                    surface.Visible = active;
                }
            }

            public Position Position
            {
                get => position;
                set
                {
                    if (position == value)
                        return;

                    position = new Position(value);
                    UpdatePosition();
                }
            }

            public void ResetPosition(ITime gameTime)
            {
                position = new Position(characterReference.Positions[0]);
                StopMoving();
                nextMoveTimeSlot = (gameTime.TimeSlot + 1) % 12;
            }

            void StopMoving()
            {
                targetPosition = new Position(position);
                moving = false;
                movedX = 0.0f;
                movedY = 0.0f;
                movedTicks = 0;
            }

            public bool Interact(EventTrigger trigger, bool bed)
            {
                // TODO

                // TODO: after battle and monster survival, set: nextMoveTimeSlot = (gameTime.TimeSlot + 1) % 12;
                return false;
            }

            void UpdatePosition(float xOffset = 0.0f, float yOffset = 0.0f)
            {
                map.UpdateCharacterSurfaceCoordinates(Position, surface, objectPosition, xOffset, yOffset);
            }

            void UpdateCurrentMovement(uint ticks)
            {
                if (Position == targetPosition)
                    return;

                if (movedTicks >= TicksPerMovement)
                {
                    moving = false;
                    if (Position != targetPosition)
                    {
                        Position = new Position(targetPosition);
                        UpdatePosition();
                    }
                    return;
                }

                uint moveTicks = Math.Min(ticks - lastMoveTicks, TicksPerMovement - movedTicks);
                var diff = targetPosition - Position;
                float stepSize = Global.DistancePerBlock * moveTicks / TicksPerMovement;

                movedX += diff.X * stepSize;
                movedY += diff.Y * stepSize;
                movedTicks += moveTicks;

                UpdatePosition(movedX, movedY);

                lastMoveTicks = ticks;

                if (numFrames <= 1 || !surface.Visible)
                    return;

                uint frame = targetPosition == Position ? numFrames / 2 : (ticks / ticksPerFrame) % numFrames; // TODO
                surface.TextureAtlasOffset = map.GetObjectTextureOffset(textureIndex) +
                    new Position((int)(frame * surface.TextureWidth), 0);
            }

            bool CanSee(Position position)
            {
                var testPosition = new Position(Position);

                while (true)
                {
                    var distance = position - testPosition;

                    if (distance.X < 0)
                        --testPosition.X;
                    else if (distance.X > 0)
                        ++testPosition.X;

                    if (distance.Y < 0)
                        --testPosition.Y;
                    else if (distance.Y > 0)
                        ++testPosition.Y;

                    if (testPosition == position)
                        return true;

                    uint blockIndex = (uint)(testPosition.X + testPosition.Y * map.Map.Width);

                    if (map.monsterBlockingBlocks.Contains(blockIndex))
                        return false;
                }
            }

            public void Update(uint ticks, ITime gameTime)
            {
                if (!Active)
                    return;

                if (characterReference.Type == CharacterType.Monster)
                {
                    var distanceToPlayer = game.RenderPlayer.Position - Position;

                    if (distanceToPlayer.X == 0 && distanceToPlayer.Y == 0)
                    {
                        // Monster has reached player -> interact/fight
                        game.MonsterSeesPlayer = true;
                        Interact(EventTrigger.Move, false);
                        StopMoving();
                        nextMoveTimeSlot = uint.MaxValue;
                        return;
                    }

                    if (!moving && CanSee(game.RenderPlayer.Position))
                    {
                        // Monster can see player -> move towards player
                        targetPosition = new Position(Position);

                        if (distanceToPlayer.X < 0)
                            --targetPosition.X;
                        else if (distanceToPlayer.X > 0)
                            ++targetPosition.X;

                        if (distanceToPlayer.Y < 0)
                            --targetPosition.Y;
                        else if (distanceToPlayer.Y > 0)
                            ++targetPosition.Y;

                        game.MonsterSeesPlayer = true;
                        nextMoveTimeSlot = (gameTime.TimeSlot + 1) % 12; // recheck for normal movement in 5 minutes

                        // if blocked don't move and stay there
                        uint blockIndex = (uint)(targetPosition.X + targetPosition.Y * map.Map.Width);
                        if (!map.monsterBlockingBlocks.Contains(blockIndex))
                        {
                            // otherwise start moving
                            movedX = 0.0f;
                            movedY = 0.0f;
                            movedTicks = 0;
                            moving = true;
                            UpdateCurrentMovement(ticks);
                        }
                    }
                }

                if (gameTime.TimeSlot != nextMoveTimeSlot)
                {
                    UpdateCurrentMovement(ticks);
                    return;
                }

                lastMoveTicks = ticks;
                var newPosition = Position;

                void MoveRandom()
                {
                    while (true)
                    {
                        newPosition = new Position(Position.X + game.RandomInt(-1, 1), Position.Y + game.RandomInt(-1, 1));

                        var collisionPosition = new Position(newPosition.X, newPosition.Y);

                        if (collisionPosition.X < 0 || collisionPosition.X >= map.Map.Width)
                            continue;

                        if (collisionPosition.Y < 0 || collisionPosition.Y >= map.Map.Height)
                            continue;

                        if (newPosition != Position)
                        {
                            uint blockIndex = (uint)(newPosition.X + newPosition.Y * map.Map.Width);

                            if (!map.monsterBlockingBlocks.Contains(blockIndex))
                                break;
                        }
                    }
                }

                if (characterReference.Type == CharacterType.Monster)
                {
                    if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement))
                    {
                        MoveRandom();
                    }
                    else
                    {
                        // Just stay
                    }
                }
                else
                {
                    if (characterReference.CharacterFlags.HasFlag(Flags.RandomMovement))
                    {
                        MoveRandom();
                    }
                    else
                    {
                        // Walk a given path every day time slot
                        newPosition = new Position(characterReference.Positions[(int)gameTime.TimeSlot]);
                        newPosition.Offset(-1, -1); // positions are 1-based
                    }
                }

                nextMoveTimeSlot = (gameTime.TimeSlot + 1) % 12;
                targetPosition = new Position(newPosition);
                movedX = 0.0f;
                movedY = 0.0f;
                movedTicks = 0;
                moving = true;
                UpdateCurrentMovement(ticks);
            }
        }

        public const int FloorTextureWidth = 64;
        public const int FloorTextureHeight = 64;
        public const int TextureWidth = 128;
        public const int TextureHeight = 80;
        public const float BlockSize = 500.0f;
        public const float ReferenceWallHeight = 320.0f;
        readonly Game game;
        readonly ICamera3D camera = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        ITextureAtlas textureAtlas = null;
        ISurface3D floor = null;
        ISurface3D ceiling = null;
        Labdata labdata = null;
        readonly List<uint> monsterBlockingBlocks = new List<uint>();
        readonly Dictionary<uint, List<ICollisionBody>> blockCollisionBodies = new Dictionary<uint, List<ICollisionBody>>();
        readonly Dictionary<uint, List<ISurface3D>> walls = new Dictionary<uint, List<ISurface3D>>();
        readonly Dictionary<uint, List<MapObject>> objects = new Dictionary<uint, List<MapObject>>();
        readonly Dictionary<uint, MapCharacter> mapCharacters = new Dictionary<uint, MapCharacter>();
        static readonly Dictionary<uint, ITextureAtlas> labdataTextures = new Dictionary<uint, ITextureAtlas>(); // contains all textures for a labdata (walls, objects and overlays)
        static Graphic[] labBackgroundGraphics = null;
        /// <summary>
        /// This contains all block indices that could be changed by map events for labdatas.
        /// </summary>
        static readonly Dictionary<uint, List<uint>> labdataChangeableBlocks = new Dictionary<uint, List<uint>>();
        public Map Map { get; private set; } = null;
        /// <summary>
        ///  This is the height for the renderer. It is expressed in relation
        ///  to the block size (e.g. wall is 2/3 as height as a block is wide).
        /// </summary>
        float WallHeight => FullWallHeight * labdata.WallHeight / ReferenceWallHeight;
        float FullWallHeight => (ReferenceWallHeight / BlockSize) * 0.75f * Global.DistancePerBlock;
        public event Action<Map> MapChanged;

        public RenderMap3D(Game game, Map map, IMapManager mapManager, IRenderView renderView, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            this.game = game;
            camera = renderView.Camera3D;
            this.mapManager = mapManager;
            this.renderView = renderView;

            EnsureLabBackgroundGraphics(renderView.GraphicProvider);

            if (map != null)
                SetMap(map, playerX, playerY, playerDirection);
        }

        public void SetMap(Map map, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to load a 2D map into a 3D render map.");

            if (Map != map)
            {
                Destroy();

                Map = map;
                labdata = mapManager.GetLabdataForMap(map);
                EnsureLabdataTextureAtlas();
                EnsureChangeableBlocks();
                UpdateSurfaces();
                AddCharacters();

                camera.GroundY = -0.5f * FullWallHeight; // TODO: Does labdata.Unknown1 contain an offset?

                MapChanged?.Invoke(map);
            }

            camera.SetPosition(playerX * Global.DistancePerBlock, (map.Height - playerY) * Global.DistancePerBlock);
            camera.TurnTowards((float)playerDirection * 90.0f);
        }

        public void Destroy()
        {
            floor?.Delete();
            ceiling?.Delete();

            floor = null;
            ceiling = null;

            walls.Values.ToList().ForEach(walls => walls.ForEach(wall => wall?.Delete()));
            objects.Values.ToList().ForEach(objects => objects.ForEach(obj => obj?.Destroy()));
            mapCharacters.Values.ToList().ForEach(mc => mc?.Destroy());

            walls.Clear();
            objects.Clear();
            mapCharacters.Clear();

            blockCollisionBodies.Clear();
            monsterBlockingBlocks.Clear();
        }

        void EnsureLabBackgroundGraphics(IGraphicProvider graphicProvider)
        {
            if (labBackgroundGraphics != null)
                return;

            // Note: Palette index 9 is used for the transparent parts (I don't know why) so
            // we replace this index here with index 0.
            labBackgroundGraphics = graphicProvider.GetGraphics(GraphicType.LabBackground).ToArray();

            for (int i = 0; i < labBackgroundGraphics.Length; ++i)
            {
                var labBackgroundGraphic = labBackgroundGraphics[i];

                for (int b = 0; b < labBackgroundGraphic.Width * labBackgroundGraphic.Height; ++b)
                {
                    if (labBackgroundGraphic.Data[b] == 9)
                        labBackgroundGraphic.Data[b] = 0;
                }
            }
        }

        void EnsureChangeableBlocks()
        {
            if (!labdataChangeableBlocks.ContainsKey(Map.TilesetOrLabdataIndex))
            {
               var blockIndices = new List<uint>();

                foreach (var mapEvent in Map.Events)
                {
                    if (mapEvent.Type == EventType.ChangeTile)
                    {
                        if (!(mapEvent is ChangeTileEvent changeTileEvent))
                            throw new AmbermoonException(ExceptionScope.Data, "Invalid map event.");

                        uint index = Map.PositionToTileIndex(changeTileEvent.X - 1, changeTileEvent.Y - 1);

                        if (!blockIndices.Contains(index))
                            blockIndices.Add(index);
                    }
                }

                labdataChangeableBlocks.Add(Map.TilesetOrLabdataIndex, blockIndices);
            }
        }

        void EnsureLabdataTextureAtlas()
        {
            if (!labdataTextures.ContainsKey(Map.TilesetOrLabdataIndex))
            {
                var graphics = new Dictionary<uint, Graphic>();

                foreach (var obj in labdata.Objects)
                {
                    foreach (var subObj in obj.SubObjects)
                    {
                        if (!graphics.ContainsKey(subObj.Object.TextureIndex))
                            graphics.Add(subObj.Object.TextureIndex, labdata.ObjectGraphics[labdata.ObjectInfos.IndexOf(subObj.Object)]);
                    }
                }
                for (int i = 0; i < labdata.WallGraphics.Count; ++i)
                    graphics.Add((uint)i + 1000u, labdata.WallGraphics[i]);
                graphics.Add(10000u, labdata.FloorGraphic ?? new Graphic(FloorTextureWidth, FloorTextureHeight, 0)); // TODO
                graphics.Add(10001u, labdata.CeilingGraphic ?? new Graphic(FloorTextureWidth, FloorTextureHeight, 0)); // TODO

                if (Map.Flags.HasFlag(MapFlags.Outdoor))
                    graphics.Add(10002u, labBackgroundGraphics[(int)Map.World]);

                labdataTextures.Add(Map.TilesetOrLabdataIndex, TextureAtlasManager.Instance.CreateFromGraphics(graphics, 1));
            }

            textureAtlas = labdataTextures[Map.TilesetOrLabdataIndex];
            renderView.GetLayer(Layer.Map3D).Texture = textureAtlas.Texture;
            renderView.GetLayer(Layer.Billboards3D).Texture = textureAtlas.Texture;
        }

        Position GetObjectTextureOffset(uint objectIndex)
        {
            return textureAtlas.GetOffset(objectIndex);
        }

        Position GetWallTextureOffset(uint wallIndex)
        {
            return textureAtlas.GetOffset(wallIndex + 1000u);
        }

        Position FloorTextureOffset => textureAtlas.GetOffset(10000u);
        Position CeilingTextureOffset => textureAtlas.GetOffset(10001u);

        void AddCharacters()
        {
            for (uint characterIndex = 0; characterIndex < Map.CharacterReferences.Length; ++characterIndex)
            {
                var characterReference = Map.CharacterReferences[characterIndex];

                if (characterReference == null)
                    break;

                AddMapCharacter(renderView.Surface3DFactory, renderView.GetLayer(Layer.Billboards3D), characterIndex, characterReference);
            }
        }

        void UpdateCharacterSurfaceCoordinates(Position position, ISurface3D surface, Labdata.ObjectPosition objectPosition,
            float xOffset = 0.0f, float yOffset = 0.0f)
        {
            float baseX = position.X * Global.DistancePerBlock;
            float baseY = (-Map.Height + position.Y) * Global.DistancePerBlock;
            surface.X = baseX + (objectPosition.X / BlockSize) * Global.DistancePerBlock + xOffset;
            surface.Y = WallHeight * (objectPosition.Z / 400.0f + objectPosition.Object.MappedTextureHeight / ReferenceWallHeight);
            surface.Z = baseY + Global.DistancePerBlock - (objectPosition.Y / BlockSize) * Global.DistancePerBlock + yOffset;
        }

        void AddMapCharacter(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint characterIndex,
            Map.CharacterReference characterReference)
        {
            var obj = labdata.Objects[(int)characterReference.GraphicIndex - 1];
            float wallHeight = WallHeight;

            if (obj.SubObjects.Count != 1)
                throw new AmbermoonException(ExceptionScope.Data, "Character with more than 1 sub objects.");

            var subObject = obj.SubObjects[0];
            var objectInfo = subObject.Object;
            var mapObject = surfaceFactory.Create(SurfaceType.Billboard,
                Global.DistancePerBlock * objectInfo.MappedTextureWidth / BlockSize,
                wallHeight * objectInfo.MappedTextureHeight / ReferenceWallHeight,
                objectInfo.TextureWidth, objectInfo.TextureHeight, objectInfo.TextureWidth,
                objectInfo.TextureHeight, true, Math.Max(1, (int)objectInfo.NumAnimationFrames),
                objectInfo.ExtrudeOffset / BlockSize);
            mapObject.Layer = layer;
            mapObject.PaletteIndex = (byte)(Map.PaletteIndex - 1);
            UpdateCharacterSurfaceCoordinates(characterReference.Positions[0], mapObject, subObject);
            mapObject.TextureAtlasOffset = GetObjectTextureOffset(objectInfo.TextureIndex);
            var mapCharacter = new MapCharacter(game, this, mapObject, characterIndex, characterReference,
                subObject, objectInfo.TextureIndex, objectInfo.NumAnimationFrames, 4.0f); // TODO: fps?
            mapCharacter.Active = !game.CurrentSavegame.GetCharacterBit(Map.Index, characterIndex);
            if (mapCharacter.Active)
                mapObject.Visible = true;
            mapCharacters.Add(characterIndex, mapCharacter);
        }

        void AddObject(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, Labdata.Object obj)
        {
            uint blockIndex = mapX + mapY * (uint)Map.Width;
            blockCollisionBodies.Add(blockIndex, new List<ICollisionBody>(8));
            float baseX = mapX * Global.DistancePerBlock;
            float baseY = (-Map.Height + mapY) * Global.DistancePerBlock;

            // TODO: animations

            float wallHeight = WallHeight;

            foreach (var subObject in obj.SubObjects)
            {
                var objectInfo = subObject.Object;
                bool floorObject = objectInfo.Flags.HasFlag(Labdata.ObjectFlags.FloorObject);
                var mapObject = floorObject
                    ? surfaceFactory.Create(SurfaceType.BillboardFloor,
                        Global.DistancePerBlock * objectInfo.MappedTextureWidth / BlockSize,
                        Global.DistancePerBlock * objectInfo.MappedTextureHeight / BlockSize,
                        objectInfo.TextureWidth, objectInfo.TextureHeight, objectInfo.TextureWidth,
                        objectInfo.TextureHeight, true, Math.Max(1, (int)objectInfo.NumAnimationFrames),
                        0.7075f * Global.DistancePerBlock) // This ensures drawing over the surrounding floor. It is a bit higher than half the diagonal -> sqrt(2) / 2.
                    : surfaceFactory.Create(SurfaceType.Billboard,
                        Global.DistancePerBlock * objectInfo.MappedTextureWidth / BlockSize,
                        wallHeight * objectInfo.MappedTextureHeight / ReferenceWallHeight,
                        objectInfo.TextureWidth, objectInfo.TextureHeight, objectInfo.TextureWidth,
                        objectInfo.TextureHeight, true, Math.Max(1, (int)objectInfo.NumAnimationFrames),
                        objectInfo.ExtrudeOffset / BlockSize);
                mapObject.Layer = layer;
                mapObject.PaletteIndex = (byte)(Map.PaletteIndex - 1);
                mapObject.X = baseX + (subObject.X / BlockSize) * Global.DistancePerBlock;
                mapObject.Y = floorObject ? (float)objectInfo.ExtrudeOffset / labdata.WallHeight : wallHeight * (subObject.Z / 400.0f + objectInfo.MappedTextureHeight / ReferenceWallHeight);
                mapObject.Z = baseY + Global.DistancePerBlock - (subObject.Y / BlockSize) * Global.DistancePerBlock;
                mapObject.TextureAtlasOffset = GetObjectTextureOffset(objectInfo.TextureIndex);
                mapObject.Visible = true; // TODO: not all objects should be always visible
                objects.SafeAdd(blockIndex, new MapObject(this, mapObject, objectInfo.TextureIndex, objectInfo.NumAnimationFrames, 4.0f)); // TODO: fps?

                if (objectInfo.Flags.HasFlag(Labdata.ObjectFlags.BlockMovement))
                {
                    // TODO: floor objects
                    blockCollisionBodies[blockIndex].Add(new CollisionSphere3D
                    {
                        CenterX = mapObject.X,
                        CenterZ = -mapObject.Z,
                        Radius = objectInfo.CollisionRadius / BlockSize
                    });
                    if (!monsterBlockingBlocks.Contains(blockIndex))
                        monsterBlockingBlocks.Add(blockIndex);
                }
            }
        }

        void AddWall(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, uint wallIndex)
        {
            uint blockIndex = mapX + mapY * (uint)Map.Width;
            blockCollisionBodies.Add(blockIndex, new List<ICollisionBody>(4));
            float wallHeight = WallHeight;
            var wallTextureOffset = GetWallTextureOffset(wallIndex);
            bool alpha = labdata.Walls[(int)wallIndex].Flags.HasFlag(Labdata.WallFlags.Transparency);
            bool blocksMovement = labdata.Walls[(int)wallIndex].Flags.HasFlag(Labdata.WallFlags.BlockMovement);

            if (!monsterBlockingBlocks.Contains(blockIndex) &&
                (blocksMovement || labdata.Walls[(int)wallIndex].Flags.HasFlag(Labdata.WallFlags.BlockSight)))
            {
                // Note: Doors will also block monsters
                monsterBlockingBlocks.Add(blockIndex);
            }

            // This is used to determine if surrounded tiles should add a wall.
            // Free block means no wall, non-blocking wall or a transparent/removable wall.
            bool IsFreeBlock(uint mapX, uint mapY)
            {
                var block = Map.Blocks[mapX, mapY];

                if (block.MapBorder)
                    return false;

                if (block.WallIndex == 0)
                    return true;

                var wall = labdata.Walls[(int)block.WallIndex - 1];

                return wall.Flags.HasFlag(Labdata.WallFlags.Transparency) ||
                    !wall.Flags.HasFlag(Labdata.WallFlags.BlockMovement) ||
                    labdataChangeableBlocks[Map.TilesetOrLabdataIndex].Contains(Map.PositionToTileIndex(mapX, mapY));
            }

            void AddSurface(WallOrientation wallOrientation, float x, float z)
            {
                var wall = surfaceFactory.Create(SurfaceType.Wall, Global.DistancePerBlock, wallHeight,
                    TextureWidth, TextureHeight, TextureWidth, TextureHeight, alpha, 1, 0.0f, wallOrientation);
                wall.Layer = layer;
                wall.PaletteIndex = (byte)(Map.PaletteIndex - 1);
                wall.X = x;
                wall.Y = wallHeight;
                wall.Z = z;
                wall.TextureAtlasOffset = wallTextureOffset;
                wall.Visible = true; // TODO: not all walls should be always visible
                walls.SafeAdd(blockIndex, wall);

                if (blocksMovement)
                {
                    blockCollisionBodies[blockIndex].Add(new CollisionLine3D
                    {
                        X = wallOrientation == WallOrientation.Rotated180 ? x - Global.DistancePerBlock : x,
                        Z = -(wallOrientation == WallOrientation.Rotated270 ? z - Global.DistancePerBlock : z),
                        Horizontal = wallOrientation == WallOrientation.Normal || wallOrientation == WallOrientation.Rotated180,
                        Length = Global.DistancePerBlock
                    });
                }
            }

            float baseX = mapX * Global.DistancePerBlock;
            float baseY = (-Map.Height + mapY) * Global.DistancePerBlock;

            // front face
            if (mapY > 0 && IsFreeBlock(mapX, mapY - 1))
                AddSurface(WallOrientation.Normal, baseX, baseY);

            // left face
            if (mapX < Map.Width - 1 && IsFreeBlock(mapX + 1, mapY))
                AddSurface(WallOrientation.Rotated90, baseX + Global.DistancePerBlock, baseY);

            // back face
            if (mapY < Map.Height - 1 && IsFreeBlock(mapX, mapY + 1))
                AddSurface(WallOrientation.Rotated180, baseX + Global.DistancePerBlock, baseY + Global.DistancePerBlock);

            // right face
            if (mapX > 0 && IsFreeBlock(mapX - 1, mapY))
                AddSurface(WallOrientation.Rotated270, baseX, baseY + Global.DistancePerBlock);
        }

        internal void UpdateBlock(uint x, uint y)
        {
            uint index = x + y * (uint)Map.Width;

            if (walls.ContainsKey(index))
            {
                walls[index].ForEach(wall => wall?.Delete());
                walls.Remove(index);
            }

            if (objects.ContainsKey(index))
            {
                objects[index].ForEach(obj => obj?.Destroy());
                objects.Remove(index);
            }

            if (blockCollisionBodies.ContainsKey(index))
                blockCollisionBodies.Remove(index);
            if (monsterBlockingBlocks.Contains(index))
                monsterBlockingBlocks.Remove(index);

            var surfaceFactory = renderView.Surface3DFactory;
            var layer = renderView.GetLayer(Layer.Map3D);
            var billboardLayer = renderView.GetLayer(Layer.Billboards3D);
            var block = Map.Blocks[x, y];

            if (block.WallIndex != 0)
                AddWall(surfaceFactory, layer, x, y, block.WallIndex - 1);
            else if (block.ObjectIndex != 0)
                AddObject(surfaceFactory, billboardLayer, x, y, labdata.Objects[(int)block.ObjectIndex - 1]);
        }

        void UpdateSurfaces()
        {
            // Delete all surfaces
            Destroy();

            var surfaceFactory = renderView.Surface3DFactory;
            var layer = renderView.GetLayer(Layer.Map3D);
            var billboardLayer = renderView.GetLayer(Layer.Billboards3D);

            // Add floor and ceiling
            floor = surfaceFactory.Create(SurfaceType.Floor,
                Map.Width * Global.DistancePerBlock, Map.Height * Global.DistancePerBlock,
                FloorTextureWidth, FloorTextureHeight,
                (uint)Map.Width * FloorTextureWidth, (uint)Map.Height * FloorTextureHeight, false);
            floor.PaletteIndex = (byte)(Map.PaletteIndex - 1);
            floor.Layer = layer;
            floor.X = 0.0f;
            floor.Y = 0.0f;
            floor.Z = -Map.Height * Global.DistancePerBlock;
            floor.TextureAtlasOffset = FloorTextureOffset;
            floor.Visible = true;
            ceiling = surfaceFactory.Create(SurfaceType.Ceiling,
                Map.Width * Global.DistancePerBlock, Map.Height * Global.DistancePerBlock,
                FloorTextureWidth, FloorTextureHeight,
                (uint)Map.Width * FloorTextureWidth, (uint)Map.Height * FloorTextureHeight, false);
            ceiling.PaletteIndex = (byte)(Map.PaletteIndex - 1);
            ceiling.Layer = layer;
            ceiling.X = 0.0f;
            ceiling.Y = WallHeight;
            ceiling.Z = 0.0f;
            ceiling.TextureAtlasOffset = CeilingTextureOffset;
            ceiling.Visible = true;

            // Add walls and objects
            for (uint y = 0; y < Map.Height; ++y)
            {
                for (uint x = 0; x < Map.Width; ++x)
                {
                    var block = Map.Blocks[x, y];

                    if (block.WallIndex != 0)
                        AddWall(surfaceFactory, layer, x, y, block.WallIndex - 1);
                    else if (block.ObjectIndex != 0)
                        AddObject(surfaceFactory, billboardLayer, x, y, labdata.Objects[(int)block.ObjectIndex - 1]);
                }
            }
        }

        public void Update(uint ticks, ITime gameTime)
        {
            foreach (var mapObject in objects)
                mapObject.Value.ForEach(obj => obj.Update(ticks));

            foreach (var mapCharacter in mapCharacters.Values)
                mapCharacter.Update(ticks, gameTime);
        }

        public void UpdateCharacterVisibility(uint characterIndex)
        {
            if (Map.CharacterReferences[characterIndex] == null)
                throw new AmbermoonException(ExceptionScope.Application, "Null map character");

            mapCharacters[characterIndex].Active = !game.CurrentSavegame.GetCharacterBit(Map.Index, characterIndex);
        }

        public CollisionDetectionInfo3D GetCollisionDetectionInfo(Position position)
        {
            var info = new CollisionDetectionInfo3D();

            for (int y = Math.Max(0, position.Y - 1); y <= Math.Min(Map.Height - 1, position.Y + 1); ++y)
            {
                for (int x = Math.Max(0, position.X - 1); x <= Math.Min(Map.Width - 1, position.X + 1); ++x)
                {
                    uint blockIndex = (uint)(x + y * Map.Width);

                    if (blockCollisionBodies.ContainsKey(blockIndex))
                    {
                        foreach (var collisionBody in blockCollisionBodies[blockIndex])
                            info.CollisionBodies.Add(collisionBody);
                    }
                }
            }

            return info;
        }
    }
}
