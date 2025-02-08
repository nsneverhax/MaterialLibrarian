using System.Xml.Linq;

namespace MaterialLibrarian;

public class MaterialTechniqueSubObject
{
    public byte[] Version4Structure { get; private set; } = new byte[88];

    public uint VertexShaderMemoryOffset { get; private set; } = 0;
    public uint PixelShaderMemoryOffset { get; private set; } = 0;


    public uint vCTABConstant { get; set; } = 0;
    public uint pCTABConstant { get; set; } = 0;

    public ItemCountAddress A { get; set; } = new();
    public ItemCountAddress B { get; set; } = new();
    public ItemCountAddress C { get; set; } = new();

    public ItemCountAddress Sampler { get; set; } = new();

    public uint UnknownA { get; set; } = 0;
    public uint UnknownB { get; set; } = 0;

    public byte[] VertexShaderCode { get; private set; } = Array.Empty<byte>();
    public byte[] PixelShaderCode { get; private set; } = Array.Empty<byte>();

    public MaterialTechniqueSubObject() { }
    public MaterialTechniqueSubObject(MatBinaryReader br) => Deserialize(br);

    public void Deserialize(MatBinaryReader br)
    {
        VertexShaderCode = new byte[br.ReadUInt32()];
        vCTABConstant = br.ReadUInt32();
        var vCTABPointer = br.MicroCodeOffset + br.ReadUInt32();
        VertexShaderMemoryOffset = br.ReadUInt32();

        PixelShaderCode = new byte[br.ReadUInt32()];
        pCTABConstant = br.ReadUInt32();
        var pCTABPointer = br.MicroCodeOffset + br.ReadUInt32();
        PixelShaderMemoryOffset = br.ReadUInt32();

        A.Deserialize(br);
        B.Deserialize(br);
        C.Deserialize(br);

        Sampler.Deserialize(br);

        UnknownA = br.ReadUInt32();
        UnknownB = br.ReadUInt32();

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        if (VertexShaderCode.Length > 0)
        {
            br.Seek(vCTABPointer);
            br.ReadBytes(VertexShaderCode);
        }

        if (PixelShaderCode.Length > 0)
        {
            br.Seek(vCTABPointer);
            br.ReadBytes(PixelShaderCode);
        }

        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}

public class MaterialTechnique
{
    public const uint BinarySize = 440;

    public uint Checksum { get; set; } = 0;
    public uint UnknownMember2_0x04 { get; set; } = 0;
    public List<MaterialPass> MaterialPasses { get; set; } = new();
    public List<uint> Flags { get; set; } = new();
    public uint ConstantB { get; set; } = 0;
    public byte[] SkipA { get; private set; } = new byte[12 * 4];
    public byte[] AboveVersion4Structure { get; set; } = new byte[8];

    public MaterialTechniqueSubObject[] SubObjects { get; private set; } = new MaterialTechniqueSubObject[5];

    public string Name { get; set; } = string.Empty;

    public MaterialTechnique() { }
    public MaterialTechnique(MatBinaryReader br) => Deserialize(br);

    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        uint namePointer = br.ReadRelativePointer();
        Checksum = br.ReadUInt32();

        UnknownMember2_0x04 = br.ReadUInt32();

        MaterialPasses = new(new MaterialPass[br.ReadUInt32()]);
        uint passPointer = br.ReadRelativePointer();

        Flags = new(new uint[br.ReadUInt32()]);
        uint flagsPointer = br.ReadRelativePointer();

        ConstantB = br.ReadUInt32();
        br.ReadBytes(SkipA);

        for (var i = 0; i < 5; i++)
            SubObjects[i] = new(br);

        if (br.MaterialLibrary!.Version >= 4)
            br.ReadBytes(AboveVersion4Structure);

        var size = br.Tell() - start;

        if (size != BinarySize)
            Console.WriteLine($"Invalid alignment of type {GetType().Name}! Got: {size} Expected: {BinarySize}");

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        Name = br.ReadStringAtAddress(namePointer);

        br.Seek(passPointer);
        for (var i = 0; i < MaterialPasses.Count; i++)
            MaterialPasses[i] = new(br);

        br.Seek(flagsPointer);
        for (var i = 0; i < Flags.Count; i++)
            Flags[i] = br.ReadUInt32();

        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}
