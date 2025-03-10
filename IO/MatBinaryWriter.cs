﻿using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Markup;

namespace MaterialLibrarian.IO;

// V: I wish only mercy upon any of you who dare read this code


/// <summary>
/// Defines a chunk segment in the binary file we are writing to
/// </summary>
public class MatChunk(string name = "")
{
    public string Name { get; private set; } = name;
    private uint _address = 0;
    private uint _size = 0;
    private uint _streamPos = 0;

    public bool Grow { get; set; } = false;
    public uint Address
    {
        get => _address;
        set
        {
            _address = value;
            _streamPos = Math.Clamp(_streamPos, _address, End);
        }
    }
    public uint Size
    {
        get => _size;
        set
        {
            _size = value;
            _streamPos = Math.Clamp(_streamPos, _address, End);
        }
    }
    public uint StreamPosition
    {
        get => _streamPos;
        set
        {
            if (value > End || value < Address)
            {
                if (Grow)
                    _size = value - _address;
                else
                    throw new InvalidOperationException($"{Name} : Attempted to write outside chunk boundaries! Needed {(value - _address) - _size} more bytes!");
            }

            _streamPos = Math.Clamp(value, _address, End);
        }
    }

    public uint End => Address + Size;
}

public class MatBinaryWriter(FileStream stream) : IDisposable
{
    public Stream BaseStream { get; private set; } = stream;
    /// <summary>
    /// The Material Library we are currently writing
    /// </summary>
    public MaterialLibrary? MaterialLibrary { get; internal set; } = null;
    /// <summary>
    /// Header Chunk
    /// </summary>
    public MatChunk HeaderChunk { get; set; } = new(nameof(HeaderChunk));
    /// <summary>
    /// TemplateTable Chunk
    /// </summary>
    public MatChunk TemplateTableChunk { get; set; } = new(nameof(TemplateTableChunk));
    /// <summary>
    /// MaterialTemplate Chunk
    /// </summary>
    public MatChunk TemplateChunk { get; set; } = new(nameof(TemplateChunk));
    /// <summary>
    /// MaterialTechnique Chunk
    /// </summary>
    public MatChunk TechniqueChunk { get; set; } = new(nameof(TechniqueChunk));
    /// <summary>
    /// MaterialPass Chunk
    /// </summary>
    public MatChunk PassChunk { get; set; } = new(nameof(PassChunk));
    /// <summary>
    /// ShaderProperties Chunk
    /// </summary>
    public MatChunk ShaderPropertiesChunk { get; set; } = new(nameof(ShaderPropertiesChunk));
    /// <summary>
    /// Defaults Chunk
    /// </summary>
    public MatChunk DefaultsChunk { get; set; } = new(nameof(DefaultsChunk));
    /// <summary>
    /// RenderStates Chunk
    /// </summary>
    public MatChunk RenderStatesChunk { get; set; } = new(nameof(RenderStatesChunk));

    /// <summary>
    /// MaterialPass UnknownChild Chunk
    /// </summary>
    public MatChunk UnknownPassChunk { get; set; } = new(nameof(UnknownPassChunk));
    /// <summary>
    /// UIProperties Chunk
    /// </summary>
    public MatChunk UIPropertiesChunk { get; set; } = new(nameof(UIPropertiesChunk));
    /// <summary>
    /// PropertyValue Chunk
    /// </summary>
    public MatChunk PropertyValueChunk { get; set; } = new(nameof(PropertyValueChunk));
    /// <summary>
    /// String Chunk
    /// </summary>
    public MatChunk StringsChunk { get; set; } = new(nameof(StringsChunk));
    /// <summary>
    /// MicroCode Chunk
    /// </summary>
    public MatChunk MicroCodeChunk { get; set; } = new(nameof(MicroCodeChunk));

    private uint _lastNonChunkPosition = 0;

    private Stack<MatChunk> _activeChunks = new();

