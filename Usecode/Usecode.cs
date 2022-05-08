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
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    ReadFunctions(br);
                }
            }
        }

        public List<SerializableFunction> ExportFunctions()
        {
            return _functions.Select(x => x.ToSerializable()).ToList();
        }
    }
}
