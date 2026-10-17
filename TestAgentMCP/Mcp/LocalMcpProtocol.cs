using System.Text.Json;

namespace TestAgentMCP
{
	internal interface IMcpTransport
	{
		Task<string> SendAsync(string requestJson, CancellationToken cancellationToken = default);
	}

	internal sealed class InMemoryMcpTransport(LocalMcpServer server) : IMcpTransport
	{
		private readonly LocalMcpServer _server = server;

		public Task<string> SendAsync(string requestJson, CancellationToken cancellationToken = default)
		{
			return _server.HandleAsync(requestJson, cancellationToken);
		}
	}

	internal sealed class LocalMcpClient(IMcpTransport transport)
	{
		private readonly IMcpTransport _transport = transport;
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

	internal sealed class LocalMcpServer(Func<LocalMcpServer.CanvasState> stateProvider, Action<AgentMCP.AgentResult> applySquareUpdates)
	{
		private readonly Func<CanvasState> _stateProvider = stateProvider;
		private readonly Action<AgentMCP.AgentResult> _applySquareUpdates = applySquareUpdates;
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
						capabilities = new { tools = new { } }
					}),
					"tools/list" => BuildResult(id, new
					{
						tools = new object[]
						{
							new
							{
								name = "get_canvas_state",
								description = "Retourne l'état actuel des carrés et des limites d'affichage.",
								inputSchema = new { type = "object", properties = new { } }
							},
							new
							{
								name = "apply_square_updates",
								description = "Ajoute, modifie ou supprime des formes sur le canvas, puis retourne le nouvel état. " +
									"Add: crée de nouvelles formes (id ignoré, attribué automatiquement ; position aléatoire si x/y omis). " +
									"Update: modifie les formes existantes par id (seuls les champs fournis changent). " +
									"Remove: supprime les formes dont l'id est donné.",
								inputSchema = new
								{
									type = "object",
									properties = new
									{
										action = new { type = "string", @enum = new[] { "Add", "Update", "Remove" } },
										infos = new
										{
											type = "array",
											items = new
											{
												type = "object",
												properties = new
												{
													id = new { type = "integer", description = "Id de la forme (Update/Remove)." },
													x = new { type = "integer" },
													y = new { type = "integer" },
													size = new { type = "integer", description = "Côté/diamètre en pixels (défaut 60)." },
													color = new { type = "string", description = "Couleur HTML, ex. #FF0000 ou Red." },
													shape = new { type = "string", @enum = new[] { "Square", "Circle", "Losange" } }
												}
											}
										}
									},
									required = new[] { "action", "infos" }
								}
							}
						}
					}),
					"tools/call" => HandleToolsCall(id, root),
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
				var stateJson = JsonSerializer.Serialize(_stateProvider(), _jsonOptions);
				return BuildResult(id, new
				{
					content = new object[] { new { type = "text", text = stateJson } }
				});
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

				return BuildResult(id, new
				{
					content = new object[] { new { type = "text", text = JsonSerializer.Serialize(_stateProvider(), _jsonOptions) } }
				});
			}

			return BuildError(id, -32602, "Tool MCP inconnu.");
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

		internal sealed class CanvasState
		{
			public int ClientWidth { get; set; }
			public int ClientHeight { get; set; }
			public int DrawingLeft { get; set; }
			public int DrawingTop { get; set; }
			public int DrawingWidth { get; set; }
			public int DrawingHeight { get; set; }
			public List<CanvasSquare> Squares { get; set; } = [];
		}

		internal sealed class CanvasSquare
		{
			public int Id { get; set; }
			public int X { get; set; }
			public int Y { get; set; }
			public int Size { get; set; }
			public string Color { get; set; } = "#808080";
           public string Shape { get; set; } = "Square";
		}
	}
}
