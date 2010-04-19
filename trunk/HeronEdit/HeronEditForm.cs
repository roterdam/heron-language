using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        TextEntryForm macroForm = new TextEntryForm();

        public HeronMainForm()
        {
            InitializeComponent();
            editor = new HeronEditor(codeControl, output, menuStrip1);
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                editor.Open(args[1]);
            }

            codeControl.AllowDrop = true;
            codeControl.DragDrop += new DragEventHandler(HeronMainForm_DragDrop);
            codeControl.DragEnter += new DragEventHandler(HeronMainForm_DragEnter);

            output.AllowDrop = true;
            output.DragDrop += new DragEventHandler(HeronMainForm_DragDrop);
            output.DragEnter += new DragEventHandler(HeronMainForm_DragEnter);
        }

        private void HeronMainForm_Shown(object sender, EventArgs e)
        {
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Open();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Undo();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Save();
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
            editor.Run();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.New();
        }

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            editor.LaunchNewEditor(editor.GetMacroFile());
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 ab = new AboutBox1();
            ab.ShowDialog();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Delete();
        }

        private void runMacroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!macroForm.ShowDialog("Enter macro name"))
                return;
            editor.RunMacro(macroForm.EnteredText);
        }

        private void HeronMainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length == 0)
                return;

            if (files.Length > 1)
            {
                MessageBox.Show("You can only drag and drop one file at a time");
                return;
            }

            if (!editor.SaveIfModified())
                return;

            editor.Open(files[0]);
        }

        private void HeronMainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All; else
                e.Effect = DragDropEffects.None;
        }
    }
}
