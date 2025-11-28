using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DeepDungeonRadar.Utils;

// utility for working with 2d 1bpp bitmaps
// some notes:
// - supports only BITMAPINFOHEADER (could've been BITMAPCOREHEADER, but bottom-up bitmaps don't make sense with FF coordinate system)
// - supports only 1bpp bitmaps without compression; per bitmap spec, first pixel is highest bit, etc.
// - supports only top-down bitmaps (with negative height)
// - horizontal/vertical resolution is equal and is 'pixels per 1024 world units'
// - per bitmap spec, rows are padded to 4 byte alignment
public sealed class Bmp1pp
{
    [StructLayout(LayoutKind.Explicit, Size = 14)]
    public struct FileHeader
    {
        [FieldOffset(0)] public ushort Type; // 0x4D42 'BM'
        [FieldOffset(2)] public int Size; // size of the file in bytes
        [FieldOffset(6)] public uint Reserved;
        [FieldOffset(10)] public int OffBits; // offset from this to pixel data
    }

    public struct BitmapInfoHeader
    {
        public int SizeInBytes;
        public int Width;
        public int Height;
        public short PlaneCount;
        public short BitCount;
        public int Compression;
        public int SizeImage;
        public int XPixelsPerMeter;
        public int YPixelsPerMeter;
        public int ColorUsedCount;
        public int ColorImportantCount;
    }

    public const ushort Magic = 0x4D42;

    public readonly int Width;
    public readonly int Height;
    public readonly int Stride;
    public readonly int Resolution; // pixels per 1024 world units
    public readonly uint Color0;
    public readonly uint Color1;
    public readonly byte[] Pixels;

    public float PixelSize => 1024.0f / Resolution;
    public int CoordToIndex(int x, int y) => y * Stride + (x >> 3);
    public byte CoordToMask(int x) => (byte)(0x80u >> (x & 7));
    public ref byte ByteAt(int x, int y) => ref Pixels[CoordToIndex(x, y)];

    public bool this[int x, int y]
    {
        get => (ByteAt(x, y) & CoordToMask(x)) != 0;
        set
        {
            if (value)
                ByteAt(x, y) |= CoordToMask(x);
            else
                ByteAt(x, y) &= (byte)~CoordToMask(x);
        }
    }

    public Bmp1pp(int width, int height, uint color0, uint color1, int resolution = 2048)
    {
        Width = width;
        Height = height;
        Stride = width + 31 >> 5 << 2;
        Resolution = resolution;
        Color0 = color0;
        Color1 = color1;
        Pixels = new byte[height * Stride];
    }

    public Bmp1pp(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var fileHeader = stream.ReadStruct<FileHeader>();
        if (fileHeader.Type != Magic)
            throw new ArgumentException($"Not a bitmap: magic is {fileHeader.Type:X4}");

        var header = stream.ReadStruct<BitmapInfoHeader>();
        if (header.SizeInBytes != Marshal.SizeOf<BitmapInfoHeader>())
            throw new ArgumentException($"Bitmap has unsupported header size {header.SizeInBytes}");
        if (header.Width <= 0)
            throw new ArgumentException($"Bitmap has non-positive width {header.Width}");
        if (header.Height >= 0)
            throw new ArgumentException($"Bitmap is not top-down (height={header.Height})");
        if (header.BitCount != 1)
            throw new ArgumentException($"Bitmap is not 1bpp (bitcount={header.BitCount})");
        if (header.Compression != 0)
            throw new ArgumentException($"Bitmap has unsupported compression method {header.Compression:X8}");
        if (header.XPixelsPerMeter != header.YPixelsPerMeter || header.XPixelsPerMeter <= 0)
            throw new ArgumentException($"Bitmap has inconsistent or non-positive resolution {header.XPixelsPerMeter}x{header.YPixelsPerMeter}");
        if (header.ColorUsedCount is not 0 or 2)
            throw new ArgumentException($"Bitmap has wrong palette size {header.ColorUsedCount}");

        Width = header.Width;
        Height = -header.Height;
        Stride = Width + 31 >> 5 << 2;
        Resolution = header.XPixelsPerMeter;
        Color0 = reader.ReadUInt32();
        Color1 = reader.ReadUInt32();
        Pixels = reader.ReadBytes(Height * Stride);
    }

    public void Save(string filename)
    {
        using var fstream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
        var headerSize = Marshal.SizeOf<FileHeader>() + Marshal.SizeOf<BitmapInfoHeader>() + 2 * Marshal.SizeOf<uint>();
        fstream.WriteStruct(new FileHeader() { Type = Magic, Size = headerSize + Pixels.Length, OffBits = headerSize });
        fstream.WriteStruct(new BitmapInfoHeader() { SizeInBytes = Marshal.SizeOf<BitmapInfoHeader>(), Width = Width, Height = -Height, PlaneCount = 1, BitCount = 1, XPixelsPerMeter = Resolution, YPixelsPerMeter = Resolution });
        fstream.WriteStruct(Color0);
        fstream.WriteStruct(Color1);
        fstream.Write(Pixels);
    }

    public Bmp1pp Clone()
    {
        var res = new Bmp1pp(Width, Height, Color0, Color1, Resolution);
        Array.Copy(Pixels, res.Pixels, Pixels.Length);
        return res;
    }
}
