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
    /// <summary>
    /// The CodeEditControl is a specialzied RichTextBox control that is intended for use 
    /// as a code editor. It provides some helper functions for coloring of text, and has some 
    /// default proeprty values that I find useful when writing text editors of code.
    /// For example: 
    /// <list type="unordered">
    /// <item>tab is replaced by spaces</item>
    /// <item>when setting rich text (using UpdateRtf()) scrollbars don't move</item>
    /// <item>when setting rich text (using UpdateRtf()) painting is halted to prevent flicker</item>
    /// </list>
    /// </summary>
    public partial class CodeEditControl : RichTextBox
    {
        #region constants 
        /// <summary>
        /// Identifies the horizontal scrollbar to the Windows API.
        /// </summary>
        const int SB_HORZ = 0;

        /// <summary>
        /// Identifies the vertical scrollbar to the Windows API.
        /// </summary>
        const int SB_VERT = 1;
        
        /// <summary>
        /// When pressing tab the following string is inserted instead. 
        /// </summary>
        const string TAB = "    ";
        #endregion

        #region Useful Windows API functions and structs not exposed to CLR
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
        #endregion

        #region fields
        /// <summary>
        /// Prevents TextChanged events from firing.
        /// </summary>
        bool bSuspendNotifications = false;

        /// <summary>
        /// Manages the undo and redo stack
        /// </summary>
        UndoSystem undoer = new UndoSystem();
        
        /// <summary>
        /// Used for StartRepaint() and StopRepaint() messages
        /// </summary>
        IntPtr eventMask;
        #endregion

        #region property overrides 
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                try
                {
                    bSuspendNotifications = true;
                    base.Text = value;
                }
                finally
                {
                    bSuspendNotifications = false;
                }
            }
        }
        #endregion 

        /// <summary>
        /// Constructor
        /// </summary>
        public CodeEditControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Returns the text of the entire line containing the current
        /// selection point.
        /// </summary>
        public string CurrentLine
        {
            get
            {
                int a = CurrentLineStart;
                int b = NextLineStart;
                return Text.Substring(a, b - a);
            }
        }

        /// <summary>
        /// Given a character offset, returns the offset of the first 
        /// character of the containing line.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the index of the last character in a line.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public int LineEndFromIndex(int n)
        {
            while (n < TextLength && Text[n] != '\n')
                ++n;
            return n;
        }

        /// <summary>
        /// Returns the index of the beginning of the current line.
        /// </summary>
        public int CurrentLineStart
        {
            get
            {
                return LineStartFromIndex(SelectionStart);
            }
        }

        /// <summary>
        /// Returns the character index of the beginning of the 
        /// next line.
        /// </summary>
        public int NextLineStart
        {
            get
            {
                return LineEndFromIndex(SelectionStart);
            }
        }

        /// <summary>
        /// Given a start point and an end point returns all lines 
        /// of text between those two points, inclusively.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public string LinesContainingRange(int start, int end)
        {
            int a = LineStartFromIndex(start);
            int b = LineEndFromIndex(end);
            return Text.Substring(a, b - a);
        }

        /// <summary>
        /// The line index of the current selection point
        /// </summary>
        public int CurrentLineIndex 
        {
            get { return GetLineFromCharIndex(SelectionStart); }
        }

        /// <summary>
        /// Forces windows to prevent painting of the control.
        /// </summary>
        private void StopRepaint()
        {
            // Stop redrawing: 
            SendMessage(this.Handle, (uint)WindowsMessages.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            // Stop sending of events: 
            eventMask = SendMessage(this.Handle, (uint)WindowsMessages.EM_GETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Tells windows to allow painting of the control.
        /// </summary>
        private void StartRepaint()
        {
            // turn on events 
            SendMessage(this.Handle, (uint)WindowsMessages.EM_SETEVENTMASK, IntPtr.Zero, eventMask);
            // turn on redrawing 
            SendMessage(this.Handle, (uint)WindowsMessages.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            // this forces a repaint, which for some reason is necessary in some cases. 
            this.Invalidate();
        }

        /// <summary>
        /// A utility function for counting nested character pairs (e.g. { }).
        /// It will return 0 if the number of open characters matches the closing 
        /// character. This is useful for computing how much to indent the next line.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="open"></param>
        /// <param name="close"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Overrides the behavior of OnKeyDown so that the enter 
        /// key causes the next line to be indented appropriately
        /// according to how much indent the previous line has, and 
        /// how many { and } characters it has.
        /// </summary>  
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
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
                case Keys.Insert:
                    if (e.Control)
                    {
                        e.Handled = true;
                        Paste();
                    }
                    break;
                case Keys.V:
                    if (e.Control)
                    {
                        e.Handled = true;
                        Paste();
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Sets the rich text property (Rtf) of the rich text 
        /// control, while assuring that the scroll bar doesn't 
        /// move and that the selection point doesn't change. 
        /// This is done without flicker.
        /// </summary>
        /// <param name="p"></param>
        public void UpdateRtf(string p)
        {
            POINT pt = GetScrollPos();
            int a = SelectionStart;
            int b = SelectionLength;
            try
            {
                bSuspendNotifications = true;
                LockWindow(Handle);
                base.Rtf = p.Replace("\t", TAB);
            }
            finally
            {
                SelectionStart = a;
                SelectionLength = b;
                SetScrollPos(pt);
                LockWindow(IntPtr.Zero);
                bSuspendNotifications = false;
            }
        }

        /// <summary>
        /// Sets the text property (Text) of the rich text 
        /// control, while assuring that the scroll bar doesn't 
        /// move and that the selection point doesn't change. 
        /// This is done without flicker.
        /// </summary>
        /// <param name="p"></param>
        public void UpdateText(string p)
        {
            POINT pt = GetScrollPos();
            int line = this.SelectionLineIndex();
            int pos = this.SelectionLinePos();
            int len = this.SelectionLength;
            try
            {
                bSuspendNotifications = true;
                LockWindow(Handle);
                base.Text = p.Replace("\t", TAB);
            }
            finally
            {
                SetScrollPos(pt);
                int n = this.GetCharIndexFromLineAndPosition(line, pos);
                SelectionStart = n;
                SelectionLength = len;
                LockWindow(IntPtr.Zero);
                bSuspendNotifications = false;
            }
        }
    
        /// <summary>
        /// Assures that pasting of code only cause plain-text 
        /// to be inserted, and replaces tab characters with whitespace.
        /// </summary>
        public new void Paste()
        {
            string s = Clipboard.GetText();
            s = s.Replace("\t", TAB);
            SelectedText = s;
        }

        /// <summary>
        /// Triggers the TextChanged message.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(EventArgs e)
        {
            if (!bSuspendNotifications)
                base.OnTextChanged(e);
        }

        /// <summary>
        /// Triggers a DragDrop message.
        /// </summary>
        /// <param name="drgevent"></param>
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            bSuspendNotifications = true;
            try 
            { 
                base.OnDragDrop(drgevent); 
            }
            finally 
            { 
                bSuspendNotifications = false; 
            }
        }
    }
}
