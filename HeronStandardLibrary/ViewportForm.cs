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
    public delegate void Procedure();

    public partial class ViewportForm : Form
    {
        public Bitmap bmp;
        public Graphics g;
        private Viewport vp;

        public ViewportForm(int w, int h, Viewport vp)
        {
            InitializeComponent();
            bmp = new Bitmap(w, h);
            g = Graphics.FromImage(bmp);
            this.vp = vp;
            Width = w;
            Height = h;
        }

        public void ApplyToBitmap(ViewportCmd cmd)
        {
            if (InvokeRequired)
            {
                Invoke(cmd, g);
            }
            else
            {
                cmd(g);
            }
            SafeInvalidate();
        }

        public void SafeInvalidate()
        {
            if (InvokeRequired)
            {
                Procedure p = SafeInvalidate;
                Invoke(p);
            }
            else
            {
                pictureBox1.Invalidate();
            }
        }


        private void pictureBox1_Paint_1(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(bmp, 0, 0);
        }

        private void ViewportForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            vp.ReleaseForm();
        }
    }
}