    public void PushChunk(MatChunk chunk)
    {
        if (chunk is null)
            throw new ArgumentNullException(nameof(chunk));

        if (_activeChunks.Count == 0)
            _lastNonChunkPosition = (uint)BaseStream.Position;

        _activeChunks.Push(chunk);
        BaseStream.Position = chunk.StreamPosition;
    }
    public void PopChunk()
    {
        if (_activeChunks.Count == 0)
        {
            Debug.WriteLine("Attempted to Pop an extra chunk off the stack!");
            return;
        }

        _activeChunks.Pop();
        
        if (_activeChunks.Count == 0)
            BaseStream.Position = _lastNonChunkPosition;
        else
            BaseStream.Position = ActiveChunk!.StreamPosition;
    }
    public MatChunk? ActiveChunk => _activeChunks.Count == 0 ? null : _activeChunks.Peek();

    public void AllocateChunks()
    {
        Trace.WriteLine("Allocating Chunks");
        // V: I'm aware this is ugly, but I can't be assed to make it better rn.
        uint MaterialTemplateCount = (uint)MaterialLibrary.MaterialTemplates.Count;
        uint MaterialTechniqueCount = 0;
        uint MaterialPassCount = 0;
        uint ShaderPropertyCount = 0;
        uint DefaultsCount = 0;
        uint RenderStatesCount = 0;
        uint UnknownCount = 0;

        uint FlagCount = 0;
        uint UIPropertyCount = 0;

        uint expectedPropertyValueChunkSize = 0;
        uint expectedStringChunkSize = 0;

        foreach (var template in MaterialLibrary.MaterialTemplates)
        {
            MaterialTechniqueCount += (uint)template.MaterialTechniques.Count;
            UIPropertyCount += (uint)template.UIProperties.Count;
            expectedStringChunkSize += (uint)template.Name.Length + 1;

            foreach (var technique in template.MaterialTechniques)
            {
                MaterialPassCount += (uint)technique.MaterialPasses.Count;
                FlagCount += (uint)technique.Flags.Count;
                expectedStringChunkSize += (uint)technique.Name.Length + 1;

                foreach (var pass in technique.MaterialPasses)
                {
                    ShaderPropertyCount += (uint)pass.ShaderProperties.Count;
                    ShaderPropertyCount += (uint)pass.GlobalProperties.Count;
                    UIPropertyCount += (uint)pass.UIProperties.Count;
                    RenderStatesCount += (uint)pass.RenderStates.Count;
                    UnknownCount += (uint)pass.UnknownChildren.Count;
                    expectedStringChunkSize += (uint)pass.Name.Length + 1;
                    
                    foreach (var property in pass.UIProperties)
                    {
                        if (property.Name is not null)
                            expectedStringChunkSize += (uint)(property.Name.Length + 1);
                        
                        expectedPropertyValueChunkSize += property.ValueLength;

                        if (property.PropertyType == UIPropertyType.NORMAL)
                            expectedPropertyValueChunkSize++;
                    }
                    
                    foreach (var shader in pass.ShaderProperties)
                    {
                        DefaultsCount += (uint)shader.Defaults.Count;
                        UIPropertyCount += (uint)shader.UIProperties.Count;
                        expectedStringChunkSize += (uint)shader.PropertyName.Length + 1;

                        if (shader.PropertyType == ShaderPropertyType.SAMPLE)
                            expectedStringChunkSize += (uint)shader.SampleName.Length + 1;

                        foreach (var property in shader.UIProperties)
                        {
                            expectedStringChunkSize += (uint)(property.Name.Length + 1);
                            expectedPropertyValueChunkSize += property.ValueLength;

                            if (property.PropertyType == UIPropertyType.NORMAL)
                                expectedPropertyValueChunkSize++;
                        }
                    }
                    foreach (var shader in pass.GlobalProperties)
                    {
                        DefaultsCount += (uint)shader.Defaults.Count;
                        UIPropertyCount += (uint)shader.UIProperties.Count;
                        expectedStringChunkSize += (uint)shader.PropertyName.Length + 1;

                        if (shader.PropertyType == ShaderPropertyType.SAMPLE)
                            expectedStringChunkSize += (uint)shader.SampleName.Length + 1;

                        foreach (var property in shader.UIProperties)
                        {
                            expectedStringChunkSize += (uint)(property.Name.Length + 1);
                            expectedPropertyValueChunkSize += property.ValueLength;
                            if (property.PropertyType == UIPropertyType.NORMAL)
                                expectedPropertyValueChunkSize++;
                        }
                    }
                }
            }
        }

        Console.WriteLine($"ExpectedString size: {expectedStringChunkSize}");
        HeaderChunk.Size = (MaterialLibrary.Version == 2 ? 20u : 16u);

        TemplateTableChunk.Address = HeaderChunk.End;
        TemplateTableChunk.Size = ((uint)(MaterialLibrary.MaterialTemplates.Count + 1) * sizeof(uint));

        TemplateChunk.Address = TemplateTableChunk.End;
        TemplateChunk.Size = MaterialTemplateCount * MaterialTemplate.BinarySize;

        TechniqueChunk.Address = TemplateChunk.End;
        TechniqueChunk.Size = MaterialTechniqueCount * MaterialTechnique.BinarySize;

        PassChunk.Address = TechniqueChunk.End;
        PassChunk.Size = MaterialPassCount * MaterialPass.BinarySize;

        ShaderPropertiesChunk.Address = PassChunk.End;
        ShaderPropertiesChunk.Size = ShaderPropertyCount * ShaderProperty.BinarySize;

        DefaultsChunk.Address = ShaderPropertiesChunk.End;
        DefaultsChunk.Size = DefaultsCount * sizeof(uint);

        RenderStatesChunk.Address = DefaultsChunk.End;
        RenderStatesChunk.Size = RenderStatesCount * RenderState.BinarySize;

        UnknownPassChunk.Address = RenderStatesChunk.End;
        UnknownPassChunk.Size = UnknownCount * UnknownMaterialChild.BinarySize;

        UIPropertiesChunk.Address = UnknownPassChunk.End;
        UIPropertiesChunk.Size = (UIPropertyCount * UIProperty.BinarySize) + (FlagCount * sizeof(uint));

        PropertyValueChunk.Address = UIPropertiesChunk.End;
        PropertyValueChunk.Size = expectedPropertyValueChunkSize;

        StringsChunk.Address = PropertyValueChunk.End;
        StringsChunk.Size = expectedStringChunkSize;

        Write(new byte[StringsChunk.End]);
        
        MicroCodeChunk.Address = StringsChunk.End;
        MicroCodeChunk.Size = 0;
        MicroCodeChunk.Grow = true;

        MatChunk[] chunks =
        [
            TemplateTableChunk, TemplateChunk, TechniqueChunk, PassChunk, ShaderPropertiesChunk, DefaultsChunk,
            RenderStatesChunk, UnknownPassChunk, UIPropertiesChunk,
            PropertyValueChunk, StringsChunk
        ];
        
        for (var i = 0; i < chunks.Length; i++)
            Console.WriteLine($"{chunks[i].Address:X08} - {chunks[i].Name}");
        Trace.WriteLine($"Expected PropertyValueChunk[{expectedPropertyValueChunkSize}] - StringsChunk[{expectedStringChunkSize}]");
    }



