using Ambermoon.Data;

namespace Ambermoon.Mod;

public partial interface IMod
{
    ModInfo Info { get; }

    ISavegameManager? CustomSavegameManager { get; }
}
