using Ambermoon.Data;
using Ambermoon.Render;
using System;

namespace Ambermoon.Geometry
{
    static class Raycast2D
    {
        // Swap the values of A and B
        static void Swap<T>(ref T a, ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }

        /// <summary>
        /// Tests if a cast ray collisions with the map environment.
        /// The collision check is represented by a passed collision tester.
        /// Returns true if a collision was detected, otherwise false.
        /// The positions must be given in tiles.
        /// This method is optimized for 1x2 tile characters and will only
        /// work correctly with them (e.g. 2D indoor maps).
        /// </summary>
        public static bool TestRay(Map map, int startX, int startY, int endX, int endY, Func<Map.Tile, bool> collisionTester)
        {
            if (Math.Abs(endX - startX) >= RenderMap2D.NUM_VISIBLE_TILES_X * RenderMap2D.TILE_WIDTH ||
                Math.Abs(endY - startY) >= RenderMap2D.NUM_VISIBLE_TILES_Y * RenderMap2D.TILE_HEIGHT)
                return true; // As we use this to check sight we handle an out-off-screen distance as "collision" -> "sight blocking".

            if (startX == endX)
            {
                if (Math.Abs(endY - startY) < 2)
                    return false;

                if (startY > endY)
                    Swap(ref startY, ref endY);

                int distInTiles = endY - startY;

                for (int y = 1; y < distInTiles; ++y)
                {
                    if (collisionTester(map.Tiles[startX, startY + y]))
                        return true;
                }

                return false;
            }
            else if (startY == endY)
            {
                // Note: The characters in y-direction are 2 tiles height so we test two rows for sight.

                if (startX > endX)
                    Swap(ref startX, ref endX);

                int distInTiles = endX - startX;
                bool upperCollision = false;
                bool lowerCollision = false;

                for (int x = 1; x < distInTiles; ++x)
                {
                    if (!upperCollision && collisionTester(map.Tiles[startX + x, startY]))
                        upperCollision = true;
                    if (!lowerCollision && collisionTester(map.Tiles[startX + x, startY + 1]))
                        lowerCollision = true;

                    if (upperCollision && lowerCollision)
                        return true;
                }

                return false;
            }
            else
            {
                // x and y differ
                int distX = Math.Abs(endX - startX);
                int distY = Math.Abs(endY - startY);
                bool sweep = distY > distX;
                int tileWidth = RenderMap2D.TILE_WIDTH;
                int tileHeight = RenderMap2D.TILE_HEIGHT;

                if (sweep)
                {
                    Swap(ref startX, ref startY);
                    Swap(ref endX, ref endY);
                    Swap(ref tileWidth, ref tileHeight);
                }

                if (startX > endX)
                {
                    Swap(ref startX, ref endX);
                    Swap(ref startY, ref endY);
                }

                // Note: m = dy/dx
                int deltaX = endX - startX;
                int deltaY = endY - startY;
                int range = sweep ? deltaX - 1 : deltaX;

                for (int x = 1; x < deltaX; ++x)
                {
                    // f(x) = startY - m*x
                    int startYInPixel = startY * tileHeight + (sweep ? tileWidth : tileHeight);
                    int xOffset = sweep ? tileWidth / 2 : 0;
                    int fx = startYInPixel + deltaY * (xOffset + x * tileWidth) / deltaX;
                    bool onEdge = fx % tileHeight == 0;
                    int tileY = fx / tileHeight;
                    int tileX = startX + x;

                    if (collisionTester(map.Tiles[sweep ? tileY : tileX, sweep ? tileX : tileY]))
                    {
                        if (!onEdge || collisionTester(map.Tiles[sweep ? tileY - 1 : tileX, sweep ? tileX : tileY - 1]))
                            return true;
                    }
                }

                return false;
            }
        }
    }
}
