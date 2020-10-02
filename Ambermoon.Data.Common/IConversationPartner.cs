using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IConversationPartner
    {
        List<string> Texts { get; }
        List<Event> Events { get; }
        List<Event> EventList { get; }        
    }
}
