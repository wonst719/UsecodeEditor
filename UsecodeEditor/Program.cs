using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using Usecode;

namespace UsecodeEditor
{
    class Program
    {
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

        private void Test()
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

        private class Options
        {
            [Option('i', Required = false, HelpText = "Import (Default)")]
            public bool Import { get; set; }

            [Option('x', Required = false, HelpText = "Export CSV")]
            public bool Export { get; set; }

            [Option('u', Required = true, HelpText = "Original USECODE file path")]
            public string UsecodePath { get; set; }

            [Option('t', Required = false, HelpText = "Translation CSV file path")]
            public string TranslationFilePath { get; set; }

            [Option('o', Required = true, HelpText = "Output file path")]
            public string OutputPath { get; set; }

            [Option("original-encoding", Required = false, HelpText = "Original Encoding Codepage (Default: 437)")]
            public int UsecodeEncoding { get; set; } = 437;

            [Option("output-encoding", Required = false, HelpText = "Output Encoding Codepage (Default: 949)")]
            public int OutputEncoding { get; set; } = 949;

            [Option("dump-path", Required = false, HelpText = "Dump path")]
            public string DumpPath { get; set; } = "DUMP.TXT";
        }

        private void RunOptions(Options opts)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (opts.Export)
            {
                using var fs = new FileStream(opts.OutputPath, FileMode.Create, FileAccess.Write);
                using var bw = new StreamWriter(fs);

                UsecodeConfig.Encoding = Encoding.GetEncoding(opts.UsecodeEncoding);
                UsecodeConfig.ExportStringOnly = true;
                UsecodeConfig.ExportCsv = true;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load(opts.UsecodePath);
            }
            else
            {
                using var fs = new FileStream(opts.DumpPath, FileMode.Create, FileAccess.Write);
                using var bw = new StreamWriter(fs);

                UsecodeConfig.Encoding = Encoding.GetEncoding(opts.UsecodeEncoding);
                UsecodeConfig.ExportStringOnly = false;
                UsecodeConfig.ExportCsv = false;

                var usecode = new Usecode.Usecode(bw);
                usecode.Load(opts.UsecodePath);
                var functions = usecode.ExportFunctions();

                var translation = LoadTranslation(opts.TranslationFilePath);

                UsecodeConfig.Encoding = Encoding.GetEncoding(opts.OutputEncoding);

                foreach ((var funcId, var texts) in translation)
                {
                    var func = functions.Find(x => x.Id == funcId);
                    Debug.Assert(func.Messages.Count == texts.Count);
                    for (int i = 0; i < texts.Count; i++)
                    {
                        func.Messages[i].Message = texts[i];
                    }
                }

                Rebuild(opts.OutputPath, functions);

            }
        }

        static void Main(string[] args)
        {
            var program = new Program();
            Parser.Default.ParseArguments<Options>(args).WithParsed(program.RunOptions);
            // program.Test();
        }
    }
}
