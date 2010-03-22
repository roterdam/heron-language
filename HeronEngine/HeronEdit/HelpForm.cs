using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HeronEdit
{
    public partial class HelpForm : Form
    {
        public HelpForm()
        {
            InitializeComponent();
        }

        private void HelpForm_Shown(object sender, EventArgs e)
        {

        }

        public void ShowFile(string s)
        {
            webBrowser1.DocumentText = File.ReadAllText(s);
        }

        public static void ShowHelpFile(string s)
        {
            HelpForm form = new HelpForm();
            form.ShowFile(s);
            form.ShowDialog();
        }
    }
}
