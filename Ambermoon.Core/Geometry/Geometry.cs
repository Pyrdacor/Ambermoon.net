using Ambermoon.Data;
using System;
using System.Collections.Generic;

namespace Ambermoon.Geometry
{
    public static class Geometry
    {
        /// <summary>
        /// Converts a block position (3D tile position) to a camera position.
        /// </summary>
        public static void BlockToCameraPosition(Map map, Position blockPosition, out float x, out float z)
        {
            x = blockPosition.X * Global.DistancePerTile + 0.5f * Global.DistancePerTile;
            z = (map.Height - blockPosition.Y) * Global.DistancePerTile - 0.5f * Global.DistancePerTile;
        }

        /// <summary>
        /// Converts a camera position to a block position (3D tile position).
        /// </summary>
        public static Position CameraToBlockPosition(Map map, float x, float z)
        {
            return new Position(Misc.Round((-x - 0.5f * Global.DistancePerTile) / Global.DistancePerTile),
                map.Height - Misc.Round((z + 0.5f * Global.DistancePerTile) / Global.DistancePerTile));
        }

        public static List<Position> CameraToTouchedBlockPositions(Map map, float x, float z, float touchRadius)
        {
            List<Position> positions = new List<Position>(4);

            float tileX = (-x - 0.5f * Global.DistancePerTile) / Global.DistancePerTile;
            float tileY = (z + 0.5f * Global.DistancePerTile) / Global.DistancePerTile;
            var mainTilePosition = new Position(Misc.Round(tileX), map.Height - Misc.Round(tileY));
            positions.Add(mainTilePosition);

            for (int ty = Math.Max(0, mainTilePosition.Y - 1); ty <= Math.Min(map.Height - 1, mainTilePosition.Y + 1); ++ty)
            {
                for (int tx = Math.Max(0, mainTilePosition.X - 1); tx <= Math.Min(map.Width - 1, mainTilePosition.X + 1); ++tx)
                {
                    if (tx == mainTilePosition.X && ty == mainTilePosition.Y)
                        continue;

                    if (Math.Abs(tx - tileX) * Global.DistancePerTile < touchRadius &&
                        Math.Abs(map.Height - ty - tileY) * Global.DistancePerTile < touchRadius)
                        positions.Add(new Position(tx, ty));
                }
            }

            return positions;
        }

        /// <summary>
        /// Converts a camera position to a map position.
        /// 
        /// Map positions start at the upper-left tile with and use a specific size per tile.
        /// </summary>
        public static void CameraToMapPosition(Map map, float x, float z, out float mapX, out float mapY)
        {
            mapX = -x;
            mapY = map.Height * Global.DistancePerTile - z;
        }

        /// <summary>
        /// Converts a camera position to a world position.
        /// 
        /// World positions are map positions with Z = 0 at the bottom and Z > 0 at the top.
        /// </summary>
        public static void CameraToWorldPosition(Map map, float x, float z, out float mapX, out float mapY)
        {
            mapX = -x;
            mapY = z;
        }
    }
}
