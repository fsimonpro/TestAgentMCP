namespace TestAgentMCP
{
	/// <summary>Demande la clé API OpenAI à l'utilisateur (saisie masquée). Contrôles dans le Designer.</summary>
	public partial class ApiKeyDialog : Form
	{
		public ApiKeyDialog()
		{
			InitializeComponent();
			txtKey.TextChanged += (_, _) => btnOk.Enabled = !string.IsNullOrWhiteSpace(txtKey.Text);
		}

		public string ApiKey => txtKey.Text.Trim();
	}
}
