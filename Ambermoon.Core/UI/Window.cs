/*
 * Window.cs - Enumeration of all possible ingame windows
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

namespace Ambermoon.UI
{
    public enum Window
    {
        MapView,
        Inventory,
        Stats,
        Door,
        Chest,
        Merchant,
        Event,
        Riddlemouth,
        Conversation,
        Battle,
        BattleLoot,
        BattlePositions,
        Trainer,
        FoodDealer,
        Healer,
        Camp,
        Inn,
        HorseSalesman,
        RaftSalesman,
        ShipSalesman,
        Sage,
        Blacksmith,
        Enchanter,
        Automap
    }

    public struct WindowInfo
    {
        public Window Window;
        public object[] WindowParameters; // party member index, chest event, etc
        public bool Closable => Window != Window.Battle && Window != Window.MapView; // TODO: add more (windows without exit button)
    }
}
