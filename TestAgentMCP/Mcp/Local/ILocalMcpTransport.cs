namespace TestAgentMCP
{
	internal interface ILocalMcpTransport
	{
		Task<string> SendAsync(string requestJson, CancellationToken cancellationToken = default);
	}
}
