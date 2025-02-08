namespace MaterialLibrarian;

/// Vultu: I want to share some common terminology you will see in this codebase
/// XPointer - defines a pointer to another region
/// XOffset - defines a relative pointer
/// 
public class MaterialLibrary
{
    public static readonly byte[] ExpectedMagic = { (byte)'M', (byte)'A', (byte)'T', (byte)'L' };
    public uint Version { get; set; } = 0;

    public List<MaterialTemplate> MaterialTemplates { get; set; } = new List<MaterialTemplate>();
    
    public void Write(string filepath)
    {
        
    }
    public void Read(string filepath)
    {
        using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        using MatBinaryReader br = new MatBinaryReader(fs);

        Deserialize(br);

        fs.Close();
    }

    public void Deserialize(MatBinaryReader br)
    {
        byte[] magic = new byte[4];

        br.ReadBytes(magic);
#if !MATLIB_NOEXCEPT
        if (!magic.SequenceEqual(ExpectedMagic))
            throw new Exception($"Invalid Header: expected \"{ExpectedMagic}\" but got \"{magic}\"");
#endif
        Version = br.ReadUInt32();

        MaterialTemplates = new(new MaterialTemplate[br.ReadUInt32()]);


        // V: This pointer is relative to the end of the pointer table
        br.MicroCodeOffset = br.ReadUInt32() + (Version == 2 ? 20u : 16u) + ((uint)(MaterialTemplates.Count + 1) * sizeof(uint));

        // V: Read MaterialTemplates Pointer Table
        uint[] templatePointers = new uint[MaterialTemplates.Count];
        for (var i = 0; i < MaterialTemplates.Count; i++)
            templatePointers[i] = br.ReadUInt32();



        br.ReadUInt32(); // material start

        br.MaterialStart = br.Tell();

        br.MaterialLibrary = this;

        // V: I could combine these, but I want this code to be readable
        for (var i = 0; i < MaterialTemplates.Count; i++)
        {
            //var orig = br.Tell();

            MaterialTemplates[i] = new(br);

            //br.Seek(orig);
        }
    }


}
