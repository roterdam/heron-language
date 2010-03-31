using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HeronEdit
{
    public partial class CodeEditControl : RichTextBox
    {
        const int SB_HORZ = 0;
        const int SB_VERT = 1;
        const string TAB = "    ";

        [DllImport("user32", CharSet = CharSet.Auto)]
        static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, POINT point);

        [DllImport("user32.dll", EntryPoint = "LockWindowUpdate", SetLastError = true, CharSet = CharSet.Auto)] 
        static extern IntPtr LockWindow(IntPtr hWnd); 

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        } 

        public delegate void Proc();

        UndoSystem undoer = new UndoSystem();

        Range changedRange = new Range(0, 0);
        IntPtr eventMask;

        public CodeEditControl()
        {
            InitializeComponent();
        }

        public class TextEventArgs : EventArgs
        {
            int begin;
            int length;
            string text;
            
            public TextEventArgs(int n, int len, string t)
            {
                begin = n;
                length = len;
                text = t;
            }
            public int Begin { get { return begin; } }
            public int Length { get { return length; } }
            public string Text { get { return text; } }
        }

        public string CurrentLine
        {
            get
            {
                int a = CurrentLineStart;
                int b = NextLineStart;
                return Text.Substring(a, b - a);
            }
        }

        public int LineStartFromIndex(int n)
        {
            while (n > 0 && Text[n - 1] != '\n')
                --n;
            return n;
        }

        /// <summary>
        /// Sends a win32 message to get the scrollbars' position.
        /// </summary>
        /// <returns>a POINT structre containing horizontal
        ///       and vertical scrollbar position.</returns>
        private POINT GetScrollPos()
        {
            POINT pos = new POINT();
            SendMessage(Handle, (uint)WindowsMessages.EM_GETSCROLLPOS, IntPtr.Zero, pos);
            return pos;

        }

        /// <summary>
        /// Sends a win32 message to set scrollbars position.
        /// </summary>
        /// <param name="point">a POINT
        ///        conatining H/Vscrollbar scrollpos.</param>
        private void SetScrollPos(POINT point)
        {
            SendMessage(Handle, (uint)WindowsMessages.EM_SETSCROLLPOS, IntPtr.Zero, point);
        }

        public int LineEndFromIndex(int n)
        {
            while (n < TextLength && Text[n] != '\n')
                ++n;
            return n;
        }

        public int CurrentLineStart
        {
            get
            {
                return LineStartFromIndex(SelectionStart);
            }
        }

        public int NextLineStart
        {
            get
            {
                return LineEndFromIndex(SelectionStart);
            }
        }

        public string LinesContainingRange(int a, int b)
        {
            a = LineStartFromIndex(a);
            b = LineEndFromIndex(b);
            return Text.Substring(a, b - a);
        }

        public int CurrentLineIndex 
        {
            get { return GetLineFromCharIndex(SelectionStart); }
        }

        private void StopRepaint()
        {
            // Stop redrawing: 
            SendMessage(this.Handle, (uint)WindowsMessages.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            // Stop sending of events: 
            eventMask = SendMessage(this.Handle, (uint)WindowsMessages.EM_GETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
        }

        private void StartRepaint()
        {
            // turn on events 
            SendMessage(this.Handle, (uint)WindowsMessages.EM_SETEVENTMASK, IntPtr.Zero, eventMask);
            // turn on redrawing 
            SendMessage(this.Handle, (uint)WindowsMessages.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            // this forces a repaint, which for some reason is necessary in some cases. 
            this.Invalidate();
        }

        private int CountNested(string s, char open, char close)
        {
            int r = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                if (s[i] == open) ++r;
                if (s[i] == close) --r;
            }
            return r;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    SelectedText = TAB;
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    {    
                        string s = CurrentLine;
                        int n = s.Length - s.TrimStart().Length;
                        string indent = s.Substring(0, n);
                        SelectedText = "\n" + indent;
                        int cnt = CountNested(s, '{', '}');
                        if (cnt > 0) SelectedText += TAB;
                        e.Handled = true;
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        public void UpdateRtf(string p)
        {
            POINT pt = GetScrollPos();
            int a = SelectionStart;
            int b = SelectionLength;
            try
            {
                LockWindow(Handle);
                base.Rtf = p;
            }
            finally
            {
                SelectionStart = a;
                SelectionLength = b;
                SetScrollPos(pt);
                LockWindow(IntPtr.Zero);
            }
        }

        public void UpdateText(string p)
        {
            POINT pt = GetScrollPos();
            int line = this.SelectionLineIndex();
            int pos = this.SelectionLinePos();
            int len = this.SelectionLength;
            try
            {
                LockWindow(Handle);
                base.Text = p;
            }
            finally
            {
                SetScrollPos(pt);
                int n = this.GetCharIndexFromLineAndPosition(line, pos);
                SelectionStart = n;
                SelectionLength = len;
                LockWindow(IntPtr.Zero);
            }
        }
    }
}
