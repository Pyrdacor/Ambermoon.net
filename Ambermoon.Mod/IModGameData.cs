using Ambermoon.Data;

namespace Ambermoon.Mod;

public partial interface IModGameData : IGameData
{
    IMod Mod { get; }
}
