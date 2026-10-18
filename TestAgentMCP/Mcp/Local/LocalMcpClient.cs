using System.Text.Json;

namespace TestAgentMCP
{
	internal sealed class LocalMcpClient(ILocalMcpTransport transport)
	{
		private readonly ILocalMcpTransport _transport = transport;
		private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
		private int _nextId = 1;

		public async Task InitializeAsync(CancellationToken cancellationToken = default)
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

			await SendAsync(payload, cancellationToken);
		}

		public async Task<string> GetCanvasStateAsync(CancellationToken cancellationToken = default)
		{
			var payload = new
			{
				jsonrpc = "2.0",
				id = _nextId++,
				method = "tools/call",
				paramsObj = new
				{
					name = "get_canvas_state",
					arguments = new { }
				}
			};

			using var response = await SendAsync(payload, cancellationToken);
			var content = response.RootElement.GetProperty("result").GetProperty("content");
			if (content.ValueKind != JsonValueKind.Array || content.GetArrayLength() == 0)
			{
				throw new InvalidOperationException("Réponse MCP invalide pour get_canvas_state.");
			}

			return content[0].GetProperty("text").GetString() ?? "{}";
		}

		public async Task ApplySquareUpdatesAsync(AgentMCP.AgentResult result, CancellationToken cancellationToken = default)
		{
			var payload = new
			{
				jsonrpc = "2.0",
				id = _nextId++,
				method = "tools/call",
				paramsObj = new
				{
					name = "apply_square_updates",
					arguments = new
					{
						action = result.Action,
						infos = result.Infos
					}
				}
			};

			await SendAsync(payload, cancellationToken);
		}

		private async Task<JsonDocument> SendAsync(object payload, CancellationToken cancellationToken)
		{
			var requestJson = JsonSerializer.Serialize(payload).Replace("\"paramsObj\"", "\"params\"");
			var responseJson = await _transport.SendAsync(requestJson, cancellationToken);
			var response = JsonDocument.Parse(responseJson);

			if (response.RootElement.TryGetProperty("error", out var errorElement))
			{
				throw new InvalidOperationException($"Erreur MCP: {errorElement.GetProperty("message").GetString()}");
			}

			return response;
		}
	}
}
