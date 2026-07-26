using McpExamples.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton<BusinessCatalog>();
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "mcp-examples-business", Title = "B2B Orders MCP Example", Version = "1.0.1" };
        options.ProtocolVersion = McpProtocol.Version;
    })
    .WithStdioServerTransport()
    .WithTools<BusinessTools>()
    .WithResources<BusinessResources>()
    .WithPrompts<BusinessPrompts>();

await builder.Build().RunAsync();
