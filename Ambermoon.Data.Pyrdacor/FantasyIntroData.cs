namespace Ambermoon.Data.Pyrdacor;

internal class FantasyIntroData : IFantasyIntroData
{
    public required Queue<FantasyIntroAction> Actions { get; internal init; }
    public required IReadOnlyList<Graphic> FantasyIntroPalettes { get; internal init; }
    public required IReadOnlyDictionary<FantasyIntroGraphic, Graphic> Graphics { get; internal init; }
}
