using Ambermoon.Data.Legacy;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class LightEffectDataProvider(Graphic[] skyGradients, Graphic[] daytimePaletteReplacements) : ILightEffectDataProvider
{
    public Graphic[] SkyGradients { get; } = skyGradients;
    public Graphic[] DaytimePaletteReplacements { get; } = daytimePaletteReplacements;
}
