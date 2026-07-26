using McpExamples.Shared;

await StdioServer.RunAsync(McpServerKind.Workspace, args);

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
            try
            {
                var response = await dispatcher.DispatchAsync(line, kind);
                if (response is not null)
                {
                    await Console.Out.WriteLineAsync(response.ToJsonString(McpProtocol.JsonOptions));
                    await Console.Out.FlushAsync();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await Console.Error.WriteLineAsync($"Request failed: {ex.GetType().Name}");
                await Console.Out.WriteLineAsync("""{"jsonrpc":"2.0","id":null,"error":{"code":-32603,"message":"Internal error."}}""");
                await Console.Out.FlushAsync();
            }
        }
    }
}
