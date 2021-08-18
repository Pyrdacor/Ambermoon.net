/*
 * InteractionExtensions.cs - Extensions for interactions
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
using System.Linq;

namespace Ambermoon
{
    internal static class InteractionExtensions
    {
        public static void ExecuteEvents(this IConversationPartner conversationPartner, Game game, EventTrigger trigger,
            uint characterIndex)
        {
            var @event = conversationPartner.EventList.FirstOrDefault(e => e is ConversationEvent ce &&
                ce.Interaction == ConversationEvent.InteractionType.Talk);

            if (@event != null)
            {
                uint x = (uint)game.RenderPlayer.Position.X;
                uint y = (uint)game.RenderPlayer.Position.Y;
                bool lastEventStatus = false;
                @event.ExecuteEvent(game.Map, game, ref trigger, x, y, ref lastEventStatus, out bool _, out var _,
                    conversationPartner, characterIndex);
            }
        }
    }
}
