using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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
    case "auth-code": await AuthenticateAsync(args); break;
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

static async Task AuthenticateAsync(string[] args)
{
    if (args.Length < 2) { Console.WriteLine("Usage: mcp-client auth-code <issuer> [space-separated-scopes]"); return; }
    var issuer = args[1].TrimEnd('/');
    var scopes = args.ElementAtOrDefault(2) ?? "catalog.read orders.read orders.write";
    var redirectUri = "http://127.0.0.1:37645/callback";
    var verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
    var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    using var listener = new HttpListener();
    listener.Prefixes.Add("http://127.0.0.1:37645/callback/");
    listener.Start();
    var authorizationUrl = $"{issuer}/connect/authorize?response_type=code&client_id=mcp-examples-public&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}&code_challenge={challenge}&code_challenge_method=S256";
    Console.Error.WriteLine($"Open this URL if a browser does not open automatically: {authorizationUrl}");
    TryOpenBrowser(authorizationUrl);
    var context = await listener.GetContextAsync();
    var code = context.Request.QueryString["code"] ?? throw new InvalidOperationException("Authorization code was not returned.");
    var buffer = Encoding.UTF8.GetBytes("Authentication completed. You can close this window.");
    context.Response.ContentLength64 = buffer.Length;
    await context.Response.OutputStream.WriteAsync(buffer);
    context.Response.Close();

    using var http = new HttpClient();
    using var response = await http.PostAsync($"{issuer}/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["client_id"] = "mcp-examples-public",
        ["redirect_uri"] = redirectUri,
        ["code"] = code,
        ["code_verifier"] = verifier
    }));
    Console.WriteLine(await response.Content.ReadAsStringAsync());
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
    var token = Environment.GetEnvironmentVariable("MCP_EXAMPLES_ACCESS_TOKEN");
    var headers = string.IsNullOrWhiteSpace(token) ? null : new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };
    await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Name = "dotnet-mcp-examples-http",
        Endpoint = new Uri(args[1]),
        TransportMode = HttpTransportMode.StreamableHttp,
        AdditionalHeaders = headers
    }));
    await RunCommandAsync(client, args.Skip(2).ToArray());
}

static async Task RunCommandAsync(McpClient client, string[] command)
{
    switch (command.ElementAtOrDefault(0))
    {
        case "tools": Console.WriteLine(JsonSerializer.Serialize(await client.ListToolsAsync(), McpProtocol.JsonOptions)); break;
        case "call": Console.WriteLine(JsonSerializer.Serialize(await client.CallToolAsync(command[1], Args(command.ElementAtOrDefault(2))), McpProtocol.JsonOptions)); break;
        case "resources": Console.WriteLine(JsonSerializer.Serialize(await client.ListResourcesAsync(), McpProtocol.JsonOptions)); break;
        case "read-resource": Console.WriteLine(JsonSerializer.Serialize(await client.ReadResourceAsync(command[1]), McpProtocol.JsonOptions)); break;
        case "prompts": Console.WriteLine(JsonSerializer.Serialize(await client.ListPromptsAsync(), McpProtocol.JsonOptions)); break;
        case "prompt": Console.WriteLine(JsonSerializer.Serialize(await client.GetPromptAsync(command[1], Args(command.ElementAtOrDefault(2))), McpProtocol.JsonOptions)); break;
        default: PrintHelp(); break;
    }
}

static IReadOnlyDictionary<string, object?> Args(string? json) => string.IsNullOrWhiteSpace(json) ? new Dictionary<string, object?>() : JsonSerializer.Deserialize<Dictionary<string, object?>>(json, McpProtocol.JsonOptions) ?? new Dictionary<string, object?>();
static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
static void TryOpenBrowser(string url) { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } }
static void PrintHelp() => Console.WriteLine("""
mcp-client doctor [httpBaseUrl]
mcp-client auth-code <issuer> [space-separated-scopes]
mcp-client stdio <serverPath> tools
mcp-client stdio <serverPath> call <toolName> [jsonArgs]
mcp-client stdio <serverPath> resources
mcp-client stdio <serverPath> read-resource <uri>
mcp-client stdio <serverPath> prompts
mcp-client stdio <serverPath> prompt <promptName> [jsonArgs]
mcp-client http <endpoint> tools|call|resources|read-resource|prompts|prompt ...
Set MCP_EXAMPLES_ACCESS_TOKEN for authenticated HTTP calls.
""");
