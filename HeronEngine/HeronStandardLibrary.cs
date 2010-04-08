/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

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

/// In theory this namespace is used for holding the standard library
/// It is not used much and will probably become deprecated.
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

    public delegate void ViewportCmd(ViewportForm vf);

    /// <summary>
    /// Wraps a form with a canvas, that is used for drawing.
    /// What is special is that the form is on a separate thread, allowing drawing commands 
    /// to be invoked asynchronously, but still allowing the programmers to interact with 
    /// the user via other mechanisms (e.ci. the command-line).
    /// </summary>
    public class Viewport
    {
        public static Mutex mutex = new Mutex();

        private ViewportForm form = null;
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
        public void Line(double x1, double y1, double x2, double y2)
        {
            AddCmd(delegate(ViewportForm vf)
            {
                vf.g.DrawLine(vf.pen, (float)x1, (float)y1, (float)x2, (float)y2);
            });
        }

        public void Ellipse(double x, double y, double w, double h)
        {
            AddCmd(delegate(ViewportForm vf)
            {
                vf.g.DrawEllipse(vf.pen, (float)x, (float)y, (float)w, (float)h);
            });
        }

        public void SetPenColor(Color x)
        {
            AddCmd(delegate(ViewportForm vf)
            {
                vf.pen.Color = x;
            });
        }

        public void SetPenWidth(int x)
        {
            AddCmd(delegate(ViewportForm vf)
            {
                vf.pen.Width = x;
            });
        }

        public void AddCmd(ViewportCmd c)
        {
            mutex.WaitOne();
            if (form == null)
                return;
            try
            {
                form.AddCmd(c);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public void SaveToFile(string s)
        {
            mutex.WaitOne();
            form.SaveToFile(s);
            mutex.ReleaseMutex();
        }
        #endregion

        #region window exposedFunctions
        public void CloseWindow()
        {
            mutex.WaitOne();
            if (form != null)
                form.SafeClose();
            mutex.ReleaseMutex();
        }

        public void Clear()
        {
            if (form != null)
                form.ClearCmds();
        }

        public bool IsOpen()
        {
            mutex.WaitOne();
            bool r = form != null;
            mutex.ReleaseMutex();
            return r;
        }
        #endregion

        #region non-published exposedFunctions
        internal void NullifyWindow()
        {
            mutex.WaitOne();
            form = null;
            mutex.ReleaseMutex();
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

        #region private exposedFunctions
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
