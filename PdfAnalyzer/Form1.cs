using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PdfAnalyzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) != DialogResult.OK) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            ReadPDF(openFileDialog1.FileName);
            Cursor.Current = cur;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ReadPDF(string pdf)
        {
            toolStripStatusLabel1.Text = pdf;
            listView1.Items.Clear();
            PdfParser parser;
            using (var fs = new FileStream(pdf, FileMode.Open))
            {
                parser = new PdfParser(fs);
                parser.ReadXref();
            }

            listView1.BeginUpdate();
            var keys = new List<int>(parser.Xref.Keys);
            keys.Sort();
            var tr = parser.GetTrailerReferences();
            foreach (var k in keys)
            {
                var obj = parser.Xref[k];
                var pos = obj.Position;
                var details = tr.ContainsKey(k) ? tr[k] : "";
                var cells = new string[7];
                bool sub = obj.ObjStm != 0;
                listView1.Items.Add(new ListViewItem(new[]
                {
                    "",
                    k.ToString(),
                    sub ? "" : pos.ToString(),
                    sub ? "" : pos.ToString("x"),
                    !sub ? "" : obj.ObjStm.ToString(),
                    !sub ? "" : obj.Index.ToString(),
                    details
                }));
            }
            listView1.EndUpdate();
        }
    }
}
