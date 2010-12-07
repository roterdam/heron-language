using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

using HeronEngine;
using Peg;

namespace HeronEdit
{
    /// <summary>
    /// This class coordinates the various controls and applciation of the HeronEdit 
    /// application. It is exposed to macros, for automation.
    /// </summary>
    public class HeronEditor
    {
        #region fields
        CodeEditControl code;
        RichTextBox output;
        MenuStrip menu;
        HeronToRtf rtfHelper = new HeronToRtf();
        bool idleChecked = false;
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
        /// <summary>
        /// Creates a HeronEditor given a CodeEditControl for the main document, 
        /// a RichTextBox instance for error messages (e.g. parsing messages)
        /// and a MenuStrip.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="output"></param>
        /// <param name="menu"></param>
        public HeronEditor(CodeEditControl code, RichTextBox output, MenuStrip menu)
        {
            prefs = new Preferences();
            this.code = code;
            this.output = output;
            this.menu = menu;
            code.TextChanged += new EventHandler(code_TextChanged);
            code.SelectionChanged += new EventHandler(code_SelectionChanged);
            timer.Interval = 100;
            timer.Enabled = true;
            timer.Tick += new EventHandler(timer_Tick);
            lastMod = DateTime.Now;
        }
        #endregion 

        #region properties
        /// <summary>
        /// The IdleTime is used for deciding when to perform computationally expensive 
        /// tasks such as recoloring the text. This keeps application responsiveness good.
        /// </summary>
        public TimeSpan IdleTime { get { return DateTime.Now - lastMod; } }
        /// <summary>
        /// This is the main text of the document, in the primary edit control.
        /// </summary>
        public string Text { get { return code.Text; } set { code.Text = Text; } }
        /// <summary>
        /// This is the menu control of the main editor form. 
        /// </summary>
        public MenuStrip Menu { get { return menu; } }
        /// <summary>
        /// Exposes the custom undo / redo system. 
        /// </summary>
        public UndoSystem UndoSystem
        {
            get { return undoer; }
            set { undoer = value; }
        }
        /// <summary>
        /// Exposes the editor control
        /// </summary>
        public CodeEditControl EditControl { get { return code; } }
        #endregion

        #region editor controls
        /// <summary>
        /// Selects text of a specified length,
        /// from the specified start point in the main editor.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="length"></param>
        public void Select(int n, int length)
        {
            code.Select(n, length);
        }
        /// <summary>
        ///  Selects all text.
        /// </summary>
        public void SelectAll()
        {
            code.SelectAll();
        }
        /// <summary>
        /// Selects the next instance of a string, afer the current selection point.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Selects the next match of a regular expressison, after the current selection point.
        /// </summary>
        /// <param name="re"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Returns true if code is selected in the main editor.
        /// </summary>
        /// <returns></returns>
        public bool CanCut()
        {
            return code.SelectionLength > 0;
        }
        /// <summary>
        /// Returns true if code is selected in the main editor.
        /// </summary>
        /// <returns></returns>
        public bool CanCopy()
        {
            return code.SelectionLength > 0;
        }
        /// <summary>
        /// Returns true if the clipboard contains text.
        /// </summary>
        /// <returns></returns>
        public bool CanPaste()
        {
            return Clipboard.ContainsText();
        }
        /// <summary>
        /// Cuts selected text from the main edit control into the clipboard.
        /// </summary>
        /// <returns></returns>
        public void Cut()
        {
            code.Cut();
        }
        /// <summary>
        /// Copies the slected text from the main edit control into the clipboard.
        /// </summary>
        /// <returns></returns>
        public void Copy()
        {
            code.Copy();
        }
        /// <summary>
        /// Pastes text into the main edit control.
        /// </summary>
        /// <returns></returns>
        public void Paste()
        {
            code.Paste();
        }
        /// <summary>
        /// Deletes the selected text from the main edit control,
        /// or if no text is selected, will delete the next character. 
        /// </summary>
        /// <returns></returns>
        public void Delete()
        {
            if (code.SelectionLength > 0)
                code.SelectionLength = 1;
            code.SelectedText = "";
        }
        #endregion
        
