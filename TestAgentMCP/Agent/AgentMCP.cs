using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TestAgentMCP
{
	internal sealed class AgentMCP
	{
		private const string OpenAiEndpoint = "https://api.openai.com/v1/responses";
		private readonly HttpClient _httpClient;
		private string? _apiKey;
		private string _model;
		private readonly LocalMcpClient _mcpClient;
		private readonly Action<string>? _logger;
		private string? _lastResponseId;
		private string? _instructions;
		private List<Dictionary<string, object?>>? _tools;
		private const int MaxToolSteps = 12;

		private const string AgentGuidance = """
			Tu pilotes le canvas uniquement avec ces outils.
			Méthode pour toute demande de dessin (ex. « dessine un chat ») :
			1. Appelle get_canvas_state (zone de dessin, formes existantes) et get_help("examples") si tu n'as pas d'exemple en tête.
			2. Planifie AVANT d'agir : décompose le sujet en 10 à 25 formes simples (tête, oreilles, yeux, museau, corps, pattes, queue…), choisis pour chacune la forme (Square/Circle/Losange), la couleur, la taille et des coordonnées précises à l'intérieur de la zone de dessin. Les parties doivent se toucher ou se chevaucher : ne les laisse pas flotter.
			3. Ajoute toutes les formes en UN SEUL appel apply_square_updates (action Add), du fond vers l'avant (corps d'abord, détails ensuite).
			4. Relis le résultat avec get_canvas_state, vérifie les positions et corrige avec Update si besoin.
			5. Pour une animation, utilise set_animation en ne listant que ce qui change.
			Quand c'est terminé, réponds par une courte phrase en français.
			""";

		public AgentMCP(string? apiKey, LocalMcpClient mcpClient, Action<string>? logger = null, HttpClient? httpClient = null)
		{
			_apiKey = apiKey;
			_httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
			_mcpClient = mcpClient;
			_logger = logger;
			_model = ProjectSettings.OpenAiModel;
		}

		public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

		public void SetApiKey(string? apiKey) => _apiKey = apiKey?.Trim();

		public void SetModel(string model) => _model = model.Trim();

		/// <summary>Appelé à chaque outil MCP invoqué par le modèle (nom + arguments), pour l'afficher à l'utilisateur.</summary>
		public Action<string>? ToolCalled { get; set; }

		/// <summary>
		/// Boucle agent : le modèle reçoit les mêmes `instructions` et les mêmes outils MCP qu'un client externe
		/// (tools/list), les appelle lui-même via le client MCP local, puis conclut par un court message.
		/// </summary>
		public async Task<string> RunAsync(string userPrompt, CancellationToken cancellationToken = default)
		{
			if (!IsConfigured)
			{
				throw new InvalidOperationException("La clé API OpenAI est manquante.");
			}

			if (_tools is null)
			{
				_instructions = await _mcpClient.InitializeAsync(cancellationToken);
				_tools = (await _mcpClient.ListToolsAsync(cancellationToken)).EnumerateArray()
					.Select(tool => new Dictionary<string, object?>
					{
						["type"] = "function",
						["name"] = tool.GetProperty("name").GetString(),
						["description"] = tool.GetProperty("description").GetString(),
						["parameters"] = tool.GetProperty("inputSchema")
					})
					.ToList();
			}

			_logger?.Invoke($"Prompt envoyé au modèle: {userPrompt}");

			object input = userPrompt;
			for (var step = 0; step < MaxToolSteps; step++)
			{
				var requestPayload = new Dictionary<string, object?>
				{
					["model"] = _model,
					["instructions"] = $"{_instructions}\n\n{AgentGuidance}",
					["tools"] = _tools,
					["input"] = input
				};

				// Les modèles à raisonnement (gpt-5, o*) sont très lents par défaut : effort réduit.
				if (_model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) || _model.StartsWith("o", StringComparison.OrdinalIgnoreCase))
				{
					requestPayload["reasoning"] = new Dictionary<string, object?> { ["effort"] = "low" };
				}

				if (!string.IsNullOrWhiteSpace(_lastResponseId))
				{
					requestPayload["previous_response_id"] = _lastResponseId;
				}

				using var request = new HttpRequestMessage(HttpMethod.Post, OpenAiEndpoint);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
				request.Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

				using var response = await _httpClient.SendAsync(request, cancellationToken);
				var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					throw new InvalidOperationException($"Erreur OpenAI: {(int)response.StatusCode} - {responseText}");
				}

				using var responseDoc = JsonDocument.Parse(responseText);
				var root = responseDoc.RootElement;
				if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
				{
					_lastResponseId = idElement.GetString();
				}

				var outputs = new List<object>();
				if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in output.EnumerateArray())
					{
						if (!item.TryGetProperty("type", out var type) || type.GetString() != "function_call")
						{
							continue;
						}

						var name = item.GetProperty("name").GetString() ?? string.Empty;
						var arguments = item.TryGetProperty("arguments", out var args) ? args.GetString() ?? "{}" : "{}";
						ToolCalled?.Invoke($"{name} {arguments}");
						var toolResult = await _mcpClient.CallToolAsync(name, arguments, cancellationToken);
						outputs.Add(new Dictionary<string, object?>
						{
							["type"] = "function_call_output",
							["call_id"] = item.GetProperty("call_id").GetString(),
							["output"] = toolResult
						});
					}
				}

				if (outputs.Count == 0)
				{
					return ExtractResponseText(root)?.Trim() ?? "Action appliquée.";
				}

				input = outputs;
			}

			throw new InvalidOperationException($"Trop d'appels d'outils ({MaxToolSteps}) sans réponse finale.");
		}

		public void ResetThread()
		{
			_lastResponseId = null;
		}

		private static string? ExtractResponseText(JsonElement root)
		{
			if (root.TryGetProperty("output_text", out var outputText))
			{
				if (outputText.ValueKind == JsonValueKind.String)
				{
					return outputText.GetString();
				}

				if (outputText.ValueKind == JsonValueKind.Array)
				{
					var chunks = outputText.EnumerateArray()
						.Where(x => x.ValueKind == JsonValueKind.String)
						.Select(x => x.GetString())
						.Where(x => !string.IsNullOrWhiteSpace(x));

					var merged = string.Join("\n", chunks!);
					if (!string.IsNullOrWhiteSpace(merged))
					{
						return merged;
					}
				}
			}

			if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
			{
				return null;
			}

			foreach (var item in output.EnumerateArray())
			{
				if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
				{
					continue;
				}

				foreach (var block in content.EnumerateArray())
				{
					if (!block.TryGetProperty("text", out var textElement))
					{
						continue;
					}

					if (textElement.ValueKind == JsonValueKind.String)
					{
						var asString = textElement.GetString();
						if (!string.IsNullOrWhiteSpace(asString))
						{
							return asString;
						}
					}

					if (textElement.ValueKind == JsonValueKind.Object && textElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
					{
						var value = valueElement.GetString();
						if (!string.IsNullOrWhiteSpace(value))
						{
							return value;
						}
					}
				}
			}

			return null;
		}

		internal sealed class AgentResult
		{
			public string? Action { get; set; }
			public string? Message { get; set; }
			public List<SquareUpdate> Infos { get; set; } = [];
		}

		internal sealed class SquareUpdate
		{
			public int Id { get; set; }
			public int? X { get; set; }
			public int? Y { get; set; }

			public int? Size { get; set; }
			public string? Color { get; set; }
           public string? Shape { get; set; }
		}
	}
}
