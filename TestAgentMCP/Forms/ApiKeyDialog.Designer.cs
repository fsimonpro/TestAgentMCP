namespace TestAgentMCP
{
	partial class ApiKeyDialog
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			lblInfo = new Label();
			txtKey = new TextBox();
			btnOk = new Button();
			btnCancel = new Button();
			SuspendLayout();
			// 
			// lblInfo
			// 
			lblInfo.Location = new Point(12, 12);
			lblInfo.Name = "lblInfo";
			lblInfo.Size = new Size(456, 44);
			lblInfo.TabIndex = 0;
			lblInfo.Text = "Clé API OpenAI requise pour utiliser le prompt. Elle sera stockée chiffrée sur ce PC (compte Windows courant).";
			// 
			// txtKey
			// 
			txtKey.Location = new Point(12, 62);
			txtKey.Name = "txtKey";
			txtKey.Size = new Size(456, 27);
			txtKey.TabIndex = 1;
			txtKey.UseSystemPasswordChar = true;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Enabled = false;
			btnOk.Location = new Point(262, 104);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 32);
			btnOk.TabIndex = 2;
			btnOk.Text = "OK";
			btnOk.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Location = new Point(368, 104);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 32);
			btnCancel.TabIndex = 3;
			btnCancel.Text = "Annuler";
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// ApiKeyDialog
			// 
			AcceptButton = btnOk;
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnCancel;
			ClientSize = new Size(480, 148);
			Controls.Add(lblInfo);
			Controls.Add(txtKey);
			Controls.Add(btnOk);
			Controls.Add(btnCancel);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "ApiKeyDialog";
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Clé API OpenAI";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label lblInfo;
		private TextBox txtKey;
		private Button btnOk;
		private Button btnCancel;
	}
}
