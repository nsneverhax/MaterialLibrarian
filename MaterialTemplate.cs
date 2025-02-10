using MaterialLibrarian.IO;
using System.ComponentModel;
using System.Diagnostics;
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
    public List<MaterialTechnique> MaterialTechniques { get; set; } = [];
    public List<UIProperty> UIProperties { get; set; } = [];

    public MaterialTemplate(MatBinaryReader br) => Deserialize(br);


    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        var alwaysZero = br.ReadUInt32(); /* 0x00 */ // V: Always zero (allegedly)

        uint namePointer = br.ReadRelativePointer(); /* 0x04 */
        Checksum = br.ReadUInt32(); /* 0x08 */
        Flags = br.ReadUInt32(); /* 0x0C */

        UnknownMember3_0x0C = br.ReadUInt32(); /* 0x10 */
        UnknownMember4_0x10 = br.ReadUInt32(); /* 0x14 */

        Debug.WriteLineIf(UnknownMember4_0x10 != 0, $"{nameof(UnknownMember4_0x10)} was expected to be 0x0000 but was 0x{UnknownMember4_0x10:X}");

        MaterialTechniques = new(new MaterialTechnique[br.ReadUInt32()]); /* 0x18 */
        uint techniquePointer = br.ReadRelativePointer(); /* 0x1C */

        UIProperties = new(new UIProperty[br.ReadUInt32()]); /* 0x20 */
        uint propertyPointer = br.ReadRelativePointer(); /* 0x24 */

        Debug.WriteLineIf(br.Tell() - start != BinarySize, $"Invalid alignment of type {GetType().Name}! Got: {br.Tell() - start} Expected: {BinarySize}");
        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        Name = br.ReadStringAtAddress(namePointer);
        br.TEMP.Add((namePointer, $"NAME"));

        // Debug.WriteLine($"Reading Template: \"{Name}\'");
        if (MaterialTechniques.Count > 0)
        {
            br.Seek(techniquePointer);
            br.TEMP.Add((techniquePointer, $"TECHNIQUE[{MaterialTechniques.Count}]"));
            for (var i = 0; i < MaterialTechniques.Count; i++)
                MaterialTechniques[i] = new(br);
        }

        if (UIProperties.Count > 0)
        {
            br.Seek(propertyPointer);
            br.TEMP.Add((propertyPointer, $"Template/UI PROPERTIES[{UIProperties.Count}]"));
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
