using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using HeronEngine;

namespace HeronEdit
{
    public class HeronEditor
    {
        #region fields
        CodeEditControl code;
        RichTextBox output;
        private HeronToRtf rtfHelper = new HeronToRtf();
        bool dirty = false;
        Timer timer = new Timer();
        public bool ignoreUpdates;
        Preferences prefs;
        DateTime lastMod;
        UndoSystem undoer = new UndoSystem();
        bool backedUp = true;
        string previousText = "";
        bool modified = false;
        string filename = "";
        SaveFileDialog saveDlg;
        OpenFileDialog openDlg;
        bool bSuspendNotifications = false;
        #endregion 

        #region constructor/destructors
        public HeronEditor(CodeEditControl code, RichTextBox output)
        {
            prefs = new Preferences(this);
            this.code = code;
            this.output = output;
            code.TextChanged += new EventHandler(code_TextChanged);
            code.SelectionChanged += new EventHandler(code_SelectionChanged);
            timer.Interval = 100;
            timer.Enabled = true;
            timer.Tick += new EventHandler(timer_Tick);
            lastMod = DateTime.Now;
        }
        #endregion 

        #region properties
        TimeSpan IdleTime { get { return DateTime.Now - lastMod; } }
        #endregion

        #region editor controls
        public string Text
        {
            get { return code.Text; }
            set { code.Text = Text; }
        }
        public void Select(int n, int length)
        {
            code.Select(n, length);
        }
        public void SelectAll()
        {
            code.SelectAll();
        }
        public bool FindNext(string s)
        {
            int n = Text.IndexOf(s, code.SelectionStart + 1);
            if (n < 0)
                n = Text.IndexOf(s);
            if (n < 0)
                return false;
            Select(n, s.Length);
            return true;
        }
        public bool FindNext(Regex re)
        {
            Match m = re.Match(Text, code.SelectionStart + 1);
            if (!m.Success)
                m = re.Match(Text);
            if (!m.Success)
                return false;
            Select(m.Index, m.Length);
            return true;
        }
        public bool CanCut()
        {
            return code.SelectionLength > 0;
        }
        public bool CanCopy()
        {
            return code.SelectionLength > 0;
        }
        public bool CanPaste()
        {
            return Clipboard.ContainsText();
        }
        public void Cut()
        {
            code.Cut();
        }
        public void Copy()
        {
            code.Copy();
        }
        public void Paste()
        {
            code.Paste(DataFormats.GetFormat(DataFormats.Text));
        }
        public void Delete()
        {
            if (code.SelectionLength > 0)
                code.SelectionLength = 1;
            code.SelectedText = "";
        }
        #endregion
        
        #region undo/redo management functions
        private UndoSystem UndoSystem
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

            if (len1 == 0 && len2 == 0)
                return;

            string t1 = s1.Substring(pre, len1);
            string t2 = s2.Substring(pre, len2);

            Action actionDo = () =>
            {
                Select(pre, len1);
                code.SelectedText = t2;
            };
            Action actionUndo = () =>
            {
                Select(pre, len2);
                code.SelectedText = t1;
            };

            undoer.AddUndo("text change", actionDo, actionUndo);
        }

        public void Undo()
        {
            if (!undoer.CanUndo()) return;
            undoer.Undo();
        }

        public void Redo()
        {
            if (!undoer.CanRedo()) return;
            undoer.Redo();
        }

        public bool CanUndo()
        {
            return undoer.CanUndo();
        }

        public bool CanRedo()
        {
            return undoer.CanRedo();
        }
        #endregion

        #region event handlers 
        void code_SelectionChanged(object sender, EventArgs e)
        {
            lastMod = DateTime.Now;
            //throw new NotImplementedException();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (dirty && (IdleTime.TotalMilliseconds > prefs.IdleBeforeReparse))
            {
                ColorSyntax();
                ParseInput();
                dirty = false;
            }
            if (prefs.AutoBackup && !backedUp && IdleTime.TotalMilliseconds > prefs.IdleBeforeBackup)
            {
                AutoBackup();
            }
        }

        void code_TextChanged(object sender, EventArgs e)
        {
            if (bSuspendNotifications)
                return;
            dirty = true;
            backedUp = false;
            ComputeUndo();
            previousText = Text;
        }
        #endregion

        #region utility functions
        bool IsAlpha(Keys keyCode)
        {
            return keyCode >= Keys.A && keyCode <= Keys.Z;
        }

        bool IsDigit(Keys keyCode)
        {
            return keyCode >= Keys.D0 && keyCode <= Keys.D9;
        }

        bool IsAlphaNumeric(Keys keyCode)
        {
            return IsAlpha(keyCode) || IsDigit(keyCode);
        }

        void Run()
        {
            Save();
            VM.RunFile(filename);
        }

        void ParseInput()
        {
            ClearOutput();
            try
            {
                if (code.Text.Trim().Length == 0)
                {
                    OutputLine("No code available");
                }
                else if (!Parser.Parse(HeronGrammar.File, code.Text))
                {
                    OutputLine("Unknown parse error.");
                }
                else
                {
                    OutputLine("Parsing succeeded.");
                }
            }
            catch (ParsingException e)
            {
                output.AppendText("Parsing exception occured at character " + e.context.col + " of line " + e.context.row + '\n');
                output.AppendText(e.context.msg + '\n');
                output.AppendText(e.context.line + '\n');
                output.AppendText(e.context.ptr + '\n');
            }
            catch (Exception e)
            {
                output.AppendText("Unknown exception occured during parse: " + e.Message);
            }
        }

