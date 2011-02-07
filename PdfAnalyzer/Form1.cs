using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PdfLib;

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

        private PdfDocument doc;

        private void ReadPDF(string pdf)
        {
            toolStripStatusLabel1.Text = pdf;
            listView1.Items.Clear();
            doc = new PdfDocument(pdf);

            listView1.BeginUpdate();
            var keys = new List<int>(doc.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                var obj = doc.GetObject(k);
                var pos = obj.Position;
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
                    obj.Details
                }) { Tag = k });
            }
            listView1.EndUpdate();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var li = listView1.FocusedItem;
            if (li == null || !(li.Tag is int))
                textBox1.Clear();
            else
                textBox1.Text = doc.ReadObject((int)li.Tag);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (doc != null) doc.Dispose();
        }
    }
}
