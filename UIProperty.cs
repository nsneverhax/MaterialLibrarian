using System.Security.Cryptography.X509Certificates;

namespace MaterialLibrarian;

public enum UIPropertyType : uint
{
    FLOAT = 0,
    INTEGER,
    UIPROP_2,
    UIPROP_3,
    NORMAL
}

public class UIProperty
{
    public string Name { get; set; } = string.Empty;
    public uint Checksum { get; set; } = 0;
    public UIPropertyType PropertyType = UIPropertyType.NORMAL;

    public dynamic? Value { get; set; } = null;

    public UIProperty() { }
    public UIProperty(MatBinaryReader br) => Deserialize(br);

    public void Deserialize(MatBinaryReader br)
    {
        uint namePointer = br.ReadRelativePointer();
        Checksum = br.ReadUInt32();

        uint propertyType = br.ReadUInt32();
#if !MATLIB_NOEXCEPT
        if (!Enum.IsDefined(typeof(UIPropertyType), propertyType))
            throw new UnexpectedEnumValueException<UIPropertyType>((int)propertyType);
#endif 
        br.ReadUInt32(); // Length
        uint valuePointer = br.ReadRelativePointer();

        PropertyType = (UIPropertyType)propertyType;


        // V: Now that we have read the actual structure data, we need to jump around the file to get the rest of it

        // ---------------------------
        uint oldAddress = br.Tell();
        // ---------------------------

        br.Seek(valuePointer);

        Value = PropertyType switch
        {
            UIPropertyType.FLOAT => br.ReadFloat(),
            UIPropertyType.INTEGER => br.ReadUInt32(),
            UIPropertyType.NORMAL => br.ReadString(),
            _ => br.ReadUInt32()
        };

        // ---------------------------
        // V: Return so we can read the next struct, even though
        // it shouldn't matter, becuase these are *supposed* to be
        // contiguous in memory.
        br.Seek(oldAddress);
    }
}
