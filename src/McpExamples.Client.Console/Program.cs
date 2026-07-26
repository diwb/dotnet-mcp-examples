using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using McpExamples.Shared;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return;
}

switch (args[0])
{
    case "doctor": await Doctor(args); break;
    case "stdio": await Stdio(args); break;
    case "http": await Http(args); break;
    default: PrintHelp(); break;
}

static async Task Doctor(string[] args)
{
    Console.WriteLine($".NET: {Environment.Version}");
    Console.WriteLine($"MCP protocol tested: {McpProtocol.Version}");
    Console.WriteLine($"MCP C# SDK documented: {McpProtocol.SdkVersion}");
    Console.WriteLine($"Repository root: {RepositoryPaths.Root}");
    if (args.Length > 1 && Uri.TryCreate(args[1], UriKind.Absolute, out var endpoint))
    {
        using var client = new HttpClient { BaseAddress = endpoint };
        using var response = await client.GetAsync("/health");
        Console.WriteLine($"HTTP health: {(int)response.StatusCode} {response.ReasonPhrase}");
    }
}

static async Task Stdio(string[] args)
{
    if (args.Length < 3) { Console.WriteLine("Usage: mcp-client stdio <serverPath> <method> [jsonParams]"); return; }
    var start = new ProcessStartInfo(args[1]) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    start.Environment["MCP_EXAMPLES_REPO_ROOT"] = RepositoryPaths.Root;
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Server could not be started.");
    await process.StandardInput.WriteLineAsync(BuildRequest(args[2], args.ElementAtOrDefault(3)));
    process.StandardInput.Close();
    Console.WriteLine(await process.StandardOutput.ReadLineAsync());
    await process.WaitForExitAsync();
}

static async Task Http(string[] args)
{
    if (args.Length < 3) { Console.WriteLine("Usage: mcp-client http <endpoint> <method> [jsonParams] [token]"); return; }
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    client.DefaultRequestHeaders.Add("MCP-Protocol-Version", McpProtocol.Version);
    if (args.Length > 4) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", args[4]);
    using var content = new StringContent(BuildRequest(args[2], args.ElementAtOrDefault(3)), Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(args[1], content);
    Console.WriteLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
}

static string BuildRequest(string method, string? parameters) => $$"""{"jsonrpc":"2.0","id":1,"method":"{{method}}","params":{{(string.IsNullOrWhiteSpace(parameters) ? "{}" : parameters)}}}""";

static void PrintHelp() => Console.WriteLine("""
mcp-client doctor [httpBaseUrl]
mcp-client stdio <serverPath> <method> [jsonParams]
mcp-client http <endpoint> <method> [jsonParams] [token]
""");
