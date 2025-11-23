/*
 * MapExtensions.cs - Extensions for maps
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

using Ambermoon.Data;
using System;

namespace Ambermoon
{
    public enum EventTrigger
    {
        Always,
        Move,
        Hand,
        Eye,
        Mouth,
        Levitating,
        /// <summary>
        /// If a specific item is needed for triggering use Item0 + ItemIndex
        /// </summary>
        Item0
    }

    internal static class MapExtensions
    {
        static uint? LastMapEventIndexMap = null;
        static uint? LastMapEventIndex = null;
        static Position LastMapEventPosition = null;

        public static void Reset()
        {
            LastMapEventIndexMap = null;
            LastMapEventIndex = null;
            LastMapEventPosition = null;
        }

        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, Savegame savegame)
        {
            return TriggerEvents(map, game, trigger, x, y, savegame, out _);
        }

        public static Event GetEvent(this Map map, uint x, uint y, Savegame savegame)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;
            bool hasMapEvent = mapEventId != 0 && savegame.IsEventActive(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return null;

            return map.EventList[(int)mapEventId - 1];
        }

        public static uint? GetEventIndex(this Map map, uint x, uint y, Savegame savegame)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;
            bool hasMapEvent = mapEventId != 0 && savegame.IsEventActive(map.Index, mapEventId - 1);

            return hasMapEvent ? mapEventId : null;
        }

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, Savegame savegame, out bool hasMapEvent, Func<Event, bool> filter = null)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;

            hasMapEvent = mapEventId != 0 && savegame.IsEventActive(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return false;

            var @event = map.EventList[(int)mapEventId - 1];

            if (filter != null && !filter(@event))
                return false;

            if (trigger == EventTrigger.Move && LastMapEventIndexMap == map.Index && LastMapEventIndex == mapEventId)
            {
                var ev = @event;
                bool hasRandomness = false;

                while (ev?.Type == EventType.Condition ||
                       ev?.Type == EventType.Dice100Roll)
                {
                    if (ev.Type == EventType.Dice100Roll)
                        hasRandomness = true;

                    ev = ev.Next;
                }

                bool hadText = false;

				if (ev != null && ev.Type == EventType.MapText)
                {
                    if (ev.Next == null)
                        return false;

                    ev = ev.Next;
                    hadText = true;
				}

                // avoid triggering the same event twice, but only for some events
                if (ev != null &&
                    ev.Type != EventType.Teleport &&
                    ev.Type != EventType.Chest &&
                    ev.Type != EventType.Door &&
                    ev.Type != EventType.EnterPlace &&
                    ev.Type != EventType.ChangeBuffs &&
                    ev.Type != EventType.Riddlemouth &&
                    ev.Type != EventType.Reward &&
                    ev.Type != EventType.Action &&
                    ev.Type != EventType.ChangeTile &&
                    ((LastMapEventPosition == new Position((int)x, (int)y) && map.Type == MapType.Map3D) || ev.Type != EventType.Trap) &&
                    (ev.Type != EventType.StartBattle || (!hasRandomness && !hadText)))
                {
                    return false;
                }

                if (ev.Type == EventType.StartBattle && hasRandomness)
                {
                    // Avoid triggering random encounters while moving on the same tile.
                    if (LastMapEventPosition == new Position((int)x, (int)y) && map.Type == MapType.Map3D)
                        return false;
                }
            }

            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = mapEventId;
            LastMapEventPosition = new Position((int)x, (int)y);

            if (!map.TriggerEventChain(game, trigger, x, y, @event))
            {
                LastMapEventIndexMap = null;
                LastMapEventIndex = null;
                LastMapEventPosition = null;
                return false;
            }

			return true;
        }

        public static void ClearLastEvent(this Map map)
        {
            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = 0;
            LastMapEventPosition = null;
        }
    }
}
