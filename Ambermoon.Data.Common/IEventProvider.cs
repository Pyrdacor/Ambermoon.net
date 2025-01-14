using System.Collections.Generic;

namespace Ambermoon.Data;

public interface IEventProvider
{
    List<Event> Events { get; }
    List<Event> EventList { get; }
}
