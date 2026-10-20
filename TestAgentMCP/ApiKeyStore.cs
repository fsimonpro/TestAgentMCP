using System.Security.Cryptography;
using System.Text;

namespace TestAgentMCP
{
	/// <summary>Stocke la clé OpenAI chiffrée (DPAPI, compte Windows courant) dans %LocalAppData%\TestAgentMCP.</summary>
	internal static class ApiKeyStore
	{
		private static readonly string FilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestAgentMCP", "openai.key");

		public static string? Load()
		{
			try
			{
				if (!File.Exists(FilePath))
				{
					return null;
				}

				var clear = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser);
				return Encoding.UTF8.GetString(clear);
			}
			catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}

		public static void Save(string apiKey)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllBytes(FilePath, ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), null, DataProtectionScope.CurrentUser));
		}

		public static void Delete()
		{
			try
			{
				File.Delete(FilePath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
	}
}
