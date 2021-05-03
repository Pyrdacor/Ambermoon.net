using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public class LightEffectProvider : ILightEffectProvider
    {
        readonly ExecutableData.ExecutableData executableData;
        readonly Dictionary<uint, List<SkyPart>> skyPartCache = new Dictionary<uint, List<SkyPart>>();
        readonly Dictionary<uint, PaletteReplacement> paletteReplaceCache = new Dictionary<uint, PaletteReplacement>();

        /// <summary>
        /// This was extracted from the original code. These are the brightness levels
        /// for outdoor maps. At least they are used to blend colors dependent on daytime
        /// on those maps.
        /// </summary>
        public static readonly byte[] OutdoorBrightnessLevels = new byte[24]
        {
            0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x28, 0x40,
            0xc8, 0xc8, 0xc8, 0xc8, 0xc8, 0xc8, 0xc8, 0xc8,
            0xc8, 0x40, 0x40, 0x28, 0x10, 0x10, 0x10, 0x10
        };

        public LightEffectProvider(ExecutableData.ExecutableData executableData)
        {
            this.executableData = executableData;
        }

        public IEnumerable<SkyPart> GetSkyParts(Map map, uint hour, uint minute,
            IGraphicProvider graphicProvider, out PaletteReplacement paletteReplacement)
        {
            if (!map.Flags.HasFlag(MapFlags.Outdoor))
            {
                paletteReplacement = null;
                return null;
            }

            var worldIndex = (int)map.World;

            // 9-18: Day
            // 18-22: Transition to night
            // 22-5: Night
            // 5-9: Transition to day
            Graphic baseGraphic = null;
            Graphic blendGraphic = null;
            Graphic basePalette = null;
            Graphic blendPalette = null;
            uint destFactor = 0;
            uint stage = 0;

            IEnumerable<SkyPart> Cache(uint stage, Func<List<SkyPart>> creator)
            {
                uint key = (uint)worldIndex * 10000 + stage;

                if (skyPartCache.TryGetValue(key, out var parts))
                    return parts;

                return skyPartCache[key] = creator();
            }

            PaletteReplacement CachePaletteReplacement(uint stage, Func<PaletteReplacement> creator)
            {
                uint key = map.TilesetOrLabdataIndex * 10000 + stage;

                if (paletteReplaceCache.TryGetValue(key, out var replacement))
                    return replacement;

                return paletteReplaceCache[key] = creator();
            }

            if (hour >= 22 || hour < 5) // Night
            {
                stage = 0;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
                basePalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 0];
            }
            else if (hour >= 9 && hour < 18) // Day
            {
                stage = 1;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
                basePalette = graphicProvider.Palettes[(int)map.PaletteIndex];
            }
            else if (hour >= 18 && hour < 20) // Dawn phase I
            {
                stage = 1000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                basePalette = graphicProvider.Palettes[(int)map.PaletteIndex];
                blendPalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 1];
                destFactor = 255 * ((hour - 18) * 60 + minute) / 120;
            }
            else if (hour >= 20 && hour < 22) // Dawn phase II
            {
                stage = 3000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
                basePalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 1];
                blendPalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 0];
                destFactor = 255 * ((hour - 20) * 60 + minute) / 120;
            }
            else if (hour >= 5 && hour < 7) // Dusk phase I
            {
                stage = 5000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                basePalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 0];
                blendPalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 1];
                destFactor = 255 * ((hour - 5) * 60 + minute) / 120;
            }
            else if (hour >= 7 && hour < 9) // Dusk phase II
            {
                stage = 7000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
                basePalette = executableData.DaytimePaletteReplacements[worldIndex * 2 + 1];
                blendPalette = graphicProvider.Palettes[(int)map.PaletteIndex];                
                destFactor = 255 * ((hour - 7) * 60 + minute) / 120;
            }

            var parts = Cache(stage, () =>
            {
                var parts = new List<SkyPart>();
                uint lastColor = uint.MaxValue;
                SkyPart currentPart = null;

                static uint ToColor(Graphic graphic, int y) =>
                    ((uint)graphic.Data[y * 4 + 0] << 16) | ((uint)graphic.Data[y * 4 + 1] << 8) | graphic.Data[y * 4 + 2];

                void ProcessColor(int y, uint color)
                {
                    if (lastColor != color)
                    {
                        if (currentPart != null)
                            parts.Add(currentPart);

                        lastColor = color;

                        currentPart = new SkyPart
                        {
                            Y = y,
                            Height = 1,
                            Color = color
                        };
                    }
                    else
                    {
                        ++currentPart.Height;
                    }
                }

                if (blendGraphic == null)
                {
                    for (int y = 0; y < 72; ++y)
                    {
                        ProcessColor(y, ToColor(baseGraphic, y));
                    }
                }
                else
                {
                    for (int y = 0; y < 72; ++y)
                    {
                        int offset = y * 4;
                        byte sr = baseGraphic.Data[offset + 0];
                        byte sg = baseGraphic.Data[offset + 1];
                        byte sb = baseGraphic.Data[offset + 2];
                        byte dr = blendGraphic.Data[offset + 0];
                        byte dg = blendGraphic.Data[offset + 1];
                        byte db = blendGraphic.Data[offset + 2];
                        uint r = (sr * (255 - destFactor) + dr * destFactor) / 255;
                        uint g = (sg * (255 - destFactor) + dg * destFactor) / 255;
                        uint b = (sb * (255 - destFactor) + db * destFactor) / 255;
                        ProcessColor(y, (r << 16) | (g << 8) | b);
                    }
                }

                parts.Add(currentPart);

                return parts;
            });

            paletteReplacement = CachePaletteReplacement(stage, () =>
            {
                var replacement = new PaletteReplacement();

                if (blendPalette == null)
                {
                    Array.Copy(basePalette.Data, replacement.ColorData, replacement.ColorData.Length);
                }
                else
                {
                    for (int c = 0; c < 16; ++c)
                    {
                        int offset = c * 4;
                        byte sr = basePalette.Data[offset + 0];
                        byte sg = basePalette.Data[offset + 1];
                        byte sb = basePalette.Data[offset + 2];
                        byte dr = blendPalette.Data[offset + 0];
                        byte dg = blendPalette.Data[offset + 1];
                        byte db = blendPalette.Data[offset + 2];
                        replacement.ColorData[offset + 0] = (byte)((sr * (255 - destFactor) + dr * destFactor) / 255);
                        replacement.ColorData[offset + 1] = (byte)((sg * (255 - destFactor) + dg * destFactor) / 255);
                        replacement.ColorData[offset + 2] = (byte)((sb * (255 - destFactor) + db * destFactor) / 255);
                        replacement.ColorData[offset + 3] = 255;
                    }
                }

                return replacement;
            });

            return parts;
        }
    }
}
