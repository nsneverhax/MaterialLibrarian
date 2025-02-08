
namespace MaterialLibrarian;
public struct RenderState
{
    public RenderState(MatBinaryReader br)
    {
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

