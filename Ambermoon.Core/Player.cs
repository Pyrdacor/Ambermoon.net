/*
 * Player.cs - Basic player information
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon
{
    internal class Player
    {
        /// <summary>
        /// On world maps this is the total coordinate.
        /// On all other 2D or 3D maps this is the map coordinate.
        /// The position is given in tiles.
        /// </summary>
        public Position Position { get; } = new Position(0, 0);
        public CharacterDirection Direction { get; set; } = CharacterDirection.Down;
        public PlayerMovementAbility MovementAbility { get; set; } = PlayerMovementAbility.NoMovement;
    }
}
