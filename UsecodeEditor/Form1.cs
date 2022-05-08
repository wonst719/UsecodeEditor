using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration;
using Usecode;

namespace UsecodeEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Dictionary<int, List<string>> LoadTranslation(string filePath)
        {
            using var sr = File.OpenText(filePath);
            using var reader = new CsvReader(sr, new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true
            });

            var dict = new Dictionary<int, List<string>>();

            var header = reader.Read();

            while (reader.Read())
            {
                var func = reader.GetField<string>(0);
                var idx = reader.GetField<int>(1);
                var text = reader.GetField<string>(3);

                var funcId = int.Parse(func, NumberStyles.HexNumber);

                if (!dict.ContainsKey(funcId))
                {
                    dict.Add(funcId, new List<string>());
                }

                Debug.Assert(idx == dict[funcId].Count);

                text = text.ReplaceLineEndings("\r\n");

                dict[funcId].Add(text);
            }

            return dict;
        }

        private void Rebuild(string filePath, List<SerializableFunction> functions)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var func in functions)
                {
                    func.Build(bw);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var fs = new FileStream("OUT_BG.CSV", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                UsecodeConfig.Encoding = Encoding.ASCII;
                UsecodeConfig.ExportStringOnly = true;
                UsecodeConfig.ExportCsv = true;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_BG");
            }

            using (var fs = new FileStream("OUT_BG_K.CSV", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                UsecodeConfig.Encoding = CodePagesEncodingProvider.Instance.GetEncoding(949);
                UsecodeConfig.ExportStringOnly = true;
                UsecodeConfig.ExportCsv = true;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_BG_K");
            }

            using (var fs = new FileStream("OUT_BG.TXT", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                UsecodeConfig.Encoding = Encoding.ASCII;
                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_BG");
                var functions = usecode.ExportFunctions();

                var translation = LoadTranslation("OUT_BG - OUT_BG.csv");

                UsecodeConfig.Encoding = CodePagesEncodingProvider.Instance.GetEncoding(949);

                foreach ((var funcId, var texts) in translation)
                {
                    var func = functions.Find(x => x.Id == funcId);
                    Debug.Assert(func.Messages.Count == texts.Count);
                    for (int i = 0; i < texts.Count; i++)
                    {
                        func.Messages[i].Message = texts[i];
                    }
                }

                Rebuild("USECODE_BG_REBUILD", functions);
            }

            using (var fs = new FileStream("OUT_BG_K_VALIDATE.TXT", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                UsecodeConfig.Encoding = CodePagesEncodingProvider.Instance.GetEncoding(949);
                UsecodeConfig.ExportStringOnly = false;
                UsecodeConfig.ExportCsv = false;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_BG_REBUILD");
                var functions = usecode.ExportFunctions();
                Rebuild("USECODE_BG_REBUILD_VALIDATE", functions);
            }

            using (var fs = new FileStream("OUT_SI.TXT", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                UsecodeConfig.Encoding = Encoding.ASCII;
                UsecodeConfig.ExportStringOnly = false;
                UsecodeConfig.ExportCsv = false;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_SI");
                var functions = usecode.ExportFunctions();
                Rebuild("USECODE_SI_REBUILD", functions);
            }
        }
    }
}
