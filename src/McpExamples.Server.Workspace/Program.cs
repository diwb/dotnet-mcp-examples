using McpExamples.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton<WorkspaceCatalog>();
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "mcp-examples-workspace", Title = "Secure Workspace MCP Example", Version = "1.0.1" };
        options.ProtocolVersion = McpProtocol.Version;
    })
    .WithStdioServerTransport()
    .WithTools<WorkspaceTools>()
    .WithResources<WorkspaceResources>()
    .WithPrompts<WorkspacePrompts>();

await builder.Build().RunAsync();
