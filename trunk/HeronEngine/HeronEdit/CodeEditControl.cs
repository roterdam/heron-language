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

        string previousText = "";
        bool suspendUndoComputation = false;
        bool suspendNotifications = false;
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

        public event EventHandler<TextEventArgs> UndoableTextChangedEvent;

        protected virtual void OnUndoableTextChangedEvent(int n, int len, string t)
        {
            if (UndoableTextChangedEvent != null)
                UndoableTextChangedEvent(this, new TextEventArgs(n, len, t));
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

        public string PreviousText
        {
            get { return previousText; }
        }
        
        protected override void OnTextChanged(EventArgs e)
        {
            if (!suspendUndoComputation)
                ComputeUndo();
            previousText = Text;
            if (!suspendNotifications)
                base.OnTextChanged(e);
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                undoer = new UndoSystem();
                suspendUndoComputation = true;
                try {
                    base.Text = value;
                }
                finally {
                    suspendUndoComputation = false;
                }
            }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Z:
                    if (e.Modifiers == Keys.Control)
                    {
                        Undo();
                        e.Handled = true;                        
                    }
                    break;
                case Keys.Y:
                    if (e.Modifiers == Keys.Control)
                    {
                        Redo();
                        e.Handled = true;
                    }
                    break;
                case Keys.Tab:
                    SelectedText = "  ";
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    {    
                        string s = CurrentLine;
                        int n = s.Length - s.TrimStart().Length;
                        string indent = s.Substring(0, n);
                        SelectedText = "\n" + indent;
                        if (s.Length > n + 2 && s[n] == '<' && s[n + 1] != '/')
                            SelectedText = "  ";
                        else if (s.Length > n && s[n] == '{')
                            SelectedText = "  ";
                        e.Handled = true;
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        #region undo/redo management functions
        public UndoSystem UndoSystem
        {
            get { return undoer; }
            set { undoer = value; }
        }

        public void ComputeUndo()
        {
            // Aliases for the strings, for simplicity
            string s1 = previousText;
            string s2 = Text;

            // Lengths of the strings
            int n1 = s1.Length;
            int n2 = s2.Length;

            // Compute end of common prefix
            int pre = 0;
            while (pre < s1.Length
                && pre < s2.Length
                && s1[pre] == s2[pre])
            {
                ++pre;
            }

            // Compute the length of uncommon strings
            int len1 = n1 - pre;
            int len2 = n2 - pre;

            Trace.Assert(len1 + pre == s1.Length);
            Trace.Assert(len2 + pre == s2.Length);

            // Compute the suffixes
            while (len1 > 0
                && len2 > 0
                && s1[pre + len1 - 1] == s2[pre + len2 - 1])
            {
                len1--;
                len2--;
            }

            Trace.Assert(len1 >= 0);
            Trace.Assert(len2 >= 0);

            string t1 = s1.Substring(pre, len1);
            string t2 = s2.Substring(pre, len2);

            changedRange.Set(pre, len1);

            Action.Procedure actionDo = () =>
            {
                Select(pre, len1);
                SelectedText = t2;
            };
            Action.Procedure actionUndo = () =>
            {
                Select(pre, len2);
                SelectedText = t1;
            };

            undoer.AddUndo("text change", actionDo, actionUndo);

            OnUndoableTextChangedEvent(pre, len1, t2);
        }

        public void SuspendUndo(Proc proc)
        {
            try 
            {
                suspendUndoComputation = true;
                proc();
            }
            finally
            {
                suspendUndoComputation = false;
            }
        }

        public new void Undo()
        {
            if (!undoer.CanUndo()) return;
            SuspendUndo(() => undoer.Undo());
        }

        public new void Redo()
        {
            if (!undoer.CanRedo()) return;
            SuspendUndo(() => undoer.Redo());
        }
        
        public new bool CanUndo()
        {
            return undoer.CanUndo();
        }

        public new bool CanRedo()
        {
            return undoer.CanRedo();
        }

        public bool FindNext(string s)
        {
            int n = Text.IndexOf(s, SelectionStart + 1);
            if (n < 0)
                n = Text.IndexOf(s);
            if (n < 0)
                return false;
            Select(n, s.Length);
            return true;           
        }

        public bool FindNext(Regex re)
        {
            Match m = re.Match(Text, SelectionStart + 1);
            if (!m.Success)
                m = re.Match(Text);
            if (!m.Success)
                return false;
            Select(m.Index, m.Length);
            return true;
        }
        #endregion

        public void UpdateRtf(string p)
        {
            POINT pt = GetScrollPos();
            int a = SelectionStart;
            int b = SelectionLength;
            suspendUndoComputation = true;
            suspendNotifications = true;
            try
            {
                LockWindow(Handle);
                base.Rtf = p;
            }
            finally
            {
                suspendUndoComputation = false;
                suspendNotifications = false;
                SelectionStart = a;
                SelectionLength = b;
                LockWindow(IntPtr.Zero);
                SetScrollPos(pt);
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
                suspendNotifications = true;
                suspendUndoComputation = true;
                LockWindow(Handle);
                base.Text = p;
            }
            finally
            {
                suspendNotifications = false;
                suspendUndoComputation = false;
                SetScrollPos(pt);
                int n = this.GetCharIndexFromLineAndPosition(line, pos);
                SelectionStart = n;
                SelectionLength = len;
                LockWindow(IntPtr.Zero);
            }
        }
    }
}
