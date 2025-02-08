using MaterialLibrarian;
using System;
namespace MaterialLibrarian;

// Vultu: Not sure what else to call this
public struct ItemCountAddress
{
    public ItemCountAddress()
    {

    }

    //public void Serialize(BinaryWriter bw, uint matStart = 0)
    //{
    //    bw.WriteBE((uint)Count);
    //    bw.WriteBE((int)(Address - matStart));
    //}
    public void Deserialize(MatBinaryReader br) 
    {
        Count = br.ReadUInt32();
        Address = br.ReadRelativePointer();
    }

    public uint Count { get; set; } = 0;
    public uint Address { get; set; } = 0;
}
