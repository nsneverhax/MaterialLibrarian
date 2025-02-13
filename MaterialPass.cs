using MaterialLibrarian.IO;
using System.Diagnostics;

namespace MaterialLibrarian;

public class UnknownMaterialChild
{
    public const uint BinarySize = 0x1A;

    public uint[] Data = new uint[0x1A / sizeof(uint)];

    public UnknownMaterialChild() { }
    public UnknownMaterialChild(MatBinaryReader br)
    {
        Deserialize(br);
    }

    public void Deserialize(MatBinaryReader br)
    {
        for (var i = 0; i < Data.Length; i++)
            Data[i] += br.ReadUInt32();
    }
}

[DebuggerDisplay("{ItemA}, {ItemB}, {ItemC}")]
public class UnknownStructureTemplate
{
    public const uint BinarySize = 0x14;

    public ItemCountAddress ItemA { get; set; } = new(); /* 0x00 */
    public ItemCountAddress ItemB { get; set; } = new(); /* 0x08 */
    public uint ItemC { get; set; } = new(); /* 0x10 */

    public UnknownStructureTemplate() { }
    public UnknownStructureTemplate(MatBinaryReader br)
    {
        Deserialize(br);
    }

    public void Deserialize(MatBinaryReader br)
    {
        ItemA.Deserialize(br);
        ItemB.Deserialize(br);
        ItemC = br.ReadUInt32();
    }
}
public class UnknownStructureC
{
    public uint Item00 = 0; /* 0x00 */
    public uint Item01 = 1; /* 0x04 */
    public uint Item02 = 0; /* 0x08 */
    public uint Item03 = 0; /* 0x0C */

    public uint Item04 = 0; /* 0x10 */
    public uint Item05 = 1; /* 0x14 */
    public uint Item06 = 0; /* 0x18 */
    public uint Item07 = 0; /* 0x1C */

    public uint Item08 = 0; /* 0x20 */
    public uint Item09 = 1; /* 0x24 */

    public bool IsZero =>
        Item00 == 0 &&
        Item01 == 0 &&
        Item02 == 0 &&
        Item03 == 0 &&
        Item04 == 0 &&
        Item05 == 0 &&
        Item06 == 0 &&
        Item07 == 0 &&
        Item08 == 0 &&
        Item09 == 0;
    public UnknownStructureC() { }
    public UnknownStructureC(MatBinaryReader br)
    {
        Deserialize(br);
    }

    public void Deserialize(MatBinaryReader br)
    {
        Item00 = br.ReadUInt32(); // 0x00
        Item01 = br.ReadUInt32(); // 0x04
        Item02 = br.ReadUInt32(); // 0x08
        Item03 = br.ReadUInt32(); // 0x0C
        Item04 = br.ReadUInt32(); // 0x10
        Item05 = br.ReadUInt32(); // 0x14
        Item06 = br.ReadUInt32(); // 0x18
        Item07 = br.ReadUInt32(); // 0x1C
        if (br.MaterialLibrary.Version >= 4)
            return;
        Item08 = br.ReadUInt32(); // 0x20
        Item09 = br.ReadUInt32(); // 0x24
    }
}
public class MaterialPass
{
    public const uint BinarySize = 0xB0;

    public List<RenderState> RenderStates { get; set; } = [];

    public List<UnknownMaterialChild> UnknownChildren { get; set; } = [];
    public uint UnknownSize = 0;
    public uint UnknownPtr = 0;

    public uint VSPropCount { get; set; } = 0;
    public uint PSPropCount { get; set; } = 0;

    // V: Supposedly a copy of the above
    public uint VSPropCountCopy { get; set; } = 0;
    public uint PSPropCountCopy { get; set; } = 0;

    public uint SampleCount { get; set; } = 0;

    public uint OtherA { get; set; } = 0;

    public ItemCountAddress VSConstant { get; set; } = new ItemCountAddress();
    public ItemCountAddress PSConstant { get; set; } = new ItemCountAddress();
    public uint OtherB { get; set; } = 0;


    public List<ShaderProperty> ShaderProperties { get; set; } = [];
    public List<ShaderProperty> GlobalProperties { get; set; } = [];
    public UnknownStructureTemplate UnknownStructureA { get; private set; } = new();
    public ItemCountAddress Strange { get; set; } = new ItemCountAddress();
    public UnknownStructureTemplate UnknownStructureB { get; private set; } = new();
    public List<UIProperty> UIProperties { get; set; } = [];

    public UnknownStructureC UnknownStructureC { get; private set; } = new();

