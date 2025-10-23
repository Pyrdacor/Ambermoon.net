﻿using Ambermoon.Data.GameDataRepository.Data;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository;

public class Palette : IIndexed, IMutableIndex, ICloneable
{

    #region Fields

    private readonly byte[] _data = new byte[GameDataRepository.PaletteSize * 4];

    #endregion


    #region Properties

    uint IMutableIndex.Index
    {
        get;
        set;
    }

    public uint Index => (this as IMutableIndex).Index;

    #endregion


    #region Constructors

    public Palette()
    {

    }

    public Palette(uint index, byte[] amigaPalette)
    {
        if (amigaPalette.Length != GameDataRepository.PaletteSize * 2)
            throw new ArgumentException("Invalid Amiga palette size.");

        for (int i = 0; i < GameDataRepository.PaletteSize; ++i)
        {
            int r = amigaPalette[i * 2] & 0xf;
            int gb = amigaPalette[i * 2 + 1];
            int g = gb >> 4;
            int b = gb & 0xf;

            _data[i * 4 + 0] = (byte)(b | (b << 4));
            _data[i * 4 + 1] = (byte)(g | (g << 4));
            _data[i * 4 + 2] = (byte)(r | (r << 4));
            _data[i * 4 + 3] = 0xff;
        }

        (this as IMutableIndex).Index = index;
    }

    #endregion


    #region Methods

    public void CopyColor(Span<byte> target, int targetIndex, int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= 32)
            throw new ArgumentOutOfRangeException(nameof(colorIndex));

        _data.AsSpan(colorIndex * 4, 4).CopyTo(target.Slice(targetIndex, 4));
    }

    public void CopyColor(Palette target, int targetColorIndex, int colorIndex)
    {
        CopyColor(target._data.AsSpan(), targetColorIndex * 4, colorIndex);
    }

    public int GetColorIndex(ReadOnlySpan<byte> source, int sourceIndex)
    {
        var sourceSlice = source.Slice(sourceIndex, 4);

        for (int i = 0; i < 32; ++i)
        {
            if (_data.AsSpan(i * 4, 4).SequenceEqual(sourceSlice))
                return i;
        }

        return -1;
    }

    public uint GetColor(uint colorIndex)
    {
        if (colorIndex >= 32)
            throw new ArgumentOutOfRangeException(nameof(colorIndex));

        colorIndex *= 4;
        return (uint)(_data[colorIndex++] | (_data[colorIndex++] << 8) | (_data[colorIndex++] << 16) | (_data[colorIndex] << 24));
    }

    public uint[] GetColors()
    {
        var colors = new uint[32];

        for (int i = 0; i < 32; ++i)
            colors[i] = GetColor((uint)i);

        return colors;
    }

    #endregion


    #region Serialization

    public static Palette Deserialize(uint index, IDataReader dataReader)
    {
        return new Palette(index, dataReader.ReadBytes(GameDataRepository.PaletteSize * 2));
    }

    #endregion


    #region Cloning

    public Palette Copy()
    {
        var copy = new Palette();
        (copy as IMutableIndex).Index = Index;
        _data.CopyTo(copy._data, 0);
        return copy;
    }

    public Palette WithTransparency(uint transparentColorIndex = 0)
    {
        if (transparentColorIndex >= 32)
            throw new ArgumentOutOfRangeException(nameof(transparentColorIndex));

        var copy = Copy();
        copy._data[transparentColorIndex * 4 + 3] = 0; // Set alpha to 0

        return copy;
    }

    public Palette WithColorReplacement(uint index, byte r, byte g, byte b)
    {
        if (index >= 32)
            throw new ArgumentOutOfRangeException(nameof(index));

        var copy = Copy();

        index *= 4;
        copy._data[index++] = r;
        copy._data[index++] = g;
        copy._data[index++] = b;
        copy._data[index] = 0xff;

        return copy;
    }

    public object Clone() => Copy();

    #endregion

}
