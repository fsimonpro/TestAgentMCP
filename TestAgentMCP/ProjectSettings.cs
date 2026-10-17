using System.Reflection;

namespace TestAgentMCP
{
	internal static class ProjectSettings
	{
		public static string? OpenAiApiKey => GetAssemblyMetadata("OpenAiApiKey");
		public static string OpenAiModel => GetAssemblyMetadata("OpenAiModel") ?? "gpt-4o-mini";

		private static string? GetAssemblyMetadata(string key)
		{
			return typeof(ProjectSettings).Assembly
				.GetCustomAttributes<AssemblyMetadataAttribute>()
				.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal))
				?.Value;
		}
	}
}
