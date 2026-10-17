using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TestAgentMCP
{
	internal sealed class AgentMCP
	{
		private const string OpenAiEndpoint = "https://api.openai.com/v1/responses";
		private readonly HttpClient _httpClient;
		private readonly string? _apiKey;
		private readonly string _model;
		private readonly LocalMcpClient _mcpClient;
		private readonly Action<string>? _logger;
		private string? _lastResponseId;
		private bool _mcpInitialized;

		public AgentMCP(string? apiKey, LocalMcpClient mcpClient, Action<string>? logger = null, HttpClient? httpClient = null)
		{
			_apiKey = apiKey;
		//	_apiKey = "";
			_httpClient = httpClient ?? new HttpClient();
			_mcpClient = mcpClient;
			_logger = logger;
			_model = ProjectSettings.OpenAiModel;
		}

		public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

		public async Task<AgentResult> GetActionAsync(string userPrompt, CancellationToken cancellationToken = default)
		{
			if (!IsConfigured)
			{
				throw new InvalidOperationException("La clé API OpenAI est manquante.");
			}

			if (!_mcpInitialized)
			{
				await _mcpClient.InitializeAsync(cancellationToken);
				_mcpInitialized = true;
			}

			var stateJson = await _mcpClient.GetCanvasStateAsync(cancellationToken);
			var inputText = $"Réponds en json strict uniquement. Etat actuel: {stateJson}\\nPrompt utilisateur: {userPrompt}";
			_logger?.Invoke($"Prompt envoyé au modèle: {inputText}");

			var requestPayload = new Dictionary<string, object?>
			{
				["model"] = _model,
				["temperature"] = 0.1,
                ["instructions"] = "Tu es un agent qui pilote des formes dans un formulaire WinForms. Réponds STRICTEMENT en JSON avec: action ('remove', 'add' ou 'update'), message (string), infos (tableau). Chaque info contient: id (int), x (int optionnel), y (int optionnel), size (int optionnel), color (string optionnel, format '#RRGGBB' ou nom de couleur), shape (string optionnel: 'Square', 'Circle', 'Losange'). N'envoie que les changements.",
				["text"] = new { format = new { type = "json_object" } },
				["input"] = inputText
			};

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

			var content = ExtractResponseText(root);

			if (string.IsNullOrWhiteSpace(content))
			{
				throw new InvalidOperationException("Réponse OpenAI vide.");
			}

			var result = JsonSerializer.Deserialize<AgentResult>(ExtractJsonObject(content), new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (result is null)
			{
				throw new InvalidOperationException("Impossible de lire la réponse de l'agent.");
			}

			await _mcpClient.ApplySquareUpdatesAsync(result, cancellationToken);

			return result;
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

		private static string ExtractJsonObject(string content)
		{
			var start = content.IndexOf('{');
			var end = content.LastIndexOf('}');

			if (start < 0 || end < 0 || end <= start)
			{
				throw new InvalidOperationException("Réponse JSON introuvable dans la sortie du modèle.");
			}

			return content[start..(end + 1)];
		}

        internal sealed record SquareInfo(int Id, int X, int Y, int Size, string Color, string Shape);

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
