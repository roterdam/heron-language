using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using HeronEngine;

namespace HeronEdit
{
    public partial class HeronMainForm : Form
    {
        HeronEditor editor;

        public HeronMainForm()
        {
            InitializeComponent();
            editor = new HeronEditor(codeControl, output);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void HeronMainForm_Shown(object sender, EventArgs e)
        {
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Load(@"C:\dev\Heron\HeronEngine\samples\NQueens.heron");
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Undo();
        }

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Save();
            saveToolStripMenuItem.Enabled = editor.CanSave();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.SaveAs();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Cut();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Redo();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.SelectAll();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
