using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TestAgentMCP
{
	/// <summary>
	/// Expose un <see cref="LocalMcpServer"/> en MCP "Streamable HTTP" (JSON-RPC sur POST /mcp, réponses JSON),
	/// accessible uniquement depuis la machine locale.
	/// </summary>
	internal sealed class HttpMcpHost : IDisposable
	{
		private static readonly string[] SupportedVersions = ["2025-06-18", "2025-03-26", "2024-11-05"];

		private readonly LocalMcpServer _server;
		private readonly Action<string> _log;
		private readonly HttpListener _listener = new();
		private readonly CancellationTokenSource _cts = new();

		public HttpMcpHost(LocalMcpServer server, int port, Action<string> log)
		{
			_server = server;
			_log = log;
			Port = port;
			_listener.Prefixes.Add($"http://localhost:{port}/");
			_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
		}

		public int Port { get; }

		public string Url => $"http://localhost:{Port}/mcp";

		public void Start()
		{
			_listener.Start();
			_ = Task.Run(AcceptLoopAsync);
		}

		private async Task AcceptLoopAsync()
		{
			while (!_cts.IsCancellationRequested && _listener.IsListening)
			{
				HttpListenerContext context;
				try
				{
					context = await _listener.GetContextAsync();
				}
				catch
				{
					return;
				}

				_ = Task.Run(() => HandleContextAsync(context));
			}
		}

		private async Task HandleContextAsync(HttpListenerContext context)
		{
			var response = context.Response;
			try
			{
				var request = context.Request;

				// Protection contre le DNS rebinding : seules les origines locales sont acceptées.
				var origin = request.Headers["Origin"];
				if (origin is not null && !(Uri.TryCreate(origin, UriKind.Absolute, out var originUri) && originUri.IsLoopback))
				{
					response.StatusCode = 403;
					return;
				}

				if (request.Url?.AbsolutePath.TrimEnd('/') != "/mcp")
				{
					response.StatusCode = 404;
					return;
				}

				if (request.HttpMethod != "POST")
				{
					response.StatusCode = 405;
					response.AddHeader("Allow", "POST");
					return;
				}

				string body;
				using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
				{
					body = await reader.ReadToEndAsync();
				}

				var reply = await ProcessAsync(body);
				if (reply is null)
				{
					response.StatusCode = 202;
					return;
				}

				var bytes = Encoding.UTF8.GetBytes(reply);
				response.StatusCode = 200;
				response.ContentType = "application/json";
				response.ContentLength64 = bytes.Length;
				await response.OutputStream.WriteAsync(bytes);
			}
			catch (Exception ex)
			{
				_log($"HTTP MCP: {ex.Message}");
				try { response.StatusCode = 500; } catch { }
			}
			finally
			{
				try { response.Close(); } catch { }
			}
		}

		private async Task<string?> ProcessAsync(string body)
		{
			JsonNode? node;
			try
			{
				node = JsonNode.Parse(body);
			}
			catch (JsonException)
			{
				return Error(null, -32700, "JSON invalide.");
			}

			if (node is not JsonObject obj || obj["method"]?.GetValue<string>() is not { } method)
			{
				return Error(null, -32600, "Requête JSON-RPC invalide.");
			}

			var id = obj["id"]?.DeepClone();

			// Notifications (pas d'id) : aucune réponse attendue.
			if (id is null)
			{
				return null;
			}

			switch (method)
			{
				case "initialize":
					var requested = obj["params"]?["protocolVersion"]?.GetValue<string>();
					var version = requested is not null && SupportedVersions.Contains(requested) ? requested : SupportedVersions[0];
					return new JsonObject
					{
						["jsonrpc"] = "2.0",
						["id"] = id,
						["result"] = new JsonObject
						{
							["protocolVersion"] = version,
							["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
							["serverInfo"] = new JsonObject { ["name"] = "TestAgentMCP", ["version"] = "1.0.0" },
							["instructions"] = "Canvas de formes (carrés, cercles, losanges). Appelle get_canvas_state pour lire l'état, puis apply_square_updates pour ajouter, modifier ou supprimer des formes."
						}
					}.ToJsonString();
				case "ping":
					return new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = new JsonObject() }.ToJsonString();
				default:
					return await _server.HandleAsync(body);
			}
		}

		private static string Error(JsonNode? id, int code, string message) =>
			new JsonObject
			{
				["jsonrpc"] = "2.0",
				["id"] = id,
				["error"] = new JsonObject { ["code"] = code, ["message"] = message }
			}.ToJsonString();

		public void Dispose()
		{
			_cts.Cancel();
			try { _listener.Close(); } catch { }
			_cts.Dispose();
		}
	}
}