    public string Name { get; set; } = string.Empty;

    public MaterialPass() { }
    public MaterialPass(MatBinaryReader br) => Deserialize(br);
    
    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        uint namePointer = br.ReadRelativePointer();

        RenderStates = new(new RenderState[br.ReadUInt32()]);
        uint renderStatePointer = br.ReadRelativePointer();
        
#if !MATLIB_NOEXCEPT
        if (br.MaterialLibrary is null)
            throw new NullReferenceException(nameof(br.MaterialLibrary));
#endif

        if (br.MaterialLibrary.Version < 4)
        {

            UnknownChildren = new(new UnknownMaterialChild[br.ReadUInt32()]);
            UnknownPtr = br.ReadRelativePointer();

            var orig = br.Tell();
            if (UnknownChildren.Count > 0)
                br.TEMP.Add((UnknownPtr, $"UnknownMatChild[{UnknownChildren.Count}]"));
            br.Seek(UnknownPtr);
            for (var i = 0; i < UnknownChildren.Count; i++)
                UnknownChildren[i] = new (br);

            br.Seek(orig);
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

        UnknownStructureA.Deserialize(br);

        Strange.Deserialize(br);

        UnknownStructureB.Deserialize(br);
        Debug.WriteLineIf(UnknownStructureB.ItemC != 0, $"UnknownStructureB.ItemC = {UnknownStructureB.ItemC}");
        UIProperties = new(new UIProperty[br.ReadUInt32()]);
        uint uiPointer = br.ReadRelativePointer();

        UnknownStructureC.Deserialize(br);
        Debug.WriteLineIf(br.Tell() - start != BinarySize, $"Invalid alignment of type {GetType().Name}! Got: {br.Tell() - start} Expected: {BinarySize}");

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        Name = br.ReadStringAtAddress(namePointer);

        if (Strange.Count != 0)
            Console.WriteLine($"{Name}: Strange found with count: {Strange.Count} @ 0x{Strange.Address:X}. Please report this to vultu with your Material!");

        Debug.WriteLineIf(VSPropCount != VSPropCountCopy, $"{Name}: {nameof(VSPropCount)} mistmatch! {VSPropCount} != {VSPropCountCopy}");
        Debug.WriteLineIf(PSPropCount != PSPropCountCopy, $"{Name}: {nameof(PSPropCount)} mistmatch! {PSPropCount} != {PSPropCountCopy}");

        Debug.WriteLineIf(OtherA != 0, $"{Name}: {nameof(OtherA)} was not 0x00 it was 0x{OtherA:X}");
        Debug.WriteLineIf(VSConstant.Count != 0, $"{Name}: {nameof(VSConstant.Count)} was not empty it has {VSConstant.Count:X} members.");
        Debug.WriteLineIf(PSConstant.Count != 0, $"{Name}: {nameof(PSConstant.Count)} was not empty it has {PSConstant.Count:X} members.");
        Debug.WriteLineIf(OtherB != 0, $"{Name}: {nameof(OtherB)} was not 0x00 it was 0x{OtherB:X}");

        //for (var i = 0; i < UnknownStructureC.Length; i++)
        //    Debug.WriteLineIf(UnknownStructureC[i] != 0, $"{nameof(UnknownStructureC)}[{i}] == 0x{UnknownStructureC[i]:X}");

        if (RenderStates.Count > 0)
        {
            br.Seek(renderStatePointer);
            br.TEMP.Add((renderStatePointer, $"RENDERSTATE[{RenderStates.Count}]"));
            for (var i = 0; i < RenderStates.Count; i++)
                RenderStates[i] = new(br);
        }

        if (ShaderProperties.Count > 0)
        {
            br.Seek(shaderPointer);
            br.TEMP.Add((shaderPointer, $"Pass/SHADER PROP[{ShaderProperties.Count}]"));
            for (var i = 0; i < ShaderProperties.Count; i++)
                ShaderProperties[i] = new(br);
        }

        if (GlobalProperties.Count > 0)
        {
            br.Seek(globalPointer);
            br.TEMP.Add((globalPointer, $"Pass/GLOBAL PROP[{GlobalProperties.Count}]"));
            for (var i = 0; i < GlobalProperties.Count; i++)
                GlobalProperties[i] = new(br);
        }
        
        if (UIProperties.Count > 0)
        {
            br.Seek(uiPointer);
            br.TEMP.Add((uiPointer, $"Pass/Prop[{UIProperties.Count}]"));
            for (var i = 0; i < UIProperties.Count; i++)
                UIProperties[i] = new(br);
        }
        
        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}
