using System.Text.Json;
using McpExamples.Shared;
using ModelContextProtocol.Client;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return;
}

switch (args[0])
{
    case "doctor": await DoctorAsync(args); break;
    case "stdio": await UseStdioAsync(args); break;
    case "http": await UseHttpAsync(args); break;
    default: PrintHelp(); break;
}

static async Task DoctorAsync(string[] args)
{
    Console.WriteLine($".NET: {Environment.Version}");
    Console.WriteLine($"MCP protocol tested: {McpProtocol.Version}");
    Console.WriteLine($"MCP C# SDK: {McpProtocol.SdkVersion}");
    Console.WriteLine($"Repository root: {RepositoryPaths.Root}");
    if (args.Length > 1 && Uri.TryCreate(args[1], UriKind.Absolute, out var endpoint))
    {
        using var http = new HttpClient { BaseAddress = endpoint };
        using var response = await http.GetAsync("/health");
        Console.WriteLine($"HTTP health: {(int)response.StatusCode} {response.ReasonPhrase}");
    }
}

static async Task UseStdioAsync(string[] args)
{
    if (args.Length < 3) { Console.WriteLine("Usage: mcp-client stdio <serverPath> <command> [name] [jsonArgs]"); return; }
    await using var client = await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = "dotnet-mcp-examples-stdio",
        Command = args[1],
        WorkingDirectory = RepositoryPaths.Root,
        EnvironmentVariables = new Dictionary<string, string?> { ["MCP_EXAMPLES_REPO_ROOT"] = RepositoryPaths.Root }
    }));
    await RunCommandAsync(client, args.Skip(2).ToArray());
}

static async Task UseHttpAsync(string[] args)
{
    if (args.Length < 3) { Console.WriteLine("Usage: mcp-client http <endpoint> <command> [name] [jsonArgs]"); return; }
    await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Name = "dotnet-mcp-examples-http",
        Endpoint = new Uri(args[1]),
        TransportMode = HttpTransportMode.StreamableHttp
    }));
    await RunCommandAsync(client, args.Skip(2).ToArray());
}

static async Task RunCommandAsync(McpClient client, string[] command)
{
    switch (command.ElementAtOrDefault(0))
    {
        case "tools":
            Console.WriteLine(JsonSerializer.Serialize(await client.ListToolsAsync(), McpProtocol.JsonOptions));
            break;
        case "call":
            Console.WriteLine(JsonSerializer.Serialize(await client.CallToolAsync(command[1], Args(command.ElementAtOrDefault(2))), McpProtocol.JsonOptions));
            break;
        case "resources":
            Console.WriteLine(JsonSerializer.Serialize(await client.ListResourcesAsync(), McpProtocol.JsonOptions));
            break;
        case "read-resource":
            Console.WriteLine(JsonSerializer.Serialize(await client.ReadResourceAsync(command[1]), McpProtocol.JsonOptions));
            break;
        case "prompts":
            Console.WriteLine(JsonSerializer.Serialize(await client.ListPromptsAsync(), McpProtocol.JsonOptions));
            break;
        case "prompt":
            Console.WriteLine(JsonSerializer.Serialize(await client.GetPromptAsync(command[1], Args(command.ElementAtOrDefault(2))), McpProtocol.JsonOptions));
            break;
        default:
            PrintHelp();
            break;
    }
}

static IReadOnlyDictionary<string, object?> Args(string? json)
{
    if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object?>();
    return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, McpProtocol.JsonOptions) ?? new Dictionary<string, object?>();
}

static void PrintHelp() => Console.WriteLine("""
mcp-client doctor [httpBaseUrl]
mcp-client stdio <serverPath> tools
mcp-client stdio <serverPath> call <toolName> [jsonArgs]
mcp-client stdio <serverPath> resources
mcp-client stdio <serverPath> read-resource <uri>
mcp-client stdio <serverPath> prompts
mcp-client stdio <serverPath> prompt <promptName> [jsonArgs]
mcp-client http <endpoint> tools|call|resources|read-resource|prompts|prompt ...
""");
