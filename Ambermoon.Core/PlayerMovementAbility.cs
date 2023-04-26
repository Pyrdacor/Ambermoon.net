/*
 * PlayerMovementAbility.cs - Enumeration of possible movement abilities
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

using Ambermoon.Data.Enumerations;
using System.ComponentModel;

namespace Ambermoon
{
    public enum PlayerMovementAbility
    {
        NoMovement,
        Walking,
        Swimming,
        FlyingDisc,
        Rafting, // raft
        Sailing, // boat, sand ship
        WitchBroom,
        Eagle, // also used for wasp
        Flying
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class PlayerMovementAbilityExtensions
    {
        public static PlayerMovementAbility ToPlayerMovementAbility(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => PlayerMovementAbility.Walking,
            TravelType.Horse => PlayerMovementAbility.Walking,
            TravelType.SandLizard => PlayerMovementAbility.Walking,
            TravelType.Swim => PlayerMovementAbility.Swimming,
            TravelType.MagicalDisc => PlayerMovementAbility.FlyingDisc,
            TravelType.SandShip => PlayerMovementAbility.Sailing,
            TravelType.Raft => PlayerMovementAbility.Rafting,
            TravelType.Ship => PlayerMovementAbility.Sailing,
            TravelType.WitchBroom => PlayerMovementAbility.WitchBroom,
            TravelType.Fly => PlayerMovementAbility.Flying,
            TravelType.Eagle => PlayerMovementAbility.Eagle,
            TravelType.Wasp => PlayerMovementAbility.Eagle,
            _ => PlayerMovementAbility.NoMovement
        };
    }
}
