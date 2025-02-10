using MaterialLibrarian.IO;
using System;
using System.Diagnostics;
namespace MaterialLibrarian;

// Vultu: Not sure what else to call this

[DebuggerDisplay("Count = {Count}, Address = {Address}")]
public struct ItemCountAddress
{
    public uint Count { get; set; } = 0;
    public uint Address { get; set; } = 0;

    public ItemCountAddress()
    {

    }

    public void Deserialize(MatBinaryReader br) 
    {
        Count = br.ReadUInt32();
        Address = br.ReadRelativePointer();
    }

}
