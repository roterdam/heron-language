using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace HeronStandardLibrary
{
    public delegate void ViewportCmd(Graphics g);

    public partial class ViewportForm : Form
    {
        public Bitmap bmp;
        public Graphics g;

        public ViewportForm(int w, int h)
        {
            InitializeComponent();
            bmp = new Bitmap(w, h);
            g = Graphics.FromImage(bmp);
            Width = w;
            Height = h;
        }

        public void ApplyToBitmap(ViewportCmd cmd)
        {
            if (InvokeRequired)
            {
                Delegate d = Delegate.CreateDelegate(GetType(), cmd, GetType().GetMethod("ApplyToBitmap"));
                Invoke(cmd);
            }
            else
            {
                cmd(g);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(bmp, 0, 0);
        }

        private void ViewportForm_Paint(object sender, PaintEventArgs e)
        {
            // ??
        }
    }
}
