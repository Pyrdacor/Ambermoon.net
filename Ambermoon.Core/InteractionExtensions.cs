using Ambermoon.Data;

namespace Ambermoon
{
    internal static class InteractionExtensions
    {
        public static void ExecuteEvents(this IConversationPartner conversationPartner, Game game, EventTrigger trigger)
        {
            foreach (var @event in conversationPartner.EventList)
            {
                uint x = (uint)game.RenderPlayer.Position.X;
                uint y = (uint)game.RenderPlayer.Position.Y; // TODO: adjust?
                bool lastEventStatus = false;
                @event.ExecuteEvent(game.Map, game, trigger, x, y, game.CurrentTicks,
                    ref lastEventStatus, out bool _, conversationPartner);
            }
        }
    }
}
