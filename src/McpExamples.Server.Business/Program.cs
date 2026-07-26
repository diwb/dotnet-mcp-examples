using McpExamples.Shared;

await StdioServer.RunAsync(McpServerKind.Business, args);

internal static class StdioServer
{
    public static async Task RunAsync(McpServerKind kind, string[] args)
    {
        if (args.Contains("--version", StringComparer.Ordinal))
        {
            await Console.Error.WriteLineAsync($"MCP protocol {McpProtocol.Version}; SDK {McpProtocol.SdkVersion}");
            return;
        }

        Console.Error.WriteLine($"Starting {kind} MCP server. Logs intentionally use stderr.");
        var dispatcher = new McpDispatcher();
        while (await Console.In.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var response = await dispatcher.DispatchAsync(line, kind);
            if (response is not null)
            {
                await Console.Out.WriteLineAsync(response.ToJsonString(McpProtocol.JsonOptions));
                await Console.Out.FlushAsync();
            }
        }
    }
}
