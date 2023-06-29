using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IConversationPartner
    {
        int LookAtTextIndex { get; }
        List<string> Texts { get; }
        List<Event> Events { get; }
        List<Event> EventList { get; }
    }
}
