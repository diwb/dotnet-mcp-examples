using System.Text.Json;
using System.Threading.RateLimiting;
using McpExamples.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<BusinessCatalog>();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(new Uri(builder.Configuration["OAuth:Issuer"] ?? "https://localhost:7001/"));
        options.AddAudiences(McpAuthorizationPolicy.ResourceAudience);
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });
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
        options.ServerInfo = new() { Name = "mcp-examples-remote", Title = "Remote B2B MCP Example", Version = "1.1.0" };
        options.ProtocolVersion = McpProtocol.Version;
    })
    .WithHttpTransport()
    .WithTools<BusinessTools>()
    .WithResources<BusinessResources>()
    .WithPrompts<BusinessPrompts>();

var app = builder.Build();
var allowedOrigins = app.Configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? ["http://localhost", "https://localhost"];

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
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

    if (context.Request.Path.StartsWithSegments("/mcp"))
    {
        var authenticate = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticate.Succeeded || authenticate.Principal is null)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer realm=\"mcp-examples\", error=\"invalid_token\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }

        var requiredScope = await RequiredScopeAsync(context.Request);
        if (requiredScope is not null && !McpAuthorizationPolicy.HasScope(authenticate.Principal, requiredScope))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "insufficient_scope", required_scope = requiredScope });
            return;
        }
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", protocol = McpProtocol.Version, sdk = McpProtocol.SdkVersion }));
app.MapGet("/.well-known/oauth-protected-resource", (HttpRequest request) => Results.Json(new
{
    resource = $"{request.Scheme}://{request.Host}/mcp",
    authorization_servers = new[] { (builder.Configuration["OAuth:Issuer"] ?? "https://localhost:7001/").TrimEnd('/') },
    scopes_supported = McpAuthorizationPolicy.Scopes,
    bearer_methods_supported = new[] { "header" }
}));
app.MapMcp("/mcp");

await app.RunAsync();

static async Task<string?> RequiredScopeAsync(HttpRequest request)
{
    if (!HttpMethods.IsPost(request.Method) || request.ContentLength is null or 0) return null;
    request.EnableBuffering();
    using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: request.HttpContext.RequestAborted);
    request.Body.Position = 0;
    if (!document.RootElement.TryGetProperty("method", out var method) || method.GetString() != "tools/call") return null;
    if (!document.RootElement.TryGetProperty("params", out var parameters) || !parameters.TryGetProperty("name", out var nameElement)) return null;
    return McpAuthorizationPolicy.RequiredScopeForTool(nameElement.GetString());
}

public partial class Program;



