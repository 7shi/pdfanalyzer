using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                ReadPDF(openFileDialog1.FileName);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private PdfDocument doc;

        private void ReadPDF(string pdf)
        {
            toolStripStatusLabel1.Text = Path.GetFileNameWithoutExtension(pdf);
            listView1.Items.Clear();
            textBox1.Clear();
            if (doc != null) doc.Dispose();

            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = true;
            menuStrip1.Enabled = listView1.Enabled = textBox1.Enabled = false;
            backgroundWorker1.RunWorkerAsync(pdf);
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

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            doc = new PdfDocument(
                e.Argument as string,
                p => backgroundWorker1.ReportProgress(p));
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listView1.BeginUpdate();
            var keys = new List<int>(doc.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                //var obj = doc.GetObject(k);
                var obj = doc[k];
                var pos = obj.Position;
                var cells = new string[7];
                bool sub = obj.ObjStm != 0;
                listView1.Items.Add(new ListViewItem(new[]
                {
                    obj.Details,
                    k.ToString(),
                    sub ? "" : pos.ToString(),
                    sub ? "" : pos.ToString("x"),
                    !sub ? "" : obj.ObjStm.ToString(),
                    !sub ? "" : obj.Index.ToString()
                }) { Tag = k });
            }
            listView1.EndUpdate();

            toolStripProgressBar1.Visible = false;
            menuStrip1.Enabled = listView1.Enabled = textBox1.Enabled = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (backgroundWorker1.IsBusy)
            {
                MessageBox.Show(
                    this, "処理中のため閉じることができません。", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                e.Cancel = true;
            }
        }
    }
}
