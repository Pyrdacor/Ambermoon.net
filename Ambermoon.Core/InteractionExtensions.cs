using Ambermoon.Data;
using System.Linq;

namespace Ambermoon
{
    internal static class InteractionExtensions
    {
        public static void ExecuteEvents(this IConversationPartner conversationPartner, Game game, EventTrigger trigger)
        {
            var @event = conversationPartner.EventList.FirstOrDefault(e => e is ConversationEvent ce &&
                ce.Interaction == ConversationEvent.InteractionType.Talk);

            if (@event != null)
            {
                uint x = (uint)game.RenderPlayer.Position.X;
                uint y = (uint)game.RenderPlayer.Position.Y;
                bool lastEventStatus = false;
                @event.ExecuteEvent(game.Map, game, ref trigger, x, y, game.CurrentTicks,
                    ref lastEventStatus, out bool _, out var _, conversationPartner);
            }
        }
    }
}
