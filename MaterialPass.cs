namespace MaterialLibrarian;

public class MaterialPass
{
    public const uint BinarySize = 176;

    public List<RenderState> RenderStates { get; set; } = new();

    public uint UnknownMember0x00 { get; set; } = 0;
    public uint UnknownMember0x04 { get; set; } = 0;

    public uint VSPropCount { get; set; } = 0;
    public uint PSPropCount { get; set; } = 0;
    public uint VSPropCountCopy { get; set; } = 0;
    public uint PSPropCountCopy { get; set; } = 0;
    public uint SampleCount { get; set; } = 0;

    public uint OtherA { get; set; } = 0;

    public ItemCountAddress VSConstant { get; set; } = new ItemCountAddress();
    public ItemCountAddress PSConstant { get; set; } = new ItemCountAddress();
    public uint OtherB { get; set; } = 0;


    public List<ShaderProperty> ShaderProperties { get; set; } = new();
    public List<ShaderProperty> GlobalProperties { get; set; } = new();
    public byte[] UnknownStructureA { get; private set; } = new byte[20];
    public ItemCountAddress Strange { get; set; } = new ItemCountAddress();
    public byte[] UnknownStructureB { get; private set; } = new byte[20];
    public List<UIProperty> UIProperties { get; set; } = new();
    public byte[] UnknownStructureC { get; private set; } = new byte[40];

    public string Name { get; set; } = string.Empty;

    public MaterialPass() { }
    public MaterialPass(MatBinaryReader br) => Deserialize(br);

    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        uint namePointer = br.ReadRelativePointer();

        RenderStates = new(new RenderState[br.ReadUInt32()]);
        uint renderStatePointer = br.ReadRelativePointer();

        if (br.MaterialLibrary.Version < 4)
        {
            UnknownMember0x00 = br.ReadUInt32();
            UnknownMember0x04 = br.ReadUInt32();
        }

        VSPropCount = br.ReadUInt32();
        PSPropCount = br.ReadUInt32();
        VSPropCountCopy = br.ReadUInt32();
        PSPropCountCopy = br.ReadUInt32();
        SampleCount = br.ReadUInt32();

        OtherA = br.ReadUInt32();
        VSConstant.Deserialize(br);
        PSConstant.Deserialize(br);
        OtherB = br.ReadUInt32();

        ShaderProperties = new(new ShaderProperty[br.ReadUInt32()]);
        uint shaderPointer = br.ReadRelativePointer();

        // V: Random ass endian flip
        GlobalProperties = new(new ShaderProperty[br.ReadUInt32(true)]);
        uint globalPointer = br.ReadRelativePointer(true);

        br.ReadBytes(UnknownStructureA);

        Strange.Deserialize(br);
        if (Strange.Count != 0)
            Console.WriteLine($"Strange found with count: {Strange.Count} @ 0x{Strange.Address.ToString("X")}. Please report this to vultu with your Material!");

        br.ReadBytes(UnknownStructureB);

        UIProperties = new(new UIProperty[br.ReadUInt32()]);
        uint uiPointer = br.ReadRelativePointer();

        if (br.MaterialLibrary.Version < 4)
            br.ReadBytes(UnknownStructureC, 0, 40);
        else
            br.ReadBytes(UnknownStructureC, 0, 32);

        var size = br.Tell() - start;

        if (size != BinarySize)
            Console.WriteLine($"Invalid alignment of type {GetType().Name}! Got: {size} Expected: {BinarySize}");

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        Name = br.ReadStringAtAddress(namePointer);

        br.Seek(renderStatePointer);
        for (var i = 0; i < RenderStates.Count; i++)
            RenderStates[i] = new(br);

        br.Seek(shaderPointer);
        for (var i = 0; i < ShaderProperties.Count; i++)
            ShaderProperties[i] = new(br);

        br.Seek(globalPointer);
        for (var i = 0; i < GlobalProperties.Count; i++)
            GlobalProperties[i] = new(br);

        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}
