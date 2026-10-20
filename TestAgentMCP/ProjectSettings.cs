using System.Reflection;

namespace TestAgentMCP
{
	internal static class ProjectSettings
	{
		private static readonly string ModelFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestAgentMCP", "model.txt");

		/// <summary>Modèle choisi par l'utilisateur au dernier lancement, sinon le défaut du .csproj.</summary>
		public static string OpenAiModel => LoadSavedModel() ?? GetAssemblyMetadata("OpenAiModel") ?? "gpt-4.1";

		public static void SaveModel(string model)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(ModelFilePath)!);
				File.WriteAllText(ModelFilePath, model.Trim());
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}

		private static string? LoadSavedModel()
		{
			try
			{
				var text = File.Exists(ModelFilePath) ? File.ReadAllText(ModelFilePath).Trim() : null;
				return string.IsNullOrEmpty(text) ? null : text;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}

		private static string? GetAssemblyMetadata(string key)
		{
			return typeof(ProjectSettings).Assembly
				.GetCustomAttributes<AssemblyMetadataAttribute>()
				.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal))
				?.Value;
		}
	}
}