        void ColorSyntax()
        {
            if (!prefs.AutoColor)
                return;
            string rtf = rtfHelper.ToRtf(code.Text);
            try
            {
                bSuspendNotifications = true;
                code.UpdateRtf(rtf);
            }
            finally
            {
                bSuspendNotifications = false;
            }
        }
        #endregion 

        #region output functions
        public void ClearOutput()
        {
            output.Clear();
        }
        public void Output(string s)
        {
            output.AppendText(s);
        }
        public void OutputLine(string s)
        {
            Output(s + "\n");
        }
        #endregion

        #region document functions
        public void Load(string sFile)
        {
            string s = File.ReadAllText(sFile);
            backedUp = true;
            modified = false;
            filename = sFile;
            code.Text = s;
            ColorSyntax();

            // HACK: there is a strange bug where coloring syntax causes the last line
            // end to disappear. This allows t
            //previousText = code.Text; 
        }

        public void Save(string sFile)
        {
            File.WriteAllText(sFile, code.Text);
            modified = false;
            filename = sFile;
        }

        public bool CanSave()
        {
            return modified;
        }

        public void Save()
        {
            if (File.Exists(filename))
                Save(filename);
            else
                SaveAs();
        }

        public void SaveAs()
        {
            if (saveDlg == null)
            {
                saveDlg = new SaveFileDialog();
                saveDlg.DefaultExt = "heron";
                saveDlg.Filter = "Heron Files (*.heron); All Files (*.*)";
                saveDlg.OverwritePrompt = true;
            }
            if (saveDlg.ShowDialog() == DialogResult.OK)
                Save(saveDlg.FileName);
        }

        public void Open()
        {
            if (openDlg == null)
            {
                openDlg = new OpenFileDialog();
                openDlg.DefaultExt = "heron";
                saveDlg.Filter = "Heron Files (*.heron); All Files (*.*)";
                openDlg.CheckFileExists = true;
            }
            if (openDlg.ShowDialog() == DialogResult.OK)
                Load(openDlg.FileName);
        }
        
        public string FilePath
        {
            get { return filename; } 
        }

        public string FileName
        {
            get { return Path.GetFileName(filename); }
        }

        public string GetBackupFile()
        {
            return prefs.BackupLocation + "//" + FileName + ".backup";
        }

        public void AutoBackup()
        {
            if (!prefs.AutoBackup) return;
            if (!Directory.Exists(prefs.BackupLocation))
                Directory.CreateDirectory(prefs.BackupLocation);
            File.WriteAllText(GetBackupFile(), code.Text);
        }
        #endregion 
    }

    public class Preferences
    {
        HeronEditor editor;

        public Preferences(HeronEditor e)
        {
            editor = e;
        }

        bool autoColor = true;

        public bool AutoColor
        {
            get { return autoColor; }
            set { autoColor = value; }
        }

        bool autoParse = true;

        public bool AutoParse
        {
            get { return autoParse; }
            set { autoParse = value; }
        }
        int idleBeforeReparse = 1000;

        public int IdleBeforeReparse
        {
            get { return idleBeforeReparse; }
            set { idleBeforeReparse = value; }
        }
        bool autoBackup = true;

        public bool AutoBackup
        {
            get { return autoBackup; }
            set { autoBackup = value; }
        }
        string backupLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Heron\\backup";

        public string BackupLocation
        {
            get { return backupLocation; }
            set { backupLocation = value; }
        }

        int idleBeforeBackup = 5000;

        public int IdleBeforeBackup
        {
          get { return idleBeforeBackup; }
          set { idleBeforeBackup = value; }
        }
    }

    public class TextPoint
    {
        RichTextBox textBox;

        public TextPoint(RichTextBox textBox)
        {
            this.textBox = textBox;
        }

        private int column = 0;
        private int row = 0;

        public TextPoint Clone()
        {
            TextPoint r = new TextPoint(textBox);
            r.column = column;
            r.row = row;
            return r;
        }

        public int Column
        {
            get
            {
                return column;
            }
            set
            {
                column = value < 0 ? 0 : value;
            }
        }

        public int Row
        {
            get { return row; }
            set
            {
                if (value < 0)
                    row = 0;
                else if (value > textBox.Lines.Length)
                    row = textBox.Lines.Length;
                else
                    row = value;
            }
        }

        public string Line { get { return ValidLineIndex(row) ? textBox.Lines[row] : ""; } }
        public int LineLength { get { return Line.Length; } }

        private bool ValidLineIndex(int n)
        {
            return n >= 0 && n < textBox.Lines.Length;
        }

        public void MoveRowDown() { Row = Row - 1; }
        public void MoveRowUp() { Row = Row + 1; }
        public void MoveColumnLeft() { MoveColumnLeft(1); }
        public void MoveColumnRight() { MoveColumnRight(1); }
        public void MoveColumnLeft(int n) { Column = Column - n; }
        public void MoveColumnRight(int n) { Column = Column + n; }
        public void MoveLineBeginning() { column = 0; }
        public void MoveLineEnd() { column = LineLength; }
    }
}