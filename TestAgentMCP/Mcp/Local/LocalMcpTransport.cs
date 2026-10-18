namespace TestAgentMCP
{
	internal sealed class LocalMcpTransport(McpServer server) : ILocalMcpTransport
	{
		private readonly McpServer _server = server;

		public Task<string> SendAsync(string requestJson, CancellationToken cancellationToken = default)
		{
			return _server.HandleAsync(requestJson, cancellationToken);
		}
	}
}
