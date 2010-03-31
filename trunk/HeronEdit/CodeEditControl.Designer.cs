namespace HeronEdit
{
    partial class CodeEditControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param text="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated textbox

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the textbox editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CodeEditControl
            // 
            this.AcceptsTab = true;
            this.AutoWordSelection = true;
            this.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.HideSelection = false;
            this.Text = "+-";
            this.WordWrap = false;
            this.ResumeLayout(false);

        }

        #endregion
    }
}
