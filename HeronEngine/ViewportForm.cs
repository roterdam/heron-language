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

namespace ViewportLib
{
    public partial class ViewportForm : Form
    {
        List<ViewportCmd> mCmds = new List<ViewportCmd>();
        Mutex mMutex = new Mutex();
        public Graphics g;
        public Pen pen = new Pen(Color.Black);
        Bitmap bmp = new Bitmap(1000, 1000);
        Viewport vp;

        public ViewportForm()
        {
            InitializeComponent();
        }

        internal void SetViewport(Viewport vp)
        {
            this.vp = vp;
        }

        public void ClearCmds()
        {
            mMutex.WaitOne();
            mCmds.Clear();
            mMutex.ReleaseMutex();

            // Tell the parent thread to invalidate
            MethodInvoker p = Invalidate;
            Invoke(p);
        }

        public void AddCmd(ViewportCmd c)
        {
            mMutex.WaitOne();
            try
            {
                mCmds.Add(c);
                pictureBox1.Invalidate();
            }
            finally
            {
                mMutex.ReleaseMutex();
            }
        }

        public void SaveToFile(string s)
        {
            mMutex.WaitOne();

            try
            {
                Bitmap b = new Bitmap(Width, Height, CreateGraphics());
                foreach (ViewportCmd c in mCmds) {
                    c(this);
                }
                b.Save(s);
            }
            finally
            {
                mMutex.ReleaseMutex();
            }
        }

        public delegate void Proc();

        public void SafeClose()
        {
            if (InvokeRequired)
            {
                Proc p = SafeClose;
                Invoke(p, null);
            }
            else
            {
                Close();
            }
        }

        public void SafeShow()
        {
            if (InvokeRequired)
            {
                Proc p = SafeShow;
                Invoke(p, null);
            }
            else
            {
                Show();
            }
        }
        
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            mMutex.WaitOne();
            try
            {
                g = Graphics.FromImage(bmp);
                g.Clear(Color.White);
                foreach (ViewportCmd cmd in mCmds)
                    cmd(this);

                // TODO: replace this call with a bit block transfer API call
                e.Graphics.DrawImageUnscaled(bmp, 0, 0);
            }
            finally
            {
                mMutex.ReleaseMutex();
                g = null;
            }        
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            bmp = new Bitmap(Width, Height, CreateGraphics());
            Refresh();
        }

        private void ViewportForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            vp.NullifyWindow();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void ViewportForm_Paint(object sender, PaintEventArgs e)
        {

        }

    }
}
