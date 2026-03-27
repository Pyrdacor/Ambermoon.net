using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

public class Textures : IFileSpec<Textures>, IFileSpec
{
    private readonly Dictionary<int, Graphic> wallGraphics = [];
    private readonly Dictionary<int, Graphic> objectGraphics = [];
    private readonly Dictionary<int, Graphic> overlayGraphics = [];
    private readonly Dictionary<int, Graphic> floorGraphics = [];
    private readonly List<Graphic> backgroundGraphics = [];

    public static string Magic => "TEX";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    public IReadOnlyDictionary<int, Graphic> WallGraphics => wallGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> ObjectGraphics => objectGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> OverlayGraphics => overlayGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> FloorGraphics => floorGraphics.AsReadOnly();
    public IReadOnlyList<Graphic> BackgroundGraphics => backgroundGraphics.AsReadOnly();

    public Textures()
    {

    }

    public Textures
    (
        Dictionary<int, Graphic> wallGraphics,
        Dictionary<int, Graphic> objectGraphics,
        Dictionary<int, Graphic> overlayGraphics,
        Dictionary<int, Graphic> floorGraphics,
        List<Graphic> backgroundGraphics
    )
    {
        this.wallGraphics = wallGraphics;
        this.objectGraphics = objectGraphics;
        this.overlayGraphics = overlayGraphics;
        this.floorGraphics = floorGraphics;
        this.backgroundGraphics = backgroundGraphics;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int numWallGraphics = dataReader.ReadWord();
        int numObjectGraphics = dataReader.ReadWord();
        int numOverlayGraphics = dataReader.ReadWord();
        int numFloorGraphics = dataReader.ReadWord();
        int numBackgroundGraphics = dataReader.ReadByte();

        Graphic LoadGraphic(int width, int height)
        {
            return new Graphic
            {
                Width = width,
                Height = height,
                IndexedGraphic = true,
                Data = dataReader.ReadBytes(width * height)
            };
        }

        Graphic ReadEncodedGraphic()
        {
            int width = SmallEncodedInt.Read(dataReader);
            int height = SmallEncodedInt.Read(dataReader);

            return LoadGraphic(width, height);
        }

        for (int i = 0; i < numWallGraphics; i++)
        {
            wallGraphics.Add(i, LoadGraphic(128, 80));
        }

        for (int i = 0; i < numObjectGraphics; i++)
        {
            objectGraphics.Add(i, ReadEncodedGraphic());
        }

        for (int i = 0; i < numOverlayGraphics; i++)
        {
            overlayGraphics.Add(i, ReadEncodedGraphic());
        }

        for (int i = 0; i < numFloorGraphics; i++)
        {
            floorGraphics.Add(i, LoadGraphic(64, 64));
        }

        for (int i = 0; i < numBackgroundGraphics; i++)
        {
            backgroundGraphics.Add(LoadGraphic(144, 20));
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((ushort)wallGraphics.Count);
        dataWriter.Write((ushort)objectGraphics.Count);
        dataWriter.Write((ushort)overlayGraphics.Count);
        dataWriter.Write((ushort)floorGraphics.Count);
        dataWriter.Write((byte)backgroundGraphics.Count);

        void WriteGraphic(Graphic graphic, bool encoded)
        {
            if (graphic.Width == 0 || graphic.Height == 0)
                throw new InvalidOperationException("Graphic dimensions must not be 0");

            if (graphic.Width > 2048 || graphic.Height > 65536)
                throw new InvalidOperationException("Graphic dimensions must not be exceed 2048x65536");

            if (encoded)
            {
                SmallEncodedInt.Write(dataWriter, (short)graphic.Width);
                SmallEncodedInt.Write(dataWriter, (short)graphic.Height);
            }

            dataWriter.Write(graphic.Data);
        }

        foreach (var wallGraphic in wallGraphics.Values)
        {
            WriteGraphic(wallGraphic, false);
        }

        foreach (var objectGraphic in objectGraphics.Values)
        {
            WriteGraphic(objectGraphic, true);
        }

        foreach (var overlayGraphic in overlayGraphics.Values)
        {
            WriteGraphic(overlayGraphic, true);
        }

        foreach (var floorGraphic in floorGraphics.Values)
        {
            WriteGraphic(floorGraphic, false);
        }

        foreach (var backgroundGraphic in backgroundGraphics)
        {
            WriteGraphic(backgroundGraphic, false);
        }
    }
}
