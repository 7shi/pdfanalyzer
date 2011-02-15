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
                OpenPDF(openFileDialog1.FileName);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OpenPDF(string pdf)
        {
            toolStripStatusLabel1.Text = Path.GetFileNameWithoutExtension(pdf);
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = true;
            menuStrip1.Enabled = false;
            analyzerPanel1.OpenPDF(pdf);
        }

        private void analyzerPanel1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void analyzerPanel1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Visible = false;
            menuStrip1.Enabled = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (analyzerPanel1.IsBusy)
            {
                MessageBox.Show(
                    this, "処理中のため閉じることができません。", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                e.Cancel = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            analyzerPanel1.ClosePDF();
        }
    }
}