        #region undo/redo management functions
        /// <summary>
        /// Computes the text which changed, and creates 
        /// an undo entry in the undo system. This uses a rather 
        /// naive algorithm to compare the previous text state with the 
        /// current text state. It seems to work well enough in 
        /// practice, but probably wouldn't be hard to optimize with 
        /// a bit of effort. I like it because the code complexity is low:
        /// we only have to store the text state of the entry, every time it 
        /// chances. 
        /// </summary>
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

            modified = true;
            backedUp = false;
            previousText = Text;
            undoer.AddUndo("text change", actionDo, actionUndo);
        }

        /// <summary>
        /// Undoes the next action in the undo stack.
        /// </summary>
        public void Undo()
        {
            if (!undoer.CanUndo()) return;
            undoer.Undo();
        }

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        public void Redo()
        {
            if (!undoer.CanRedo()) return;
            undoer.Redo();
        }

        /// <summary>
        /// Returns true if there is an undoable action in the undo stack.
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return undoer.CanUndo();
        }

        /// <summary>
        /// Returns true if there is an redoable action in the redo stack.
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return undoer.CanRedo();
        }
        #endregion

        #region event handlers 
        /// <summary>
        /// Called whenever the selection changes in the main
        /// edit control. Updates the last-modified 
        /// time stamp for computing idle time. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void code_SelectionChanged(object sender, EventArgs e)
        {
            lastMod = DateTime.Now;
        }

        /// <summary>
        /// Responds to a timer tick. Performs a reparse and syntax coloring
        /// if the idle time is greater than some threshold. This is done 
        /// to assure that parsing and coloring does not affect performance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timer_Tick(object sender, EventArgs e)
        {
            if (idleChecked && (IdleTime.TotalMilliseconds > prefs.IdleBeforeReparse))
            {
                ColorSyntax();
                ParseInput();
                idleChecked = false;
            }
            if (prefs.AutoBackup && !backedUp && IdleTime.TotalMilliseconds > prefs.IdleBeforeBackup)
            {
                AutoBackup();
            }
        }

        /// <summary>
        /// Called whenever text changes in the main edit control. 
        /// Sets the idleChecked bit to true, sets the backedUp bit to false, 
        /// computes an undo, and stores the previous text state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void code_TextChanged(object sender, EventArgs e)
        {
            if (bSuspendNotifications)
                return;
            idleChecked = true;
            ComputeUndo();
        }
        #endregion

        #region utility functions
        /// <summary>
        /// Returns true if the key code is for a letter key.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        bool IsAlpha(Keys keyCode)
        {
            return keyCode >= Keys.A && keyCode <= Keys.Z;
        }

        /// <summary>
        /// Returns true if the key code is for a number key.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        bool IsDigit(Keys keyCode)
        {
            return keyCode >= Keys.D0 && keyCode <= Keys.D9;
        }

        /// <summary>
        /// Returns true if the key code is for a letter or number key.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        bool IsAlphaNumeric(Keys keyCode)
        {
            return IsAlpha(keyCode) || IsDigit(keyCode);
        }

        /// <summary>
        /// Runs the Heron parser on the input text and outputs the first found 
        /// parse error in the output pane.
        /// </summary>
        void ParseInput()
        {
            ClearOutput();
            try
            {
                string s = code.Text; 
                if (s.Trim().Length == 0)
                {
                    OutputLine("No code available");
                }
                else if (!Parser.Parse(HeronGrammar.File, s))
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

        /// <summary>
        /// Colors the text in the main edit control using a fixed coloring scheme 
        /// for Heron sytnax. 
        /// </summary>
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
        /// <summary>
        /// Clears the text of the output pane
        /// </summary>
        public void ClearOutput()
        {
            output.Clear();
        }
        /// <summary>
        /// Adds some text to the output pane editor.
        /// </summary>
        /// <param name="s"></param>
        public void Output(string s)
        {
            output.AppendText(s);
        }
        /// <summary>
        /// Adds a line of text to the output pane editor.
        /// </summary>
        /// <param name="s"></param>
        public void OutputLine(string s)
        {
            Output(s + "\n");
        }
        #endregion

        #region document functions
        /// <summary>
        /// Creates a new document with a blank name.
        /// Saves a previous document if modified.
        /// </summary>
        public void New()
        {
            if (!SaveIfModified())
                return;
            filename = "";
            code.Text = prefs.NewText;
        }

        /// <summary>
        /// If document was modified, queries the user. Returns false, 
        /// if the user presses cancel, or says they want to save and 
        /// does not save. Returns true otherwise.
        /// </summary>
        /// <returns></returns>
        public bool SaveIfModified()
        {
            if (modified)
            {
                DialogResult dr = MessageBox.Show("Documented was modified, do you want to save?", "Document modified", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes)
                {
                    if (!Save())
                        return false;
                }
                else if (dr == DialogResult.No)
                {
                    return true;
                }
                else // dr == DialogResult.Cancel
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Opens the document from the specified file. 
        /// </summary>
        /// <param name="sFile"></param>
        public void Open(string sFile)
        {
            string s = File.ReadAllText(sFile);
            filename = sFile;
            bSuspendNotifications = true;
            code.Text = s;
            bSuspendNotifications = false;
            ColorSyntax();
            backedUp = true;
            modified = false;
            previousText = Text;
            undoer.Clear();
        }

        /// <summary>
        /// Saves the document to the specified file.
        /// </summary>
        /// <param name="sFile"></param>
        public void Save(string sFile)
        {
            File.WriteAllText(sFile, code.Text);
            modified = false;
            filename = sFile;
        }

        /// <summary>
        /// Runs the current program.
        /// </summary>
        public void Run()
        {
            if (!SaveIfModified())
                return;
            Process p = new Process();
            p.StartInfo.WorkingDirectory = GetAppDirectory(); 
            p.StartInfo.FileName = GetAppDirectory() + "\\HeronEngine.exe";
            p.StartInfo.Arguments = "\"" + filename + "\"";
            p.Start();
        }

        /// <summary>
        /// Returns true if and only if the file was modified.
        /// </summary>
        /// <returns></returns>
        public bool CanSave()
        {
            return modified;
        }

        /// <summary>
        /// Saves the file using the most recently used name.
        /// If no name was used then a dialog is opened.
        /// If the user presses cancel, this will return false.
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if (filename.Length > 0)
            {
                Save(filename);
                return true;
            }
            else
            {
                return SaveAs();
            }
        }

        /// <summary>
        /// Used to set the properties shared between the OpenDialog and SaveDialog.
        /// </summary>
        /// <param name="fd"></param>
        void InitializeFileDialog(FileDialog fd)
        {
            fd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            fd.FileName = FileName;
            fd.DefaultExt = "heron";
            fd.Filter = "Heron Files (*.heron)|*.heron|All Files (*.*)|*.*";
        }

        /// <summary>
        /// Represents the behavior as if the user pressed "SaveAs()".
        /// </summary>
        /// <returns></returns>
        public bool SaveAs()
        {
            if (saveDlg == null)
            {
                saveDlg = new SaveFileDialog();
                InitializeFileDialog(saveDlg);
                saveDlg.OverwritePrompt = true;
            }
            if (saveDlg.ShowDialog() != DialogResult.OK)
                return false;

            Save(saveDlg.FileName);
            return true;
        }
        
        /// <summary>
        /// Provides the user with an opportunity to save the current
        /// document if modified. If the user cancels then the open 
        /// file process is halted. Otherwise the user is shown a 
        /// dialog box and they can save it.
        /// </summary>
        public void Open()
        {
            if (!SaveIfModified())
                return;
            if (openDlg == null)
            {
                openDlg = new OpenFileDialog();
                InitializeFileDialog(openDlg);
                openDlg.CheckFileExists = true;
            }
            if (openDlg.ShowDialog() == DialogResult.OK)
                Open(openDlg.FileName);
        }
        
        /// <summary>
        /// Returns the full path of the currently opened file.
        /// </summary>
        public string FilePath
        {
            get { return filename; } 
        }

        /// <summary>
        /// Returns the name and extension of the currently opened file (no directory).
        /// </summary>
        public string FileName
        {
            get { return Path.GetFileName(filename); }
        }

        /// <summary>
        /// Returns the full path to the backup file.
        /// </summary>
        /// <returns></returns>
        public string GetBackupFile()
        {
            return prefs.BackupLocation + "//" + FileName + ".backup";
        }

        /// <summary>
        /// Performs an auto-backup.
        /// </summary>
        public void AutoBackup()
        {
            if (!prefs.AutoBackup) return;
            if (!Directory.Exists(prefs.BackupLocation))
                Directory.CreateDirectory(prefs.BackupLocation);
            File.WriteAllText(GetBackupFile(), code.Text);
        }

        /// <summary>
        /// Returns the application directory.
        /// </summary>
        /// <returns></returns>
        public string GetAppDirectory()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        /// <summary>
        /// Returns the name of the file used to store the macro. 
        /// </summary>
        /// <returns></returns>
        public string GetMacroFile()
        {
            return GetAppDirectory() + "//macros//Macros.heron";
        }

        /// <summary>
        /// Runs the named macro.
        /// </summary>
        public void RunMacro(string s)
        {
            try
            {
                string sFile = GetMacroFile();
                VM vm = new VM();
                vm.InitializeVM();
                vm.RegisterDotNetType(typeof (HeronEditor));
                vm.RegisterDotNetType(typeof(CodeEditControl));
                vm.RegisterDotNetType(typeof(Preferences));
                
                //vm.RegisterAssembly(Assembly.GetExecutingAssembly());
                vm.RegisterCommonWinFormTypes();
                ModuleDefn m = vm.LoadModule(sFile);
                vm.LoadDependentModules(sFile);
                vm.ResolveTypes
                    ();
                ModuleInstance mi = m.Instantiate(vm, new HeronValue[] { }, null) as ModuleInstance;
                vm.RunMeta(mi);
                HeronValue f = mi.GetFieldOrMethod("RunMacro");
                if (f == null)
                    throw new Exception("Could not find a 'Main' method to run");
                f.Apply(vm, new HeronValue[] { DotNetObject.Marshal(this), DotNetObject.Marshal(s) });
            }
            catch (Exception e)
            {
                MessageBox.Show("Error during macro: " + e.Message);
            }
        }
        
        /// <summary>
        /// Opens a new instance of the HeronEditor with the specified 
        /// file opened.
        /// </summary>
        /// <param name="file"></param>
        public void LaunchNewEditor(string file)
        {
            Process p = new Process();
            p.StartInfo.FileName = Application.ExecutablePath;
            p.StartInfo.Arguments = "\"" + file + "\"";
            p.Start();
        }
        #endregion
    }

    /// <summary>
    /// Contains various configuration opens for the editor.
    /// This will probably be serialized to a file in later version, 
    /// and the user may be given the option to control. Macros
    /// have access to an instance of this class.
    /// </summary>
    public class Preferences
    {
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
        int idleBeforeReparse = 500;

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

        string newText = @"module MyModule
{
    imports
    {
        // This is a list of imported modules
        console = new Windows.Heron.Console();
    }
    fields 
    {
        // place module data declarations here 
    }
    methods
    {
        // place module methods here. 
        
        // The method named ""Main"" is the entry point 
        // of an application.
        Main() 
        {
            // Plase the main code here
        }
    }
}

// Place class, interface, and enum definitions here.
";

        public string NewText
        {
            get { return newText; }
            set { newText = value; }
        }
    }

    /// <summary>
    /// Not currently used. 
    /// </summary>
    class TextPoint
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