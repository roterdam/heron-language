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
    public class Util
    {
        static Random r = new Random();

        public static double Random(double x)
        {
            return r.NextDouble() * x;
        }

        public static int Random(int x)
        {
            return r.Next(x);
        }

        public static void Sleep(int msec)
        {
            Thread.Sleep(msec);
        }
    }
    public class Viewport
    {
        private ViewportForm form = null;
        private Pen pen = new Pen(Color.Black);
        private static AutoResetEvent mWait = new AutoResetEvent(false);
        private int width;
        private int height;

        public Viewport(int w, int h)
        {
            width = w;
            height = h;
            Thread t = new Thread(new ParameterizedThreadStart(LaunchWindow));
            t.Start(this);
            mWait.WaitOne();
        }

        #region public methods

        public void Clear()
        {
            form.ApplyToBitmap((Graphics g) => {
                g.Clear(Color.White);
            });
        }

        public void Line(float x1, float y1, float x2, float y2)
        {
            form.ApplyToBitmap((Graphics g) => {
                g.DrawLine(pen, x1, y1, x2, y2);
            });
        }

        public void Ellipse(float x, float y, float w, float h)
        {
            form.ApplyToBitmap((Graphics g) => {
                g.DrawEllipse(pen, x, y, w, h);
            });
        }

        public void SetPenColor(Color x)
        {
            pen.Color = x;
        }

        public Color GetPenColor()
        {
            return pen.Color;
        }

        public void SetPenWidth(float x)
        {
            pen.Width = x;
        }

        public float GetPenWidth()
        {
            return pen.Width;
        }
        #endregion

        #region window functions
        public void CloseWindow()
        {
            form.Close();
        }

        public bool IsOpen()
        {
            return form.Visible;
        }
        #endregion

        #region private functions
        private static void LaunchWindow(Object o)
        {
            Viewport vp = o as Viewport;
            vp.form = new ViewportForm(vp.width, vp.height);
            vp.form.Width = vp.width;
            vp.form.Height = vp.height;
            vp.form.Show();
            mWait.Set();

            // Start GUI event loop
            Application.Run(vp.form);
        }
        #endregion
    }

}
