using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Characters
{
    public class PartyMemberReader : CharacterReader, IPartyMemberReader
    {
        public void ReadPartyMember(PartyMember partyMember,
            IDataReader dataReader, IDataReader partyTextReader,
            IDataReader fallbackDataReader = null)
        {
            ReadCharacter(partyMember, dataReader);
            int eventOffset = dataReader.Position;
            try
            {
                EventReader.ReadEvents(dataReader, partyMember.Events, partyMember.EventList);
            }
            catch
            {
                if (fallbackDataReader == null)
                    throw;

                // Events were messed up on writing but we can load them from initial save eventually.
                partyMember.EventList.Clear();
                partyMember.Events.Clear();
                fallbackDataReader.Position = eventOffset;
                EventReader.ReadEvents(fallbackDataReader, partyMember.Events, partyMember.EventList);
                System.Console.WriteLine("Fixed corrupted savegame");
            }
            partyMember.Texts = partyTextReader == null ? new List<string>() : TextReader.ReadTexts(partyTextReader);
        }
    }
}
