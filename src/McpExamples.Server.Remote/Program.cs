using System.Threading.RateLimiting;
using McpExamples.Shared;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<BusinessCatalog>();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter("mcp", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "mcp-examples-remote", Title = "Remote B2B MCP Example", Version = "1.0.1" };
        options.ProtocolVersion = McpProtocol.Version;
    })
    .WithHttpTransport()
    .WithTools<BusinessTools>()
    .WithResources<BusinessResources>()
    .WithPrompts<BusinessPrompts>();

var app = builder.Build();
var allowedOrigins = app.Configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? ["http://localhost", "https://localhost"];

app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp") && context.Request.Headers.TryGetValue("Origin", out var origin) && !allowedOrigins.Contains(origin.ToString(), StringComparer.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "origin_not_allowed" });
        return;
    }

    if (context.Request.Path.StartsWithSegments("/mcp") && context.Request.ContentLength > 64 * 1024)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new { error = "payload_too_large" });
        return;
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", protocol = McpProtocol.Version, sdk = McpProtocol.SdkVersion }));
app.MapMcp("/mcp");

await app.RunAsync();
