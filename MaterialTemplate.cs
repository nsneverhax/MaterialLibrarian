using System.ComponentModel;
using System.Drawing;
using System.Reflection.PortableExecutable;

namespace MaterialLibrarian;

public class MaterialTemplate
{
    public const uint BinarySize = 0x28;

    public uint Checksum { get; set; } = 0;
    public uint Flags { get; set; } = 4; // Vultu: ??
    public uint UnknownMember3_0x0C { get; set; } = 0;
    public uint UnknownMember4_0x10 { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public List<MaterialTechnique> MaterialTechniques { get; set; } = new();
    public List<UIProperty> UIProperties { get; set; } = new();

    public MaterialTemplate(MatBinaryReader br) => Deserialize(br);



    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        var alwaysZero = br.ReadUInt32(); // V: Always zero (allegedly)

        uint namePointer = br.ReadRelativePointer();
        Checksum = br.ReadUInt32();
        Flags = br.ReadUInt32();

        UnknownMember3_0x0C = br.ReadUInt32();
        UnknownMember4_0x10 = br.ReadUInt32();

        MaterialTechniques = new(new MaterialTechnique[br.ReadUInt32()]);
        uint techniquePointer = br.ReadRelativePointer();

        UIProperties = new(new UIProperty[br.ReadUInt32()]);
        uint propertyPointer = br.ReadRelativePointer();

                var size = br.Tell() - start;

        if (size != BinarySize)
            Console.WriteLine($"Invalid alignment of type {GetType().Name}! Got: {size} Expected: {BinarySize}");

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        Name = br.ReadStringAtAddress(namePointer);

        // Console.WriteLine($"Reading Template: \"{Name}\'");
        br.Seek(techniquePointer);
        for (var i = 0; i < MaterialTechniques.Count; i++)
            MaterialTechniques[i] = new(br);

        br.Seek(propertyPointer);
        for (var i = 0; i < UIProperties.Count; i++)
            UIProperties[i] = new(br);

        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}
