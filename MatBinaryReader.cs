
using System.Security.Cryptography;

namespace MaterialLibrarian;

public class MatBinaryReader : IDisposable
{
    public MatBinaryReader(FileStream stream)
    {
        BaseStream = stream;
    }

    public Stream BaseStream { get; private set; }

    public void Dispose()
    {
        ((IDisposable)BaseStream).Dispose();
    }

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

        while (PeekChar()  != '\0')
            buff += ReadChar();

        return buff;
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

    public int ReadBytes(Span<byte> buffer) =>  BaseStream.Read(buffer);
    public int ReadBytes(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);

    public uint MaterialStart { get; internal set; } = 0;
    public uint MicroCodeOffset { get; internal set; } = 0;

    public MaterialLibrary? MaterialLibrary { get; internal set; } = null;

    public uint ReadRelativePointer(bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        BaseStream.Read(buffer);

        if (!littleEndian)
            buffer.Reverse();

        return (uint)(MaterialStart + BitConverter.ToInt32(buffer));
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
