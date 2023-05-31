
namespace CLPS2C
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.PanCode = new System.Windows.Forms.Panel();
            this.LblCLPS2C = new System.Windows.Forms.Label();
            this.TxtCodeConv = new System.Windows.Forms.TextBox();
            this.LbLSyncs = new System.Windows.Forms.Label();
            this.PanelTop = new System.Windows.Forms.Panel();
            this.PanelTopRight = new System.Windows.Forms.Panel();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.MenuStripFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripFileNew = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripAutoIndent = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripAutoC = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripSendRaw = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripPCSX2Format = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripScrollPosition = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripShowOpCodes = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStripSnippet = new System.Windows.Forms.ToolStripMenuItem();
            this.BtnCopy = new System.Windows.Forms.Button();
            this.PanelTop.SuspendLayout();
            this.MenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // PanCode
            // 
            this.PanCode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanCode.BackColor = System.Drawing.Color.White;
            this.PanCode.Location = new System.Drawing.Point(12, 61);
            this.PanCode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.PanCode.Name = "PanCode";
            this.PanCode.Size = new System.Drawing.Size(492, 376);
            this.PanCode.TabIndex = 0;
            // 
            // LblCLPS2C
            // 
            this.LblCLPS2C.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.LblCLPS2C.AutoSize = true;
            this.LblCLPS2C.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblCLPS2C.Location = new System.Drawing.Point(300, 9);
            this.LblCLPS2C.Name = "LblCLPS2C";
            this.LblCLPS2C.Size = new System.Drawing.Size(176, 46);
            this.LblCLPS2C.TabIndex = 8;
            this.LblCLPS2C.Text = "CLPS2C";
            this.LblCLPS2C.Click += new System.EventHandler(this.LblCLPS2C_Click);
            // 
            // TxtCodeConv
            // 
            this.TxtCodeConv.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtCodeConv.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)), true);
            this.TxtCodeConv.Location = new System.Drawing.Point(510, 62);
            this.TxtCodeConv.Multiline = true;
            this.TxtCodeConv.Name = "TxtCodeConv";
            this.TxtCodeConv.ReadOnly = true;
            this.TxtCodeConv.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TxtCodeConv.Size = new System.Drawing.Size(278, 376);
            this.TxtCodeConv.TabIndex = 9;
            this.TxtCodeConv.WordWrap = false;
            // 
            // LbLSyncs
            // 
            this.LbLSyncs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LbLSyncs.AutoSize = true;
            this.LbLSyncs.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LbLSyncs.Location = new System.Drawing.Point(597, 40);
            this.LbLSyncs.Name = "LbLSyncs";
            this.LbLSyncs.Size = new System.Drawing.Size(70, 17);
            this.LbLSyncs.TabIndex = 10;
            this.LbLSyncs.Text = "Syncs: 0";
            // 
            // PanelTop
            // 
            this.PanelTop.Controls.Add(this.PanelTopRight);
            this.PanelTop.Controls.Add(this.MenuStrip);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(800, 23);
            this.PanelTop.TabIndex = 11;
            // 
            // PanelTopRight
            // 
            this.PanelTopRight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelTopRight.Location = new System.Drawing.Point(202, 0);
            this.PanelTopRight.Name = "PanelTopRight";
            this.PanelTopRight.Size = new System.Drawing.Size(598, 23);
            this.PanelTopRight.TabIndex = 0;
            // 
            // MenuStrip
            // 
            this.MenuStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuStripFile,
            this.MenuStripSettings,
            this.MenuStripSnippet});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(800, 23);
            this.MenuStrip.TabIndex = 1;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // MenuStripFile
            // 
            this.MenuStripFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuStripFileNew,
            this.MenuStripFileOpen,
            this.MenuStripFileSave,
            this.MenuStripFileSaveAs});
            this.MenuStripFile.Name = "MenuStripFile";
            this.MenuStripFile.Size = new System.Drawing.Size(37, 19);
            this.MenuStripFile.Text = "File";
            // 
            // MenuStripFileNew
            // 
            this.MenuStripFileNew.Name = "MenuStripFileNew";
            this.MenuStripFileNew.ShortcutKeyDisplayString = "Ctrl+N";
            this.MenuStripFileNew.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.MenuStripFileNew.Size = new System.Drawing.Size(195, 22);
            this.MenuStripFileNew.Text = "New";
            this.MenuStripFileNew.Click += new System.EventHandler(this.MenuStripFileNew_Click);
            // 
            // MenuStripFileOpen
            // 
            this.MenuStripFileOpen.Name = "MenuStripFileOpen";
            this.MenuStripFileOpen.ShortcutKeyDisplayString = "Ctrl+O";
            this.MenuStripFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.MenuStripFileOpen.Size = new System.Drawing.Size(195, 22);
            this.MenuStripFileOpen.Text = "Open";
            this.MenuStripFileOpen.Click += new System.EventHandler(this.MenuStripFileOpen_Click);
            // 
            // MenuStripFileSave
            // 
            this.MenuStripFileSave.Name = "MenuStripFileSave";
            this.MenuStripFileSave.ShortcutKeyDisplayString = "Ctrl+S";
            this.MenuStripFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.MenuStripFileSave.Size = new System.Drawing.Size(195, 22);
            this.MenuStripFileSave.Text = "Save";
            this.MenuStripFileSave.Click += new System.EventHandler(this.MenuStripFileSave_Click);
            // 
            // MenuStripFileSaveAs
            // 
            this.MenuStripFileSaveAs.Name = "MenuStripFileSaveAs";
            this.MenuStripFileSaveAs.ShortcutKeyDisplayString = "Ctrl+Shift+S";
            this.MenuStripFileSaveAs.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.MenuStripFileSaveAs.Size = new System.Drawing.Size(195, 22);
            this.MenuStripFileSaveAs.Text = "Save As...";
            this.MenuStripFileSaveAs.Click += new System.EventHandler(this.MenuStripFileSaveAs_Click);
            // 
            // MenuStripSettings
            // 
            this.MenuStripSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuStripAutoIndent,
            this.MenuStripAutoC,
            this.MenuStripSendRaw,
            this.MenuStripPCSX2Format,
            this.MenuStripScrollPosition,
            this.MenuStripShowOpCodes});
            this.MenuStripSettings.Name = "MenuStripSettings";
            this.MenuStripSettings.Size = new System.Drawing.Size(61, 19);
            this.MenuStripSettings.Text = "Settings";
            // 
            // MenuStripAutoIndent
            // 
            this.MenuStripAutoIndent.Checked = true;
            this.MenuStripAutoIndent.CheckOnClick = true;
            this.MenuStripAutoIndent.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuStripAutoIndent.Name = "MenuStripAutoIndent";
            this.MenuStripAutoIndent.Size = new System.Drawing.Size(343, 22);
            this.MenuStripAutoIndent.Text = "Enable auto-indentation";
            // 
            // MenuStripAutoC
            // 
            this.MenuStripAutoC.Checked = true;
            this.MenuStripAutoC.CheckOnClick = true;
            this.MenuStripAutoC.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuStripAutoC.Name = "MenuStripAutoC";
            this.MenuStripAutoC.Size = new System.Drawing.Size(343, 22);
            this.MenuStripAutoC.Text = "Enable auto-completion list";
            // 
            // MenuStripSendRaw
            // 
            this.MenuStripSendRaw.Checked = true;
            this.MenuStripSendRaw.CheckOnClick = true;
            this.MenuStripSendRaw.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuStripSendRaw.Name = "MenuStripSendRaw";
            this.MenuStripSendRaw.Size = new System.Drawing.Size(343, 22);
            this.MenuStripSendRaw.Text = "Enable SendRaw command";
            // 
            // MenuStripPCSX2Format
            // 
            this.MenuStripPCSX2Format.CheckOnClick = true;
            this.MenuStripPCSX2Format.Name = "MenuStripPCSX2Format";
            this.MenuStripPCSX2Format.Size = new System.Drawing.Size(343, 22);
            this.MenuStripPCSX2Format.Text = "PCSX2-Format output";
            this.MenuStripPCSX2Format.CheckedChanged += new System.EventHandler(this.MenuStripPCSX2Format_CheckedChanged);
            // 
            // MenuStripScrollPosition
            // 
            this.MenuStripScrollPosition.Checked = true;
            this.MenuStripScrollPosition.CheckOnClick = true;
            this.MenuStripScrollPosition.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuStripScrollPosition.Name = "MenuStripScrollPosition";
            this.MenuStripScrollPosition.Size = new System.Drawing.Size(343, 22);
            this.MenuStripScrollPosition.Text = "Keep output\'s window scroll bar position after sync";
            // 
            // MenuStripShowOpCodes
            // 
            this.MenuStripShowOpCodes.CheckOnClick = true;
            this.MenuStripShowOpCodes.Name = "MenuStripShowOpCodes";
            this.MenuStripShowOpCodes.Size = new System.Drawing.Size(343, 22);
            this.MenuStripShowOpCodes.Text = "Write opcodes as comments in assembly\'s output";
            // 
            // MenuStripSnippet
            // 
            this.MenuStripSnippet.Name = "MenuStripSnippet";
            this.MenuStripSnippet.Size = new System.Drawing.Size(64, 19);
            this.MenuStripSnippet.Text = "Snippets";
            // 
            // BtnCopy
            // 
            this.BtnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCopy.Location = new System.Drawing.Point(516, 34);
            this.BtnCopy.Name = "BtnCopy";
            this.BtnCopy.Size = new System.Drawing.Size(75, 23);
            this.BtnCopy.TabIndex = 12;
            this.BtnCopy.Text = "Copy";
            this.BtnCopy.UseVisualStyleBackColor = true;
            this.BtnCopy.Click += new System.EventHandler(this.BtnCopy_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.BtnCopy);
            this.Controls.Add(this.LblCLPS2C);
            this.Controls.Add(this.PanelTop);
            this.Controls.Add(this.LbLSyncs);
            this.Controls.Add(this.TxtCodeConv);
            this.Controls.Add(this.PanCode);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.PanelTop.ResumeLayout(false);
            this.PanelTop.PerformLayout();
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel PanCode;
        private System.Windows.Forms.Label LblCLPS2C;
        private System.Windows.Forms.TextBox TxtCodeConv;
        private System.Windows.Forms.Label LbLSyncs;
        private System.Windows.Forms.Panel PanelTop;
        private System.Windows.Forms.Panel PanelTopRight;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.Button BtnCopy;
        private System.Windows.Forms.ToolStripMenuItem MenuStripSettings;
        private System.Windows.Forms.ToolStripMenuItem MenuStripPCSX2Format;
        private System.Windows.Forms.ToolStripMenuItem MenuStripAutoIndent;
        private System.Windows.Forms.ToolStripMenuItem MenuStripScrollPosition;
        private System.Windows.Forms.ToolStripMenuItem MenuStripFile;
        private System.Windows.Forms.ToolStripMenuItem MenuStripFileOpen;
        private System.Windows.Forms.ToolStripMenuItem MenuStripFileSaveAs;
        private System.Windows.Forms.ToolStripMenuItem MenuStripSendRaw;
        private System.Windows.Forms.ToolStripMenuItem MenuStripFileSave;
        private System.Windows.Forms.ToolStripMenuItem MenuStripFileNew;
        private System.Windows.Forms.ToolStripMenuItem MenuStripAutoC;
        private System.Windows.Forms.ToolStripMenuItem MenuStripSnippet;
        private System.Windows.Forms.ToolStripMenuItem MenuStripShowOpCodes;
    }
}

