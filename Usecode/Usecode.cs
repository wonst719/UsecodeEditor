using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Usecode
{
    public class Usecode
    {
        private readonly StreamWriter _outWriter;

        List<Function> _functions = new List<Function>();

        public Usecode(StreamWriter bw)
        {
            _outWriter = bw;
        }

        private void ReadFunctions(BinaryReader br)
        {
            var func = new Function(_outWriter);
            func.Load(br);
            func.Disassemble();
            _functions.Add(func);
        }

        public void Load(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            using (var ms = new MemoryStream(bytes, false))
            using (var br = new BinaryReader(ms))
            {
                // HasSymbolTable
                if (CheckSig(br, stackalloc byte[8] { 0xff, 0xff, 0xff, 0xff, 0x59, 0x53, 0x43, 0x55 }))
                {
                    throw new Exception("No support for symbol table");
                }

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    ReadFunctions(br);
                }
            }
        }

        private bool CheckSig(BinaryReader br, ReadOnlySpan<byte> signature)
        {
            bool matches = false;
            long origPos = br.BaseStream.Position;
            if (origPos + signature.Length > br.BaseStream.Length)
                return false;

            ReadOnlySpan<byte> sig = br.ReadBytes(signature.Length);
            if (sig.SequenceEqual(signature))
            {
                matches = true;
            }

            br.BaseStream.Position = origPos;
            return matches;
        }

        public List<SerializableFunction> ExportFunctions()
        {
            return _functions.Select(x => x.ToSerializable()).ToList();
        }
    }
}
