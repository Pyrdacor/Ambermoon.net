using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public class SkyProvider : ISkyProvider
    {
        readonly ExecutableData.ExecutableData executableData;
        readonly Dictionary<uint, List<SkyPart>> skyPartCache = new Dictionary<uint, List<SkyPart>>();
        uint lastMapIndex = 0;
        uint lastPhase = uint.MaxValue;

        public SkyProvider(ExecutableData.ExecutableData executableData)
        {
            this.executableData = executableData;
        }

        public IEnumerable<SkyPart> GetSkyParts(Map map, uint hour, uint minute)
        {
            var worldIndex = (int)map.World;

            // 9-18: Day
            // 18-22: Transition to night
            // 22-5: Night
            // 5-9: Transition to day
            Graphic baseGraphic;
            Graphic blendGraphic = null;
            uint destFactor = 0;

            IEnumerable<SkyPart> Cache(uint phase, Func<List<SkyPart>> creator)
            {
                if (skyPartCache.TryGetValue(phase, out var parts))
                    return parts;

                return skyPartCache[phase] = creator();
            }

            if (hour >= 22 || hour < 5) // Night
            {
                lastPhase = 0;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
            }
            else if (hour >= 9 && hour < 18) // Day
            {
                lastPhase = 1;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
            }
            else if (hour >= 18 && hour < 20) // Dawn phase I
            {
                lastPhase = 1000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                destFactor = 255 * ((hour - 18) * 60 + minute) / 120;
            }
            else if (hour >= 20 && hour < 22) // Dawn phase II
            {
                lastPhase = 3000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
                destFactor = 255 * ((hour - 20) * 60 + minute) / 120;
            }
            else if (hour >= 5 && hour < 7) // Dusk phase I
            {
                lastPhase = 5000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 0];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                destFactor = 255 * ((hour - 5) * 60 + minute) / 120;
            }
            else if (hour >= 7 && hour < 9) // Dusk phase II
            {
                lastPhase = 7000 + hour * 60 + minute;
                baseGraphic = executableData.SkyGradients[worldIndex * 3 + 1];
                blendGraphic = executableData.SkyGradients[worldIndex * 3 + 2];
                destFactor = 255 * ((hour - 7) * 60 + minute) / 120;
            }
            else
            {
                return null;
            }

            lastMapIndex = map.Index;

            return Cache(lastPhase, () =>
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
                        byte sr = baseGraphic.Data[y * 4 + 0];
                        byte sg = baseGraphic.Data[y * 4 + 1];
                        byte sb = baseGraphic.Data[y * 4 + 2];
                        byte dr = blendGraphic.Data[y * 4 + 0];
                        byte dg = blendGraphic.Data[y * 4 + 1];
                        byte db = blendGraphic.Data[y * 4 + 2];
                        uint r = (sr * (255 - destFactor) + dr * destFactor) / 255;
                        uint g = (sg * (255 - destFactor) + dg * destFactor) / 255;
                        uint b = (sb * (255 - destFactor) + db * destFactor) / 255;
                        ProcessColor(y, (r << 16) | (g << 8) | b);
                    }
                }

                parts.Add(currentPart);

                return parts;
            });
        }
    }
}
