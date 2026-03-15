using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

public class Textures : IFileSpec<Textures>, IFileSpec
{
    private readonly Dictionary<int, Graphic> wallGraphics = [];
    private readonly Dictionary<int, Graphic> objectGraphics = [];
    private readonly Dictionary<int, Graphic> overlayGraphics = [];
    private readonly Dictionary<int, Graphic> floorGraphics = [];

    public static string Magic => "TEX";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    public IReadOnlyDictionary<int, Graphic> WallGraphics => wallGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> ObjectGraphics => objectGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> OverlayGraphics => overlayGraphics.AsReadOnly();
    public IReadOnlyDictionary<int, Graphic> FloorGraphics => floorGraphics.AsReadOnly();

    public Textures()
    {

    }

    public Textures
    (
        Dictionary<int, Graphic> wallGraphics,
        Dictionary<int, Graphic> objectGraphics,
        Dictionary<int, Graphic> overlayGraphics,
        Dictionary<int, Graphic> floorGraphics
    )
    {
        this.wallGraphics = wallGraphics;
        this.objectGraphics = objectGraphics;
        this.overlayGraphics = overlayGraphics;
        this.floorGraphics = floorGraphics;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int numWallGraphics = dataReader.ReadWord();
        int numObjectGraphics = dataReader.ReadWord();
        int numOverlayGraphics = dataReader.ReadWord();
        int numFloorGraphics = dataReader.ReadWord();

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
            int width = dataReader.ReadByte();
            int height;

            if ((width & 0x80) == 0)
                height = 1 + dataReader.ReadByte();
            else
                height = dataReader.ReadWord();

            width &= 0x7f;
            width++;
            width <<= 4;

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
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((ushort)wallGraphics.Count);
        dataWriter.Write((ushort)objectGraphics.Count);
        dataWriter.Write((ushort)overlayGraphics.Count);
        dataWriter.Write((ushort)floorGraphics.Count);

        void WriteGraphic(Graphic graphic, bool encoded)
        {
            if (graphic.Width == 0 || graphic.Height == 0)
                throw new InvalidOperationException("Graphic dimensions must not be 0");

            if (graphic.Width > 2048 || graphic.Height > 65536)
                throw new InvalidOperationException("Graphic dimensions must not be exceed 2048x65536");

            if (encoded)
            {
                int width = (graphic.Width >> 4) - 1;
                int height = graphic.Height - 1;

                if (height > 255)
                    width |= 0x80;

                dataWriter.Write((byte)width);
                if (height > 255)
                    dataWriter.Write((ushort)height);
                else
                    dataWriter.Write((byte)height);
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
    }
}
