using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Usecode
{
    public class Function
    {
        public long Pos;
        public ushort Id;
        public ushort Size;

        public byte[] Data;

        private long _codePos;

        public ushort Argc;
        public ushort Localc;
        public List<ushort> Externs = new List<ushort>();

        Dictionary<long, Message> Messages = new Dictionary<long, Message>();
        private readonly StreamWriter _outWriter;

        Dictionary<int, long> PatchLocMap = new Dictionary<int, long>();

        public Function(StreamWriter outWriter)
        {
            _outWriter = outWriter;
        }

        public void Load(BinaryReader br)
        {
            Pos = br.BaseStream.Position;
            Id = br.ReadUInt16();
            Size = br.ReadUInt16();
            Data = new byte[Size];
            br.Read(Data);
            if (!UsecodeConfig.ExportStringOnly && !UsecodeConfig.ExportCsv)
            {
                _outWriter?.WriteLine($"__FUNC__ Pos: {Pos}, Id: {Id:X}, Size: {Size}");
            }
        }

        public void Disassemble()
        {
            Span<byte> ptr = Data;
            int locLen = BitConverter.ToUInt16(ptr);
            ptr = ptr.Slice(2, locLen);

            int idx = 0;
            int pos = 0;
            while (ptr.Length > 0)
            {
                int splitterPos = ptr.IndexOf((byte)0);
                if (UsecodeConfig.ExportCsv)
                {
                    _outWriter?.WriteLine($"{Id:X4},{idx:D3},{pos:X4},\"{UsecodeConfig.Encoding.GetString(ptr.Slice(0, splitterPos)).Replace("\"", "\"\"")}\"");
                }
                else
                {
                    _outWriter?.WriteLine($"Str#{idx:D3}@[{Id:X4}:{pos:X4}]: {UsecodeConfig.Encoding.GetString(ptr.Slice(0, splitterPos))}");
                }
                Messages.Add(pos, new Message { Idx = idx++, Pos = pos, Data = ptr.Slice(0, splitterPos).ToArray() });
                ptr = ptr.Slice(splitterPos + 1);
                pos += splitterPos + 1;
            }

            using (var ms = new MemoryStream(Data, false))
            using (var br = new BinaryReader(ms))
            {
                ms.Position = 2 + locLen;
                DisassembleFunction(br);
            }
        }

        public void DisassembleFunction(BinaryReader br)
        {
            Argc = br.ReadUInt16();
            Localc = br.ReadUInt16();
            if (!UsecodeConfig.ExportStringOnly)
            {
                _outWriter?.WriteLine($"Argc: {Argc}, Localc: {Localc}");
            }

            int externSize = br.ReadUInt16();
            for (int i = 0; i < externSize; i++)
            {
                Externs.Add(br.ReadUInt16());

                if (!UsecodeConfig.ExportStringOnly)
                {
                    _outWriter?.WriteLine($"Extern[{i}]: {Externs[Externs.Count - 1]:X}");
                }
            }

            _codePos = br.BaseStream.Position;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                DisassembleOp(br);
            }
        }

        public void DisassembleOp(BinaryReader br)
        {
            byte opcode = br.ReadByte();
            var usecodeOp = (UsecodeOps)opcode;
            switch (usecodeOp)
            {
                case UsecodeOps.UC_ADDSI:
                case UsecodeOps.UC_PUSHS:
                    long loc = br.BaseStream.Position - _codePos;

                    if (!UsecodeConfig.ExportStringOnly)
                    {
                        _outWriter?.Write($"{br.BaseStream.Position - _codePos - 1:X} {opcode:X} {usecodeOp}");
                    }
                    var pos = br.ReadUInt16();
                    if (Messages.TryGetValue(pos, out var msg))
                    {
                        if (PatchLocMap.ContainsKey(msg.Idx))
                        {
                            if (!UsecodeConfig.ExportCsv)
                            {
                                _outWriter?.WriteLine($" ERROR Dup {pos:X} \"{UsecodeConfig.Encoding.GetString(msg.Data)}\"");
                            }
                        }
                        else
                        {
                            PatchLocMap.Add(msg.Idx, loc);

                            if (!UsecodeConfig.ExportStringOnly)
                            {
                                _outWriter?.WriteLine($" STR {pos:X} ;\"{UsecodeConfig.Encoding.GetString(msg.Data)}\"");
                            }
                        }
                    }
                    else
                    {
                        if (!UsecodeConfig.ExportCsv)
                        {
                            _outWriter?.WriteLine($" ERROR {pos:X}");
                        }
                        //throw new Exception();
                    }
                    break;
                default:
                    if (!UsecodeConfig.ExportStringOnly)
                    {
                        _outWriter?.Write($"{br.BaseStream.Position - _codePos - 1:X} {opcode:X} {usecodeOp}");
                    }
                    for (int i = 0; i < OpsBytes.ops[usecodeOp]; i++)
                    {
                        var b = br.ReadByte();
                        if (!UsecodeConfig.ExportStringOnly)
                        {
                            _outWriter?.Write($" {b:X}");
                        }
                    }
                    if (!UsecodeConfig.ExportStringOnly)
                    {
                        _outWriter?.WriteLine("");
                    }
                    break;
            }
        }

        public SerializableFunction ToSerializable()
        {
            var sf = new SerializableFunction();

            sf.Id = Id;
            sf.Messages = Messages.Select(x => x.Value)
                .OrderBy(x => x.Idx)
                .Select(x => new SerializableMessage { Message = x.ToString() })
                .ToList();

            sf.Argc = Argc;
            sf.Localc = Localc;

            int externLen = Externs.Count * sizeof(ushort);
            byte[] x = ArrayPool<byte>.Shared.Rent(Externs.Count * sizeof(ushort));
            Span<byte> span = x.AsSpan().Slice(0, externLen);
            for (int i = 0; i < Externs.Count; i++)
            {
                BitConverter.TryWriteBytes(span.Slice(i * sizeof(ushort)), Externs[i]);
            }

            sf.ExternSeg = span.ToArray();

            sf.CodeSeg = Data.AsSpan().Slice((int)_codePos).ToArray();
            sf.PatchLocList = PatchLocMap.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            return sf;
        }

        public void BuildFunction(BinaryWriter bw)
        {
            var sf = ToSerializable();
            sf.Build(bw);
        }
    }
}
