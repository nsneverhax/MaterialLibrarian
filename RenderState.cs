
using MaterialLibrarian.IO;

namespace MaterialLibrarian;
public struct RenderState
{
    public const uint BinarySize = 0x8;
    public RenderState(MatBinaryReader br)
    {
        br.TEMP.Add((br.Tell(), this.GetType().Name));

        UnknownMember1_0x00 = br.ReadUInt32();
        UnknownMember2_0x04 = br.ReadUInt32();
    }

    public uint UnknownMember1_0x00;
    public uint UnknownMember2_0x04;

    //public void Serialize(MaterialLibraryWriter bw)
    //{
    //    bw.WriteBE(UnknownMember1_0x00);
    //    bw.WriteBE(UnknownMember2_0x04);
    //}
}

