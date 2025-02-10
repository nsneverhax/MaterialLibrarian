using MaterialLibrarian.IO;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace MaterialLibrarian;

/// Vultu: I want to share some common terminology you will see in this codebase
/// XPointer - defines a pointer to another region
/// XOffset - defines a relative pointer
/// 
public class MaterialLibrary
{
    public static readonly byte[] ExpectedMagic = [ (byte)'M', (byte)'A', (byte)'T', (byte)'L' ];
    public uint Version { get; set; } = 0;

    public List<MaterialTemplate> MaterialTemplates { get; set; } = [];

    public uint TemplateCount { get; internal set; } = 0;

    public void Write(string filepath)
    {
        using FileStream fs = new(filepath, FileMode.OpenOrCreate, FileAccess.Write);
        using MatBinaryWriter bw = new(fs);

        Serialize(bw);

        fs.Close();
    }
    public void Read(string filepath)
    {
        using FileStream fs = new(filepath, FileMode.Open, FileAccess.Read);
        using MatBinaryReader br = new(fs);

        Deserialize(br);

        fs.Close();
    }
    public void Serialize(MatBinaryWriter bw)
    {
        bw.Write(this);
    }
    public void Deserialize(MatBinaryReader br)
    {
        br.MaterialLibrary = this;

        byte[] magic = new byte[4];

        br.ReadBytes(magic);
#if !MATLIB_NOEXCEPT
        if (!magic.SequenceEqual(ExpectedMagic))
            throw new Exception($"Invalid Header: expected \"{ExpectedMagic}\" but got \"{magic}\"");
#endif
        Version = br.ReadUInt32();

        MaterialTemplates = new(new MaterialTemplate[br.ReadUInt32()]);


        // V: This pointer is relative to the end of the pointer table
        br.MicroCodeAddress = br.ReadUInt32() + (Version == 2 ? 20u : 16u) + ((uint)(MaterialTemplates.Count + 1) * sizeof(uint));

        // V: Read MaterialTemplates Pointer Table
        uint[] templatePointers = new uint[MaterialTemplates.Count];
        for (var i = 0; i < MaterialTemplates.Count; i++)
            templatePointers[i] = br.ReadUInt32();



        var size = br.ReadUInt32(); // file size

        br.MaterialStart = br.Tell();
        br.TEMP.Add((br.MaterialStart, "TEMPLATE"));
        // V: I could combine these, but I want this code to be readable
        for (var i = 0; i < MaterialTemplates.Count; i++)
        {
            //var orig = br.Tell();

            MaterialTemplates[i] = new(br);

            //br.Seek(orig);
        }
        List<string> list = [];

        br.TEMP = br.TEMP.OrderBy(x => x.addr).ToList();
        foreach (var value in br.TEMP)
            list.Add($"0x{value.addr:X8} - {value.name}");

        File.WriteAllLines("OUT.txt", list.ToArray());


        uint MaterialTemplateCount = (uint)MaterialTemplates.Count;
        uint MaterialTechniqueCount = 0;
        uint MaterialPassCount = 0;
        uint ShaderPropertyCount = 0;
        uint DefaultsCount = 0;
        uint RenderStatesCount = 0;
        uint UnknownCount = 0;

        uint FlagCount = 0;
        uint UIPropertyCount = 0;

        uint lastCount = 0;
        uint lastAddr = 0;

        uint addrDiff = uint.MaxValue;

        uint unknownStructASizes = 0;

        foreach (var template in MaterialTemplates)
        {
            MaterialTechniqueCount += (uint)template.MaterialTechniques.Count;
            UIPropertyCount += (uint)template.UIProperties.Count;

            foreach (var technique in template.MaterialTechniques)
            {
                MaterialPassCount += (uint)technique.MaterialPasses.Count;
                FlagCount += (uint)technique.Flags.Count;

                foreach (var pass in technique.MaterialPasses)
                {
                    ShaderPropertyCount += (uint)pass.ShaderProperties.Count;
                    ShaderPropertyCount += (uint)pass.GlobalProperties.Count;
                    UIPropertyCount += (uint)pass.UIProperties.Count;
                    RenderStatesCount += (uint)pass.RenderStates.Count;
                    UnknownCount += (uint)pass.UnknownChildren.Count;

                    unknownStructASizes += pass.UnknownStructureA.ItemC;
                    if (pass.UnknownStructureA.ItemA.Count != 0 || pass.UnknownStructureA.ItemA.Address != 0 || pass.UnknownStructureA.ItemB.Address != 0)
                    {
                        Console.WriteLine("aaaa");
                    }
                    if (lastAddr != 0 && lastCount != 0)
                    {
                        var dif = (pass.UnknownPtr - lastAddr) / lastCount;
                        if (dif < addrDiff)
                            addrDiff = dif;
                    }

                    lastCount = (uint)pass.UnknownChildren.Count;
                    lastAddr = pass.UnknownPtr;
                    foreach (var shader in pass.ShaderProperties)
                    {
                        DefaultsCount += (uint)shader.Defaults.Count;
                        UIPropertyCount += (uint)shader.UIProperties.Count;
                    }
                    foreach (var shader in pass.GlobalProperties)
                    {
                        DefaultsCount += (uint)shader.Defaults.Count;
                        UIPropertyCount += (uint)shader.UIProperties.Count;
                    }
                }
            }
        }
        Console.WriteLine($"Size is: {addrDiff:X}");
        uint count = MaterialTemplateCount + MaterialTechniqueCount + MaterialPassCount + ShaderPropertyCount + DefaultsCount + FlagCount + UIPropertyCount + RenderStatesCount;
        Console.WriteLine($"StructA is: {unknownStructASizes:X}");
        uint expected = (Version == 2 ? 20u : 16u) + ((uint)(MaterialTemplates.Count + 1) * sizeof(uint));
        Console.WriteLine($"Mat Start: {expected:X}");
        expected += MaterialTemplateCount * MaterialTemplate.BinarySize; // padding?
        Console.WriteLine($"Tech Start: {expected:X}");
        expected += MaterialTechniqueCount * MaterialTechnique.BinarySize;
        Console.WriteLine($"Pass Start: {expected:X}");
        expected += MaterialPassCount * MaterialPass.BinarySize;
        Console.WriteLine($"ShPr Start: {expected:X} End?: {(expected + ShaderPropertyCount * ShaderProperty.BinarySize):X}");
        expected += ShaderPropertyCount * ShaderProperty.BinarySize;
        Console.WriteLine($"Defs Start: {expected:X}");
        expected += DefaultsCount * sizeof(float);
        Console.WriteLine($"RSts Start: {expected:X}");
        expected += RenderStatesCount * RenderState.BinarySize;
        Console.WriteLine($"Unks Start: {expected:X}");
        expected += UnknownCount * UnknownMaterialChild.BinarySize;
        expected += FlagCount * sizeof(float);
        expected += UIPropertyCount * UIProperty.BinarySize;

        expected += (Version == 2 ? 20u : 16u) + ((uint)(MaterialTemplates.Count + 1) * sizeof(uint)); // header

        Console.WriteLine($"expected is about: {expected.ToString()} / {count.ToString()}");

        br.MaterialLibrary = null;
    }


}
