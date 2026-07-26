using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using McpExamples.Shared;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
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

var app = builder.Build();
var dispatcher = new McpDispatcher();
using var activitySource = new ActivitySource("McpExamples.Remote");
var allowedOrigins = app.Configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? ["http://localhost", "https://localhost"];

app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/mcp" && context.Request.Headers.TryGetValue("Origin", out var origin) && !allowedOrigins.Contains(origin.ToString(), StringComparer.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "origin_not_allowed" });
        return;
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", protocol = McpProtocol.Version }));
app.MapGet("/.well-known/oauth-protected-resource", (HttpRequest request) => Results.Json(new
{
    resource = $"{request.Scheme}://{request.Host}/mcp",
    authorization_servers = new[] { $"{request.Scheme}://{request.Host}/demo-authorization-server" },
    scopes_supported = new[] { "catalog.read", "orders.read", "orders.write" },
    bearer_methods_supported = new[] { "header" }
}));
app.MapGet("/mcp", () => Results.Text(": mcp stream ready\n\n", "text/event-stream"));
app.MapPost("/mcp", async (HttpContext context, CancellationToken cancellationToken) =>
{
    if (!context.Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true || value?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) == true)) return Results.BadRequest(new { error = "accept_header_required" });
    if (context.Request.ContentLength > 64 * 1024) return Results.BadRequest(new { error = "payload_too_large" });

    using var reader = new StreamReader(context.Request.Body);
    var json = await reader.ReadToEndAsync(cancellationToken);
    var requiredScope = RequiredScope(json);
    if (requiredScope is not null && !HasScope(context.Request.Headers.Authorization.ToString(), requiredScope))
    {
        context.Response.Headers.WWWAuthenticate = "Bearer realm=\"mcp-examples\", resource_metadata=\"/.well-known/oauth-protected-resource\"";
        return string.IsNullOrWhiteSpace(context.Request.Headers.Authorization) ? Results.Unauthorized() : Results.Forbid();
    }

    using var activity = activitySource.StartActivity("mcp.request");
    activity?.SetTag("mcp.protocol_version", context.Request.Headers["MCP-Protocol-Version"].ToString());
    activity?.SetTag("mcp.transport", "streamable-http");
    activity?.SetTag("mcp.required_scope", requiredScope ?? "none");
    var response = await dispatcher.DispatchAsync(json, McpServerKind.Business, cancellationToken);
    return response is null ? Results.Accepted() : Results.Json(response, McpProtocol.JsonOptions);
});

app.Run();

static string? RequiredScope(string json)
{
    using var document = JsonDocument.Parse(json);
    if (!document.RootElement.TryGetProperty("method", out var method) || method.GetString() != "tools/call") return null;
    var name = document.RootElement.GetProperty("params").GetProperty("name").GetString();
    return name switch
    {
        "business.get_customer" or "business.list_orders" or "business.get_order" => "orders.read",
        "business.check_availability" or "business.quote_order" => "catalog.read",
        "business.create_demo_order" or "business.cancel_demo_order" => "orders.write",
        _ => null
    };
}

static bool HasScope(string authorization, string requiredScope)
{
    var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization["Bearer ".Length..] : string.Empty;
    var scopes = token switch
    {
        "demo-read" => ["catalog.read", "orders.read"],
        "demo-write" => ["catalog.read", "orders.read", "orders.write"],
        _ => Array.Empty<string>()
    };
    return scopes.Contains(requiredScope, StringComparer.Ordinal);
}



