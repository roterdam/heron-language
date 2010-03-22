using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace HeronEdit
{
    public partial class HeronMainForm : Form
    {
        private HeronToRtf rtfHelper = new HeronToRtf();

        public HeronMainForm()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private void cheatSheetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpForm.ShowHelpFile("quick-reference.html");
        }

        private void HeronMainForm_Shown(object sender, EventArgs e)
        {
            string s = File.ReadAllText(@"samples\NQueens.heron");
            string rtf = rtfHelper.ToRtf(s);
            codeControl.Rtf = rtf;
        }
    }
}
