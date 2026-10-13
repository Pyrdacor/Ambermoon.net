using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates the labdata of the dungeon: a copy of a cave labdata with additional
/// monster and person sprites taken from other labdata. As all object textures
/// are global, the sprites can be combined freely (they all use the same palette).
/// </summary>
internal class DCLabdata
{
    /// <summary>
    /// Index of the new labdata (the original game uses 1 to 35).
    /// </summary>
    public const uint LabdataIndex = 60;

    readonly Dictionary<string, uint> spriteObjects = [];

    public Labdata Labdata { get; }

    public uint CombatBackground => Labdata.CombatBackground;

    DCLabdata(Labdata labdata)
    {
        Labdata = labdata;
    }

    public uint GetSpriteObject(string sprite) => spriteObjects[sprite];

    public static DCLabdata Create(ModApi api)
    {
        var baseLabdata = api.GetLabdata(DCContent.BaseLabdata);
        var labdata = new Labdata
        {
            WallHeight = baseLabdata.WallHeight,
            CombatBackground = baseLabdata.CombatBackground,
            Flags = baseLabdata.Flags,
            CeilingColorIndex = baseLabdata.CeilingColorIndex,
            FloorColorIndex = baseLabdata.FloorColorIndex,
            CeilingTextureIndex = baseLabdata.CeilingTextureIndex,
            FloorTextureIndex = baseLabdata.FloorTextureIndex,
            FloorGraphic = baseLabdata.FloorGraphic,
            CeilingGraphic = baseLabdata.CeilingGraphic
        };

        labdata.Walls.AddRange(baseLabdata.Walls);
        labdata.WallGraphics.AddRange(baseLabdata.WallGraphics);
        labdata.ObjectInfos.AddRange(baseLabdata.ObjectInfos);
        labdata.ObjectGraphics.AddRange(baseLabdata.ObjectGraphics);
        labdata.Objects.AddRange(baseLabdata.Objects);

        var result = new DCLabdata(labdata);

        foreach (var sprite in DCContent.Sprites)
        {
            var (sourceIndex, objectIndex) = sprite.Value;

            if (sourceIndex == DCContent.BaseLabdata)
            {
                result.spriteObjects[sprite.Key] = objectIndex;
                continue;
            }

            var source = api.GetLabdata(sourceIndex);
            var sourceObject = source.Objects[(int)objectIndex - 1];
            var subObjects = new List<Labdata.ObjectPosition>();

            foreach (var subObject in sourceObject.SubObjects)
            {
                var graphic = source.ObjectGraphics[source.ObjectInfos.IndexOf(subObject.Object)];
                int infoIndex = labdata.ObjectInfos.IndexOf(subObject.Object);

                // The renderer finds the graphic by the index of the object info, so equal
                // infos must also share the same graphic. Different graphics are only possible
                // for different textures which also results in a different info.
                if (infoIndex == -1)
                {
                    labdata.ObjectInfos.Add(subObject.Object);
                    labdata.ObjectGraphics.Add(graphic);
                }

                subObjects.Add(subObject);
            }

            labdata.Objects.Add(new Labdata.Object
            {
                AutomapType = sourceObject.AutomapType,
                SubObjects = subObjects
            });

            result.spriteObjects[sprite.Key] = (uint)labdata.Objects.Count;
        }

        if (labdata.Objects.Count > 100)
            throw new InvalidOperationException("Too many labdata objects.");

        api.Maps.SetLabdata(LabdataIndex, labdata);

        return result;
    }
}