    public uint MicroCodePointerAddress { get; internal set; } = 0;


    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ((IDisposable)BaseStream).Dispose();
    }

    #region General Purpose
    public uint Length => (uint)BaseStream.Length;
    public uint Tell() => ActiveChunk is null ? (uint)BaseStream.Position : ActiveChunk.StreamPosition;
    //public uint TellRelative() => (uint)BaseStream.Position - TemplateChunk.Address;

    public void Seek(uint address, SeekOrigin origin = SeekOrigin.Begin)
    {
        BaseStream.Seek(address, origin);

        if (ActiveChunk is not null)
            ActiveChunk.StreamPosition = (uint)BaseStream.Position;
    }

    public void Write(byte value)
    {
        BaseStream.WriteByte(value);

        if (ActiveChunk is not null)
            ActiveChunk.StreamPosition++;
    }
    public void Write(char value)
    {
        BaseStream.WriteByte((byte)value);

        if (ActiveChunk is not null)
            ActiveChunk.StreamPosition++;
    }

    public void Write(byte[] value)
    {
        BaseStream.Write(value);

        if (ActiveChunk is not null)
            ActiveChunk.StreamPosition += (uint)value.Length;
    }
    public void Write(Span<byte> value)
    {
        BaseStream.Write(value);

        if (ActiveChunk is not null)
            ActiveChunk.StreamPosition += (uint)value.Length;
    }

    public void Write(uint value, bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        buffer = BitConverter.GetBytes(value);

        if (!littleEndian)
            buffer.Reverse();

        Write(buffer);
    }
    public void Write(int value, bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];

        buffer = BitConverter.GetBytes(value);

        if (!littleEndian)
            buffer.Reverse();

        Write(buffer);
    }
    public void Write(float value, bool littleEndian = false)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];

        buffer = BitConverter.GetBytes(value);

        if (!littleEndian)
            buffer.Reverse();

        Write(buffer);
    }

    public void WriteAtAddress(uint address, uint value, bool littleEndian = false)
    {
        var orig = Tell();
        Seek(address);
        Write(value, littleEndian);
        Seek(address);
    }
    public void WriteAtAddress(uint address, int value, bool littleEndian = false)
    {
        var orig = Tell();
        Seek(address);
        Write(value, littleEndian);
        Seek(address);
    }
    public void WriteAtAddress(uint address, float value, bool littleEndian = false)
    {
        var orig = Tell();
        Seek(address);
        Write(value, littleEndian);
        Seek(address);
    }
    /// <summary>
    /// Write a null terminated string to the underlying stream
    /// </summary>
    /// <param name="value"></param>
    public void Write(string value)
    {
        foreach (char c in value)
            Write(c);
        Write('\0');
    }
    #endregion

    /// <summary>
    /// Write to the string chunk, advancing its position by the length of the string + 1, and write the address of the string to the current stream advancing it by 4
    /// </summary>
    /// <param name="value"></param>
    public void WriteReferencedString(string value)
    {
        if (value == "")
        {
            Write(-1);
            return;
        }
        
        PushChunk(StringsChunk);
        var stringAddr = StringsChunk.StreamPosition;
        Write(value);

        PopChunk();
        
        //Console.WriteLine($"{Tell():X08}");
        var offset = stringAddr - TemplateChunk.Address;
        Write(offset);
        // Console.WriteLine($"WROTE STRING{stringAddr:X08}");
    }

    struct CachedMicroCodeWrite(uint vertexShaderPointerAddress, byte[] vertexShader, uint pixelShaderPointerAddress, byte[] pixelShader)
    {
        public uint VertexShaderPointerAddress { get; set; } = vertexShaderPointerAddress;
        public uint PixelShaderPointerAddress { get; set; } = pixelShaderPointerAddress;

        public byte[] VertexShader { get; set; } = vertexShader;
        public byte[] PixelShader { get; set; } = pixelShader;
    }


    public void Write(ItemCountAddress itemCountAddress)
    {
        Write(itemCountAddress.Count);
        Write(itemCountAddress.Address - TemplateChunk.Address);
    }

    #region Write Specific

    private Queue<CachedMicroCodeWrite> _cachedMicroCodeWrites = new();
    public void Write(MaterialLibrary library)
    {
        if (library is null)
            throw new ArgumentNullException(nameof(library));

        MaterialLibrary = library;

        AllocateChunks();
        
        Seek(HeaderChunk.Address);

        for (var i = 0; i < StringsChunk.End / 4; i++)
        {
            Write('P');
            Write('A');
            Write('D');
            Write('!');
            Write('P');
            Write('A');
            Write('D');
            Write('!');
        }
        
        Seek(HeaderChunk.Address);

        Write(MaterialLibrary.ExpectedMagic);
        Write(MaterialLibrary.Version);
        Write((uint)MaterialLibrary.MaterialTemplates.Count);

        //MicroCodePointerAddress = Tell();
        Write(StringsChunk.End);
        
        Seek(TemplateTableChunk.Address);
        // V: Write MaterialTemplates Pointer Table
        // From what I can tell, these will always be stored contigously
        for (var i = 0; i < MaterialLibrary!.MaterialTemplates.Count; i++)
            Write((uint)(MaterialTemplate.BinarySize * i));

        var fileSizeLengthAddress = Tell();
        Write(0x00); // V: temp
        uint matStart = Tell();
        int iterator = 0;

        foreach (var template in MaterialLibrary.MaterialTemplates)
        {
            // Trace.WriteLine($"Writing Template: {iterator++} @ {Tell():X08}");
            Seek(matStart + (uint)(iterator++ * MaterialTemplate.BinarySize));
            Write(template);
        }
        
        Seek(0, SeekOrigin.End);
        uint shaderStart = Tell();
        
        foreach (var cachedWrite in _cachedMicroCodeWrites)
        {
            if (cachedWrite.VertexShader.Length > 0)
            {
                var orig = Tell();
                Seek(cachedWrite.VertexShaderPointerAddress + matStart);
                Write(orig);
                Seek(orig);
                Write(cachedWrite.VertexShader);
            }
            if (cachedWrite.PixelShader.Length > 0)
            {
                var orig = Tell();
                Seek(cachedWrite.PixelShaderPointerAddress + matStart);
                Write(orig);
                Seek(orig);
                Write(cachedWrite.PixelShader);
            }
        }
        Seek(0, SeekOrigin.End);
        WriteAtAddress(fileSizeLengthAddress, Tell() - fileSizeLengthAddress);

        MaterialLibrary = null;
        
        Trace.WriteLine("Finished Writing Material Library");
    }

    private static uint techniqueNum = 0;
    
    private void Write(MaterialTemplate template)
    {
        PushChunk(TemplateChunk);

        var start = Tell();

        Write(0x00);
        WriteReferencedString(template.Name);
        Write(template.Checksum);
        Write(template.Flags);
        Write(template.UnknownMember3_0x0C);
        Write(template.UnknownMember4_0x10);

        Write((uint)template.MaterialTechniques.Count);
        Write(TechniqueChunk.StreamPosition - TemplateChunk.Address);

        foreach (var technique in template.MaterialTechniques)
            Write(technique);

        Write((uint)template.UIProperties.Count);
        Write(UIPropertiesChunk.StreamPosition - TemplateChunk.Address);

        foreach (var  uiProperties in template.UIProperties)
            Write(uiProperties);

        var size = Tell() - start;

        Trace.WriteLineIf(size != MaterialTemplate.BinarySize, $"MaterialTemplate WAS WRITTEN WITH AN INCORRECT SIZE: WROTE {size} BUT BS: {MaterialTemplate.BinarySize}");

        PopChunk();
    }

    private void Write(MaterialTechnique technique)
    {
        PushChunk(TechniqueChunk);
        var start = Tell();

        WriteReferencedString(technique.Name);
        Write(technique.Checksum);

        Write(technique.UnknownMember2_0x04);

        Write((uint)technique.MaterialPasses.Count);
        Write(PassChunk.StreamPosition - TemplateChunk.Address);


        foreach (var pass in technique.MaterialPasses)
            Write(pass);


        Write((uint)technique.Flags.Count);
        Write(UIPropertiesChunk.StreamPosition - TemplateChunk.Address);

        PushChunk(UIPropertiesChunk);
        foreach (var flag in technique.Flags)
            Write(flag);

        PopChunk();

        Write(technique.ConstantB);
        foreach (var value in technique.SkipA)
            Write(value);

        foreach (var child in technique.SubObjects)
            WriteChild(child);

        if (MaterialLibrary!.Version >= 4)
            Write(technique.AboveVersion4Structure);

        var size = Tell() - start;

        Trace.WriteLineIf(size != MaterialTechnique.BinarySize, $"MATERIAL TECHNIQUE WAS WRITTEN WITH AN INCORRECT SIZE: WROTE{size} BUT BS: {MaterialTechnique.BinarySize}");
        PopChunk();

        return;

        void WriteChild(MaterialTechniqueSubObject subObject)
        {
            if (MaterialLibrary!.Version >= 4)
                Write(subObject.Version4Structure);

            uint vertexPointerAddr = 0;
            uint pixelPointerAddr = 0;

            Write((uint)subObject.VertexShaderCode.Length);
            Write(subObject.vCTABConstant);

            PushChunk(MicroCodeChunk);
            Write(subObject.VertexShaderCode);
            PopChunk();

            Write(MicroCodeChunk.StreamPosition - MicroCodeChunk.Address);
            Write(subObject.VertexShaderMemoryOffset);

            Write((uint)subObject.PixelShaderCode.Length);
            Write(subObject.pCTABConstant);

            Write(MicroCodeChunk.StreamPosition - MicroCodeChunk.Address);
            PushChunk(MicroCodeChunk);
            Write(subObject.PixelShaderCode);
            PopChunk();

            Write(subObject.PixelShaderMemoryOffset);

            _cachedMicroCodeWrites.Enqueue(new(vertexPointerAddr, subObject.VertexShaderCode, pixelPointerAddr, subObject.PixelShaderCode));
            
            Write(subObject.A);
            Write(subObject.B);
            Write(subObject.C);

            Write(subObject.Sampler);

            Write(subObject.UnknownA);
            Write(subObject.UnknownB);
        }
    }

    private void Write(MaterialPass pass)
    {
        PushChunk(PassChunk);
        var start = Tell();

        WriteReferencedString(pass.Name);

        Write((uint)pass.RenderStates.Count);
        Write(RenderStatesChunk.StreamPosition - TemplateChunk.Address);

        foreach (var renderState in pass.RenderStates)
            Write(renderState);

        if (MaterialLibrary!.Version < 4)
        {
            Write((uint)pass.UnknownChildren.Count);
            Write(UnknownPassChunk.StreamPosition - TemplateChunk.Address);
            foreach (var child in pass.UnknownChildren)
                Write(child);
        }

        Write(pass.VSPropCount);
        Write(pass.PSPropCount);
        Write(pass.VSPropCountCopy);
        Write(pass.PSPropCountCopy);
        Write(pass.SampleCount);

        Write(pass.OtherA);
        Write(pass.VSConstant);
        Write(pass.PSConstant);
        Write(pass.OtherB);

        Write((uint)pass.ShaderProperties.Count);
        Write(ShaderPropertiesChunk.StreamPosition - TemplateChunk.Address);
        foreach (var property in pass.ShaderProperties)
            Write(property);

        Write((uint)pass.GlobalProperties.Count, true);
        Write(ShaderPropertiesChunk.StreamPosition - TemplateChunk.Address);
        foreach (var property in pass.GlobalProperties)
            Write(property);

        WriteStruct(pass.UnknownStructureA);
        Write(pass.Strange);
        WriteStruct(pass.UnknownStructureB);
        Write((uint)pass.UIProperties.Count);
        Write(UIPropertiesChunk.StreamPosition - TemplateChunk.Address);

        WriteStructC(pass.UnknownStructureC);

        var size = Tell() - start;

        Trace.WriteLineIf(size != MaterialPass.BinarySize, $"MATERIAL PASS WAS WRITTEN WITH AN INCORRECT SIZE: WROTE{size} BUT BS: {MaterialPass.BinarySize}");

        PopChunk();

        return;

        void WriteStruct(UnknownStructureTemplate structure)
        {
            Write(structure.ItemA);
            Write(structure.ItemB);
            Write(structure.ItemC);
        }

        void WriteStructC(UnknownStructureC structure)
        {
            Write(structure.Item00);
            Write(structure.Item01);
            Write(structure.Item02);
            Write(structure.Item03);
            Write(structure.Item04);
            Write(structure.Item05);
            Write(structure.Item06);
            Write(structure.Item07);
            if (MaterialLibrary.Version >= 4)
                return;
            Write(structure.Item08);
            Write(structure.Item09);
        }
    }
    private void Write(RenderState state)
    {
        PushChunk(RenderStatesChunk);

        Write(state.UnknownMember1_0x00);
        Write(state.UnknownMember2_0x04);

        PopChunk();
    }
    private void Write(UnknownMaterialChild passChild)
    {
        PushChunk(UnknownPassChunk);

        var start = Tell();

        // V: Don't know this structure yet :<
        foreach (var value in passChild.Data)
            Write(value);

        var size = Tell() - start;

        Trace.WriteLineIf(size != UnknownMaterialChild.BinarySize, $"MATERIAL UnknownMaterialChild WAS WRITTEN WITH AN INCORRECT SIZE: WROTE {size} BUT BS: {UnknownMaterialChild.BinarySize}");

        PopChunk();
    }

    private void Write(ShaderProperty property)
    {
        PushChunk(ShaderPropertiesChunk);

        uint start = Tell();

        WriteReferencedString(property.PropertyName);

        Write(property.UsuallyNull);

        if (property.PropertyType == ShaderPropertyType.SAMPLE)
            WriteReferencedString(property.SampleName);
        else
            Write(-1);

        Write(property.Checksum);

        Write((byte)property.PropertyType);
        Write((byte)property.UVIndex);
        Write((byte)property.UnknownByte1);
        Write((byte)property.UnknownByte2);

        Write(property.UnknownIndexUInt1);
        Write(property.UnknownIndexUInt2);
        Write(property.SkinMatIndex);
        Write(property.UnknownIndexUInt3);

        Write((uint)property.Defaults.Count);
        Write(DefaultsChunk.StreamPosition - TemplateChunk.Address);

        PushChunk(DefaultsChunk);
        foreach (var def in property.Defaults)
            Write(def, true);
        PopChunk();


        Write((uint)property.UIProperties.Count);
        Write(UIPropertiesChunk.StreamPosition - TemplateChunk.Address);

        foreach (var prop in property.UIProperties)
            Write(prop);

        uint end = Tell();

        Trace.WriteLineIf(end - start != ShaderProperty.BinarySize, $"ShaderProperty: Wrote {end - start} but length is {ShaderProperty.BinarySize}");

        PopChunk();
    }

    private void Write(UIProperty property)
    {
        PushChunk(UIPropertiesChunk);

        uint start = Tell();

        WriteReferencedString(property.Name);
        Write(property.Checksum);
        Write((uint)property.PropertyType);
        Write(property.ValueLength);
        Write(PropertyValueChunk.StreamPosition - TemplateChunk.Address);

        uint end = Tell();

        Trace.WriteLineIf(end - start != UIProperty.BinarySize, $"UIProperty: Wrote {end - start} but length is {UIProperty.BinarySize}");

        PushChunk(PropertyValueChunk);
        Write(property.Value);
        PopChunk();


        PopChunk();
    }

    #endregion

}
