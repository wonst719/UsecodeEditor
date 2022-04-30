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

        public void TestBuild(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var func in _functions)
                {
#if false
                    var serializableFunction = func.ToSerializable();

                    void SerializeJson()
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(serializableFunction);
                        File.WriteAllText($"OUT_{serializableFunction.Id}.TXT", json);
                    }
                    SerializableFunction DeserializeJson()
                    {
                        var json = File.ReadAllText($"OUT_{serializableFunction.Id}.TXT");
                        var obj = System.Text.Json.JsonSerializer.Deserialize<SerializableFunction>(json);
                        return obj;
                    }
                    SerializeJson();
                    var f = DeserializeJson();
                    f.Build(bw);
#else
                    func.BuildFunction(bw);
#endif
                }
            }
        }
    }
}
