using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HeronEdit
{
    public partial class TextEntryForm : Form
    {
        public TextEntryForm()
        {
            InitializeComponent();
        }

        public bool ShowDialog(string title)
        {
            Text = title;
            return base.ShowDialog() == DialogResult.OK;
        }

        public string EnteredText
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }
    }
}
