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
			txtPrompt = new TextBox();
			btnApplyPrompt = new Button();
			btnOpenLog = new Button();
			pnlToolbar = new Panel();
			btnAddLosange = new Button();
			btnAddCircle = new Button();
            chkDeleteMode = new CheckBox();
			btnClearAll = new Button();
			btnAddSquare = new Button();
			txtStatus = new TextBox();
			pnlToolbar.SuspendLayout();
			SuspendLayout();
			// 
			// txtPrompt
			// 
			txtPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtPrompt.Location = new Point(12, 12);
			txtPrompt.Name = "txtPrompt";
			txtPrompt.PlaceholderText = "Ex: mets le carré rouge en bas à droite, et le bleu en jaune";
			txtPrompt.Size = new Size(796, 27);
			txtPrompt.TabIndex = 0;
			// 
			// btnApplyPrompt
			// 
			btnApplyPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
           btnApplyPrompt.Location = new Point(814, 10);
			btnApplyPrompt.Name = "btnApplyPrompt";
			btnApplyPrompt.Size = new Size(75, 34);
			btnApplyPrompt.TabIndex = 1;
			btnApplyPrompt.Text = "Envoyer";
			btnApplyPrompt.UseVisualStyleBackColor = true;
			// 
			// btnOpenLog
			// 
			btnOpenLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
           btnOpenLog.Location = new Point(895, 10);
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
			pnlToolbar.Controls.Add(btnAddLosange);
			pnlToolbar.Controls.Add(btnAddCircle);
			pnlToolbar.Controls.Add(chkDeleteMode);
			pnlToolbar.Controls.Add(btnAddSquare);
			pnlToolbar.Dock = DockStyle.Right;
			pnlToolbar.ForeColor = Color.Black;
            pnlToolbar.Location = new Point(952, 0);
			pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new Size(72, 768);
			pnlToolbar.TabIndex = 4;
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
			// btnClearAll
			// 
			btnClearAll.Location = new Point(8, 172);
			btnClearAll.Name = "btnClearAll";
			btnClearAll.Size = new Size(56, 34);
			btnClearAll.TabIndex = 4;
			btnClearAll.Text = "Clear";
			btnClearAll.UseVisualStyleBackColor = true;
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
			txtStatus.Location = new Point(12, 54);
			txtStatus.Multiline = true;
			txtStatus.Name = "txtStatus";
			txtStatus.ReadOnly = true;
			txtStatus.ScrollBars = ScrollBars.Vertical;
            txtStatus.Size = new Size(932, 84);
			txtStatus.TabIndex = 3;
			// 
			// MainForm
			// 
			AcceptButton = btnApplyPrompt;
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1024, 768);
			Controls.Add(pnlToolbar);
			Controls.Add(txtStatus);
			Controls.Add(btnOpenLog);
			Controls.Add(btnApplyPrompt);
			Controls.Add(txtPrompt);
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Name = "MainForm";
			Text = "Créateur de carrés - Agent MCP";
			pnlToolbar.ResumeLayout(false);
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
	}
}
