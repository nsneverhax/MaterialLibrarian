using MaterialLibrarian.IO;
using System.Diagnostics;
using System.Xml.Linq;

namespace MaterialLibrarian;

public enum ShaderPropertyType : byte
{
    VALUE = 0,
    SAMPLE,
    TYPE_2,
    TYPE_3,
    TYPE_4,
    MATRIX,
}

public class ShaderProperty
{
    public const uint BinarySize = 0x34;
    public uint MatStart { get; set; } = 0;
    public uint SkinMatIndex { get; set; } = 0;
    public string PropertyName { get; set; } = "m_UnkProperty";
    public string SampleName { get; set; } = string.Empty;

    public uint Checksum = 0;

    public ShaderPropertyType PropertyType { get; set; } = ShaderPropertyType.VALUE;
    public byte UVIndex { get; set; } = 0;

    public List<float> Defaults { get; set; } = [];
    public List<UIProperty> UIProperties { get; set; } = [];

    public uint UsuallyNull { get; set; } = 0;

    public byte UnknownByte1 { get; set; } = 0;
    public byte UnknownByte2 { get; set; } = 0;

    public uint UnknownIndexUInt1 { get; set; } = 0;
    public uint UnknownIndexUInt2 { get; set; } = 0;
    public uint UnknownIndexUInt3 { get; set; } = 0;

    public ShaderProperty() { }
    public ShaderProperty(MatBinaryReader br) => Deserialize(br);

    public void Deserialize(MatBinaryReader br)
    {
        uint start = br.Tell();

        uint namePointer = br.ReadRelativePointer();
        UsuallyNull = br.ReadUInt32();
        uint samplePointer = br.ReadRelativePointer();
        br.TEMP.Add((samplePointer, $"SAMPLE NAME"));
        Checksum = br.ReadUInt32();

        byte propertyType = br.ReadByte();
#if !MATLIB_NOEXCEPT
        if (!Enum.IsDefined(typeof(ShaderPropertyType), propertyType))
            throw new UnexpectedEnumValueException<ShaderPropertyType>(propertyType);
#endif 

        PropertyType = (ShaderPropertyType)propertyType;
        UVIndex = br.ReadByte();

        UnknownByte1 = br.ReadByte();
        UnknownByte2 = br.ReadByte();

        UnknownIndexUInt1 = br.ReadUInt32();
        UnknownIndexUInt2 = br.ReadUInt32();
        SkinMatIndex = br.ReadUInt32();
        UnknownIndexUInt3 = br.ReadUInt32();

        Defaults = new(new float[br.ReadUInt32()]);
        uint defaultPointer = br.ReadRelativePointer();

        UIProperties = new(new UIProperty[br.ReadUInt32()]);
        uint uiPointer = br.ReadRelativePointer();

        Debug.WriteLineIf(br.Tell() - start != BinarySize, $"Invalid alignment of type {GetType().Name}! Got: {br.Tell() - start} Expected: {BinarySize}");

        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        PropertyName = br.ReadStringAtAddress(namePointer);

        if (PropertyType == ShaderPropertyType.SAMPLE)
            SampleName = br.ReadStringAtAddress(samplePointer);

        if (Defaults.Count > 0)
        {
            br.Seek(defaultPointer);
            br.TEMP.Add((defaultPointer, $"Defaults[{Defaults.Count}]"));
            for (var i = 0; i < Defaults.Count; i++)
                Defaults[i] = br.ReadFloat(true); // V: Another random ass endian swap
        }

        if (UIProperties.Count > 0)
        {
            br.Seek(uiPointer);
            br.TEMP.Add((uiPointer, $"ShaderProp/UI PROPERTIES[{UIProperties.Count}]"));
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
