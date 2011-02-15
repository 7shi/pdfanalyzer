using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using PdfLib;

namespace PdfAnalyzer
{
    public partial class AnalyzerPanel : UserControl
    {
        public AnalyzerPanel()
        {
            InitializeComponent();
        }

        private PdfDocument doc;

        public void OpenPDF(string pdf)
        {
            listView1.Items.Clear();
            textBox1.Clear();
            ClosePDF();

            listView1.Enabled = textBox1.Enabled = false;
            backgroundWorker1.RunWorkerAsync(pdf);
        }

        public void ClosePDF()
        {
            if (doc != null) doc.Dispose();
            doc = null;
        }

        private int progress;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            progress = 0;
            doc = new PdfDocument(
                e.Argument as string,
                p => backgroundWorker1.ReportProgress(p));
        }

        public event ProgressChangedEventHandler ProgressChanged;

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress = e.ProgressPercentage;
            if (ProgressChanged != null) ProgressChanged(sender, e);
        }

        public event RunWorkerCompletedEventHandler RunWorkerCompleted;

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listView1.BeginUpdate();
            var keys = new List<int>(doc.Keys);
            keys.Sort();
            var prg = progress == 0 && ProgressChanged != null;
            int p = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                if (prg)
                {
                    int pp = i * 100 / keys.Count;
                    if (p != pp)
                        ProgressChanged(sender, new ProgressChangedEventArgs(p = pp, null));
                }
                var k = keys[i];
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
            listView1.Enabled = textBox1.Enabled = true;

            if (RunWorkerCompleted != null) RunWorkerCompleted(sender, e);
        }

        public bool IsBusy
        {
            get
            {
                return backgroundWorker1.IsBusy;
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var li = listView1.FocusedItem;
            if (li == null || !(li.Tag is int))
                textBox1.Clear();
            else
                textBox1.Text = doc.ReadObject((int)li.Tag);
        }
    }
}
