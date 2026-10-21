using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IConversationPartner : IEventProvider
    {
        int LookAtTextIndex { get; }
        List<string> Texts { get; }
        Language SpokenLanguages { get; set; }
        ExtendedLanguage SpokenExtendedLanguages { get; set; }
    }
}
