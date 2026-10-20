using System.Text.Json;

namespace TestAgentMCP
{
	/// <summary>Client MCP en mémoire : mêmes méthodes (initialize, tools/list, tools/call) qu'un client externe.</summary>
	internal sealed class LocalMcpClient(ILocalMcpTransport transport)
	{
		private readonly ILocalMcpTransport _transport = transport;
		private int _nextId = 1;

		/// <summary>Initialise la session et retourne les `instructions` du serveur.</summary>
		public async Task<string> InitializeAsync(CancellationToken cancellationToken = default)
		{
			var payload = new
			{
				jsonrpc = "2.0",
				id = _nextId++,
				method = "initialize",
				paramsObj = new
				{
					protocolVersion = "2024-11-05",
					clientInfo = new { name = "TestAgentMCP", version = "1.0.0" }
				}
			};

			using var response = await SendAsync(payload, cancellationToken);
			var result = response.RootElement.GetProperty("result");
			return result.TryGetProperty("instructions", out var instructions) ? instructions.GetString() ?? string.Empty : string.Empty;
		}

		/// <summary>Retourne le tableau JSON `tools` de tools/list (name, description, inputSchema).</summary>
		public async Task<JsonElement> ListToolsAsync(CancellationToken cancellationToken = default)
		{
			var payload = new { jsonrpc = "2.0", id = _nextId++, method = "tools/list" };
			using var response = await SendAsync(payload, cancellationToken);
			return response.RootElement.GetProperty("result").GetProperty("tools").Clone();
		}

		/// <summary>Appelle un outil et retourne son texte. Une erreur MCP est renvoyée en texte pour que le modèle puisse corriger.</summary>
		public async Task<string> CallToolAsync(string name, string argumentsJson, CancellationToken cancellationToken = default)
		{
			using var arguments = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
			var payload = new
			{
				jsonrpc = "2.0",
				id = _nextId++,
				method = "tools/call",
				paramsObj = new { name, arguments = arguments.RootElement }
			};

			try
			{
				using var response = await SendAsync(payload, cancellationToken);
				var content = response.RootElement.GetProperty("result").GetProperty("content");
				return content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0
					? content[0].GetProperty("text").GetString() ?? string.Empty
					: string.Empty;
			}
			catch (InvalidOperationException ex)
			{
				return ex.Message;
			}
		}

		private async Task<JsonDocument> SendAsync(object payload, CancellationToken cancellationToken)
		{
			var requestJson = JsonSerializer.Serialize(payload).Replace("\"paramsObj\"", "\"params\"");
			var responseJson = await _transport.SendAsync(requestJson, cancellationToken);
			var response = JsonDocument.Parse(responseJson);

			if (response.RootElement.TryGetProperty("error", out var errorElement))
			{
				var message = errorElement.GetProperty("message").GetString();
				response.Dispose();
				throw new InvalidOperationException($"Erreur MCP: {message}");
			}

			return response;
		}
	}
}
