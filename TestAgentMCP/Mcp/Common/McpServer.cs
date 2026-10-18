using System.Text.Json;

namespace TestAgentMCP
{
	internal sealed class McpServer(
		Func<CanvasState> stateProvider,
		Action<AgentMCP.AgentResult> applySquareUpdates,
		Action<AnimationRequest>? setAnimation = null)
	{
		private readonly Func<CanvasState> _stateProvider = stateProvider;
		private readonly Action<AgentMCP.AgentResult> _applySquareUpdates = applySquareUpdates;
		private readonly Action<AnimationRequest>? _setAnimation = setAnimation;
		private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

		public Task<string> HandleAsync(string requestJson, CancellationToken cancellationToken = default)
		{
			using var requestDoc = JsonDocument.Parse(requestJson);
			var root = requestDoc.RootElement;
			var id = root.TryGetProperty("id", out var idElement) ? idElement : default;
			var method = root.GetProperty("method").GetString();

			try
			{
				return Task.FromResult(method switch
				{
					"initialize" => BuildResult(id, new
					{
						protocolVersion = "2024-11-05",
						serverInfo = new { name = "SquareMcpServer", version = "1.0.0" },
						capabilities = new { tools = new { }, resources = new { } },
						instructions = McpDocs.Instructions
					}),
					"tools/list" => BuildResult(id, new { tools = McpToolDefinitions.All }),
					"tools/call" => HandleToolsCall(id, root),
					"resources/list" => BuildResult(id, new
					{
						resources = McpDocs.All.Select(d => new { uri = d.Uri, name = d.Topic, title = d.Title, mimeType = "text/markdown" }).ToArray()
					}),
					"resources/read" => HandleResourcesRead(id, root),
					_ => BuildError(id, -32601, "Méthode MCP inconnue.")
				});
			}
			catch (Exception ex)
			{
				return Task.FromResult(BuildError(id, -32000, ex.Message));
			}
		}

		private string HandleToolsCall(JsonElement id, JsonElement root)
		{
			var @params = root.GetProperty("params");
			var name = @params.GetProperty("name").GetString();

			if (string.Equals(name, "get_canvas_state", StringComparison.OrdinalIgnoreCase))
			{
				return BuildTextResult(id, JsonSerializer.Serialize(_stateProvider(), _jsonOptions));
			}

			if (string.Equals(name, "get_help", StringComparison.OrdinalIgnoreCase))
			{
				var args = @params.TryGetProperty("arguments", out var a) ? a : default;
				var topic = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("topic", out var t) ? t.GetString() : null;
				var text = string.IsNullOrWhiteSpace(topic)
					? string.Join("\n\n", McpDocs.All.Select(d => d.Text))
					: McpDocs.Find(topic)?.Text ?? $"Topic inconnu. Topics disponibles: {McpDocs.TopicList}.";
				return BuildTextResult(id, text);
			}

			if (string.Equals(name, "apply_square_updates", StringComparison.OrdinalIgnoreCase))
			{
				var args = @params.GetProperty("arguments");
				var action = args.TryGetProperty("action", out var actionElement) ? actionElement.GetString() : null;
				var infos = args.TryGetProperty("infos", out var infosElement)
					? JsonSerializer.Deserialize<List<AgentMCP.SquareUpdate>>(infosElement.GetRawText(), _jsonOptions) ?? []
					: [];

				_applySquareUpdates(new AgentMCP.AgentResult
				{
					Action = action,
					Infos = infos,
					Message = "Mise à jour appliquée via MCP"
				});

				return BuildTextResult(id, JsonSerializer.Serialize(_stateProvider(), _jsonOptions));
			}

			if (string.Equals(name, "set_animation", StringComparison.OrdinalIgnoreCase))
			{
				if (_setAnimation is null)
				{
					return BuildError(id, -32602, "set_animation indisponible sur ce transport.");
				}

				var request = JsonSerializer.Deserialize<AnimationRequest>(@params.GetProperty("arguments").GetRawText(), _jsonOptions) ?? new AnimationRequest();
				_setAnimation(request);

				return BuildTextResult(id, JsonSerializer.Serialize(_stateProvider(), _jsonOptions));
			}

			return BuildError(id, -32602, "Tool MCP inconnu.");
		}

		private static string HandleResourcesRead(JsonElement id, JsonElement root)
		{
			var uri = root.GetProperty("params").GetProperty("uri").GetString();
			var doc = McpDocs.All.FirstOrDefault(d => d.Uri == uri);
			return doc is null
				? BuildError(id, -32002, "Resource inconnue.")
				: BuildResult(id, new { contents = new object[] { new { uri = doc.Uri, mimeType = "text/markdown", text = doc.Text } } });
		}

		private static string BuildTextResult(JsonElement id, string text)
		{
			return BuildResult(id, new { content = new object[] { new { type = "text", text } } });
		}

		private static string BuildResult(JsonElement id, object result)
		{
			return JsonSerializer.Serialize(new
			{
				jsonrpc = "2.0",
				id = id.ValueKind == JsonValueKind.Undefined ? 0 : JsonSerializer.Deserialize<object>(id.GetRawText()),
				result
			});
		}

		private static string BuildError(JsonElement id, int code, string message)
		{
			return JsonSerializer.Serialize(new
			{
				jsonrpc = "2.0",
				id = id.ValueKind == JsonValueKind.Undefined ? 0 : JsonSerializer.Deserialize<object>(id.GetRawText()),
				error = new { code, message }
			});
		}
	}
}
