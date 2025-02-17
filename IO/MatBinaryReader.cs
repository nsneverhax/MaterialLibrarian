using System.Security.Cryptography;

namespace MaterialLibrarian.IO;

public class MatBinaryReader : IDisposable
{
    public MatBinaryReader(FileStream stream)
    {
        BaseStream = stream;
    }

    public Stream BaseStream { get; private set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ((IDisposable)BaseStream).Dispose();
    }

    public List<(uint addr, string name)> TEMP = [];

    public uint Tell() => (uint)BaseStream.Position;
    public void Seek(uint offset, SeekOrigin origin = SeekOrigin.Begin) => BaseStream.Seek(offset, origin);

    public char ReadChar() => (char)BaseStream.ReadByte();
    public byte ReadByte() => (byte)BaseStream.ReadByte();

    public char PeekChar()
    {
        char c = ReadChar();
        BaseStream.Seek(-1, SeekOrigin.Current);
        return c;
    }

    public string ReadString()
    {
        string buff = string.Empty;

        while (PeekChar() != '\0')
            buff += ReadChar();

        return buff;
    }
    public ushort ReadUInt16(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();

        return BitConverter.ToUInt16(buffer);
    }

    public uint ReadUInt32(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();

        return BitConverter.ToUInt32(buffer);
    }
    public int ReadInt32(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();

        return BitConverter.ToInt32(buffer);
    }

    public float ReadFloat(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();

        return BitConverter.ToSingle(buffer);
    }

    public int ReadBytes(Span<byte> buffer) => BaseStream.Read(buffer);
    public int ReadBytes(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);

    /// <summary>
    /// The address of the start of the materials block
    /// </summary>
    public uint MaterialStart { get; internal set; } = 0;

    /// <summary>
    /// The address of the start of the shader micro-code block
    /// </summary>
    public uint MicroCodeAddress { get; internal set; } = 0;

    /// <summary>
    /// The Material Library we are current Reading
    /// </summary>
    public MaterialLibrary? MaterialLibrary { get; internal set; } = null;

    /// <summary>
    /// Reads a 32-Bit integer from the underlying stream, adds <c>MaterialStart</c> to it, and advances the stream by 4 bytes
    /// </summary>
    /// <param name="littleEndian"></param>
    /// <returns></returns>
    public uint ReadRelativePointer(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();
        int offset = BitConverter.ToInt32(buffer);

        return (uint)(MaterialStart + offset);
    }

    public string ReadStringAtAddress(uint address)
    {
        uint orig = Tell();

        Seek(address);

        string buff = string.Empty;

        while (PeekChar() != '\0')
            buff += ReadChar();

        Seek(orig);

        return buff;
    }
}
