namespace TestAgentMCP
{
	partial class MainForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			txtPrompt = new TextBox();
			btnApplyPrompt = new Button();
			btnOpenLog = new Button();
			pnlToolbar = new Panel();
			btnClearAll = new Button();
			btnColor = new Button();
			btnAddLosange = new Button();
			btnAddCircle = new Button();
			chkDeleteMode = new CheckBox();
			btnAddSquare = new Button();
			txtStatus = new TextBox();
			menuStrip = new MenuStrip();
			mnuFile = new ToolStripMenuItem();
			mnuSavePreset = new ToolStripMenuItem();
			mnuLoadPreset = new ToolStripMenuItem();
			mnuEdit = new ToolStripMenuItem();
			mnuUndo = new ToolStripMenuItem();
			pnlPlayer = new Panel();
			btnPlay = new Button();
			sliderFrame = new TrackBar();
			lblFrame = new Label();
			btnAddFrame = new Button();
			btnDeleteFrame = new Button();
			playTimer = new System.Windows.Forms.Timer(components);
			toolTip = new ToolTip(components);
			toolTip.SetToolTip(btnApplyPrompt, "Envoyer le prompt à l'agent OpenAI (Entrée)");
			toolTip.SetToolTip(btnOpenLog, "Ouvrir le fichier de log");
			toolTip.SetToolTip(btnClearAll, "Supprimer toutes les formes");
			toolTip.SetToolTip(btnColor, "Choisir la couleur des formes");
			toolTip.SetToolTip(btnAddSquare, "Ajouter un carré");
			toolTip.SetToolTip(btnAddCircle, "Ajouter un cercle");
			toolTip.SetToolTip(btnAddLosange, "Ajouter un losange");
			toolTip.SetToolTip(chkDeleteMode, "Mode suppression : un clic sur une forme la supprime");
			toolTip.SetToolTip(btnPlay, "Lecture / pause de l'animation");
			toolTip.SetToolTip(sliderFrame, "Naviguer entre les frames");
			toolTip.SetToolTip(btnAddFrame, "Ajouter une frame (copie de la frame affichée)");
			toolTip.SetToolTip(btnDeleteFrame, "Supprimer la frame affichée");
			pnlToolbar.SuspendLayout();
			menuStrip.SuspendLayout();
			pnlPlayer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)sliderFrame).BeginInit();
			SuspendLayout();
			// 
			// txtPrompt
			// 
			txtPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtPrompt.Location = new Point(12, 40);
			txtPrompt.Name = "txtPrompt";
			txtPrompt.PlaceholderText = "Ex: mets le carré rouge en bas à droite, et le bleu en jaune";
			txtPrompt.Size = new Size(796, 27);
			txtPrompt.TabIndex = 0;
			// 
			// btnApplyPrompt
			// 
			btnApplyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnApplyPrompt.Location = new Point(814, 38);
			btnApplyPrompt.Name = "btnApplyPrompt";
			btnApplyPrompt.Size = new Size(75, 34);
			btnApplyPrompt.TabIndex = 1;
			btnApplyPrompt.Text = "Envoyer";
			btnApplyPrompt.UseVisualStyleBackColor = true;
			// 
			// btnOpenLog
			// 
			btnOpenLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnOpenLog.Location = new Point(895, 38);
			btnOpenLog.Name = "btnOpenLog";
			btnOpenLog.Size = new Size(49, 34);
			btnOpenLog.TabIndex = 2;
			btnOpenLog.Text = "Log";
			btnOpenLog.UseVisualStyleBackColor = true;
			// 
			// pnlToolbar
			// 
			pnlToolbar.BackColor = Color.DimGray;
			pnlToolbar.Controls.Add(btnClearAll);
			pnlToolbar.Controls.Add(btnColor);
			pnlToolbar.Controls.Add(btnAddLosange);
			pnlToolbar.Controls.Add(btnAddCircle);
			pnlToolbar.Controls.Add(chkDeleteMode);
			pnlToolbar.Controls.Add(btnAddSquare);
			pnlToolbar.Dock = DockStyle.Right;
			pnlToolbar.ForeColor = Color.Black;
			pnlToolbar.Location = new Point(952, 28);
			pnlToolbar.Name = "pnlToolbar";
			pnlToolbar.Size = new Size(72, 740);
			pnlToolbar.TabIndex = 4;
			// 
			// btnClearAll
			// 
			btnClearAll.Location = new Point(8, 172);
			btnClearAll.Name = "btnClearAll";
			btnClearAll.Size = new Size(56, 34);
			btnClearAll.TabIndex = 4;
			btnClearAll.Text = "Clear";
			btnClearAll.UseVisualStyleBackColor = true;
			// 
			// btnColor
			// 
			btnColor.Location = new Point(8, 212);
			btnColor.Name = "btnColor";
			btnColor.Size = new Size(56, 34);
			btnColor.TabIndex = 5;
			btnColor.UseVisualStyleBackColor = true;
			// 
			// btnAddLosange
			// 
			btnAddLosange.Location = new Point(8, 92);
			btnAddLosange.Name = "btnAddLosange";
			btnAddLosange.Size = new Size(56, 34);
			btnAddLosange.TabIndex = 3;
			btnAddLosange.Text = "◇";
			btnAddLosange.UseVisualStyleBackColor = true;
			// 
			// btnAddCircle
			// 
			btnAddCircle.Location = new Point(8, 52);
			btnAddCircle.Name = "btnAddCircle";
			btnAddCircle.Size = new Size(56, 34);
			btnAddCircle.TabIndex = 2;
			btnAddCircle.Text = "○";
			btnAddCircle.UseVisualStyleBackColor = true;
			// 
			// chkDeleteMode
			// 
			chkDeleteMode.Appearance = Appearance.Button;
			chkDeleteMode.Location = new Point(8, 132);
			chkDeleteMode.Name = "chkDeleteMode";
			chkDeleteMode.Size = new Size(56, 34);
			chkDeleteMode.TabIndex = 1;
			chkDeleteMode.Text = "Del";
			chkDeleteMode.TextAlign = ContentAlignment.MiddleCenter;
			chkDeleteMode.UseVisualStyleBackColor = true;
			// 
			// btnAddSquare
			// 
			btnAddSquare.Location = new Point(8, 12);
			btnAddSquare.Name = "btnAddSquare";
			btnAddSquare.Size = new Size(56, 34);
			btnAddSquare.TabIndex = 0;
			btnAddSquare.Text = "□";
			btnAddSquare.UseVisualStyleBackColor = true;
			// 
			// txtStatus
			// 
			txtStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtStatus.Location = new Point(12, 82);
			txtStatus.Multiline = true;
			txtStatus.Name = "txtStatus";
			txtStatus.ReadOnly = true;
			txtStatus.ScrollBars = ScrollBars.Vertical;
			txtStatus.Size = new Size(932, 56);
			txtStatus.TabIndex = 3;
			// 
			// menuStrip
			// 
			menuStrip.ImageScalingSize = new Size(20, 20);
			menuStrip.Items.AddRange(new ToolStripItem[] { mnuFile, mnuEdit });
			menuStrip.Location = new Point(0, 0);
			menuStrip.Name = "menuStrip";
			menuStrip.Size = new Size(1024, 28);
			menuStrip.TabIndex = 5;
			// 
			// mnuFile
			// 
			mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuSavePreset, mnuLoadPreset });
			mnuFile.Name = "mnuFile";
			mnuFile.Size = new Size(66, 24);
			mnuFile.Text = "&Fichier";
			// 
			// mnuSavePreset
			// 
			mnuSavePreset.Name = "mnuSavePreset";
			mnuSavePreset.ShortcutKeys = Keys.Control | Keys.S;
			mnuSavePreset.Size = new Size(283, 26);
			mnuSavePreset.Text = "&Enregistrer le preset...";
			// 
			// mnuLoadPreset
			// 
			mnuLoadPreset.Name = "mnuLoadPreset";
			mnuLoadPreset.ShortcutKeys = Keys.Control | Keys.O;
			mnuLoadPreset.Size = new Size(283, 26);
			mnuLoadPreset.Text = "&Charger un preset...";
			// 
			// mnuEdit
			// 
			mnuEdit.DropDownItems.AddRange(new ToolStripItem[] { mnuUndo });
			mnuEdit.Name = "mnuEdit";
			mnuEdit.Size = new Size(70, 24);
			mnuEdit.Text = "&Édition";
			// 
			// mnuUndo
			// 
			mnuUndo.Name = "mnuUndo";
			mnuUndo.ShortcutKeys = Keys.Control | Keys.Z;
			mnuUndo.Size = new Size(194, 26);
			mnuUndo.Text = "&Annuler";
			// 
			// pnlPlayer
			// 
			pnlPlayer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			pnlPlayer.BackColor = Color.FromArgb(60, 60, 60);
			pnlPlayer.Controls.Add(btnPlay);
			pnlPlayer.Controls.Add(sliderFrame);
			pnlPlayer.Controls.Add(lblFrame);
			pnlPlayer.Controls.Add(btnAddFrame);
			pnlPlayer.Controls.Add(btnDeleteFrame);
			pnlPlayer.Location = new Point(0, 724);
			pnlPlayer.Name = "pnlPlayer";
			pnlPlayer.Size = new Size(952, 44);
			pnlPlayer.TabIndex = 6;
			pnlPlayer.Visible = false;
			// 
			// btnPlay
			// 
			btnPlay.Location = new Point(8, 6);
			btnPlay.Name = "btnPlay";
			btnPlay.Size = new Size(44, 32);
			btnPlay.TabIndex = 0;
			btnPlay.Text = "▶";
			btnPlay.UseVisualStyleBackColor = true;
			// 
			// sliderFrame
			// 
			sliderFrame.AutoSize = false;
			sliderFrame.Location = new Point(60, 4);
			sliderFrame.Maximum = 1;
			sliderFrame.Name = "sliderFrame";
			sliderFrame.Size = new Size(560, 36);
			sliderFrame.TabIndex = 1;
			sliderFrame.TickStyle = TickStyle.None;
			// 
			// lblFrame
			// 
			lblFrame.ForeColor = Color.White;
			lblFrame.Location = new Point(630, 6);
			lblFrame.Name = "lblFrame";
			lblFrame.Size = new Size(100, 32);
			lblFrame.TabIndex = 2;
			lblFrame.Text = "1 / 2";
			lblFrame.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// btnAddFrame
			// 
			btnAddFrame.Location = new Point(744, 6);
			btnAddFrame.Name = "btnAddFrame";
			btnAddFrame.Size = new Size(44, 32);
			btnAddFrame.TabIndex = 4;
			btnAddFrame.Text = "+";
			btnAddFrame.UseVisualStyleBackColor = true;
			// 
			// btnDeleteFrame
			// 
			btnDeleteFrame.Location = new Point(794, 6);
			btnDeleteFrame.Name = "btnDeleteFrame";
			btnDeleteFrame.Size = new Size(44, 32);
			btnDeleteFrame.TabIndex = 5;
			btnDeleteFrame.Text = "🗑";
			btnDeleteFrame.UseVisualStyleBackColor = true;
			// 
			// playTimer
			// 
			playTimer.Interval = 66;
			// 
			// MainForm
			// 
			AcceptButton = btnApplyPrompt;
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1024, 768);
			Controls.Add(pnlPlayer);
			Controls.Add(pnlToolbar);
			Controls.Add(txtStatus);
			Controls.Add(btnOpenLog);
			Controls.Add(btnApplyPrompt);
			Controls.Add(txtPrompt);
			Controls.Add(menuStrip);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MainMenuStrip = menuStrip;
			MaximizeBox = false;
			Name = "MainForm";
			Text = "Créateur de formes - Agent MCP";
			Load += MainForm_Load;
			pnlToolbar.ResumeLayout(false);
			menuStrip.ResumeLayout(false);
			menuStrip.PerformLayout();
			pnlPlayer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)sliderFrame).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private TextBox txtPrompt;
		private Button btnApplyPrompt;
       private Button btnOpenLog;
		private TextBox txtStatus;
		private Panel pnlToolbar;
		private Button btnAddSquare;
      private CheckBox chkDeleteMode;
		private Button btnAddCircle;
		private Button btnAddLosange;
       private Button btnClearAll;
		private Button btnColor;
		private MenuStrip menuStrip;
		private ToolStripMenuItem mnuFile;
		private ToolStripMenuItem mnuSavePreset;
		private ToolStripMenuItem mnuLoadPreset;
		private ToolStripMenuItem mnuEdit;
		private ToolStripMenuItem mnuUndo;
		private Panel pnlPlayer;
		private Button btnPlay;
		private TrackBar sliderFrame;
		private Label lblFrame;
		private ToolTip toolTip;
		private Button btnAddFrame;
		private Button btnDeleteFrame;
		private System.Windows.Forms.Timer playTimer;
	}
}
