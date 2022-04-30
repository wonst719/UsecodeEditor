using System;
using System.IO;
using System.Windows.Forms;

namespace UsecodeEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var fs = new FileStream("OUT_BG.TXT", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_BG");
                usecode.TestBuild("USECODE_BG_REBUILD");
                var functions = usecode.ExportFunctions();

                // ApplyTranslations();

                // build
            }

            using (var fs = new FileStream("OUT_SI.TXT", FileMode.Create, FileAccess.Write))
            using (var bw = new StreamWriter(fs))
            {
                var usecode = new Usecode.Usecode(bw);
                usecode.Load("USECODE_SI");
                usecode.TestBuild("USECODE_SI_REBUILD");
                var functions = usecode.ExportFunctions();
            }
        }
    }
}
