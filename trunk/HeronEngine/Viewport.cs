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
    public delegate void ViewportCmd(ViewportForm vf);

    /// <summary>
    /// Wraps a form with a canvas, that is used for drawing.
    /// What is special is that the form is on a separate thread, allowing drawing commands 
    /// to be invoked asynchronously, but still allowing the programmers to interact with 
    /// the user via other mechanisms (e.g. the command-line).
    /// </summary>
    public class Viewport
    {
        private ViewportForm form = null;
        private static Mutex mMutex = new Mutex();
        private static AutoResetEvent mWait = new AutoResetEvent(false);
        private static int width;
        private static int height;

        public Viewport(int w, int h)        
        {
            width = w;
            height = h;
            Thread t = new Thread(new ParameterizedThreadStart(LaunchWindow));
            t.Start(this);
            mWait.WaitOne();
        }

        #region public methods
        public void Line(int x1, int y1, int x2, int y2)
        {
            form.AddCmd(delegate(ViewportForm vf)
            {
                vf.g.DrawLine(vf.pen, x1, y1, x2, y2);
            });
        }

        public void Ellipse(int x, int y, int w, int h)
        {
            form.AddCmd(delegate(ViewportForm vf)
            {
                vf.g.DrawEllipse(vf.pen, x, y, w, h);
            });
        }

        public void SetPenColor(Color x)
        {
            form.AddCmd(delegate(ViewportForm vf)
            {
                vf.pen.Color = x;
            });
        }

        public void SetPenWidth(int x)
        {
            form.AddCmd(delegate(ViewportForm vf)
            {
                vf.pen.Width = x;
            });
        }

        public void SaveToFile(string s)
        {
            mMutex.WaitOne();
            form.SaveToFile(s);
            mMutex.ReleaseMutex();
        }
        #endregion

        #region window functions
        public void Sleep(int msec)
        {
            Thread.Sleep(msec);
        }

        public void CloseWindow()
        {
            mMutex.WaitOne();
            if (form == null)
            {
                mMutex.ReleaseMutex();
                return;
            }
            mMutex.ReleaseMutex();
            form.SafeClose();
        }

        public void ClearWindow()
        {
            form.ClearCmds();
        }

        public bool IsOpen()
        {
            mMutex.WaitOne();
            bool r = form != null;
            mMutex.ReleaseMutex();
            return r;
        }
        #endregion

        #region non-published functions
        internal void NullifyWindow()
        {
            mMutex.WaitOne();
            form = null;
            mMutex.ReleaseMutex();
        }

        /// <summary>
        /// Unsafe access to form. Only used once.
        /// </summary>
        /// <returns></returns>
        internal Form GetForm()
        {
            return form;
        }
        #endregion

        #region private functions
        private static void LaunchWindow(Object o)
        {
            Viewport vp = o as Viewport;
            ViewportForm form = new ViewportForm();
            form.Width = width;
            form.Height = height;
            form.SetViewport(vp);
            vp.form = form;
            form.Show();
            mWait.Set();

            // Start GUI event loop
            Application.Run(form);
        }
        #endregion 
            
    }
}
