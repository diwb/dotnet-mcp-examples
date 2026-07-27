using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var issuer = new Uri(builder.Configuration["OAuth:Issuer"] ?? "https://localhost:7001/");

builder.Services.AddDbContext<AuthorizationDbContext>(options =>
{
    options.UseInMemoryDatabase("mcp-examples-authorization");
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthorizationDbContext>())
    .AddServer(options =>
    {
        options.SetIssuer(issuer)
            .SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange();

        options.RegisterScopes("catalog.read", "orders.read", "orders.write");
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();
        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    });

builder.Services.AddHostedService<AuthorizationSeedHostedService>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", issuer = issuer.ToString() }));
app.MapGet("/.well-known/oauth-protected-resource", (HttpRequest request) => Results.Json(new
{
    resource = builder.Configuration["OAuth:Resource"] ?? "https://mcp-examples.local/mcp",
    authorization_servers = new[] { issuer.ToString().TrimEnd('/') },
    scopes_supported = new[] { "catalog.read", "orders.read", "orders.write" },
    bearer_methods_supported = new[] { "header" }
}));

app.MapMethods("/connect/authorize", ["GET", "POST"], async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenID Connect request not found.");
    if (string.IsNullOrWhiteSpace(request.CodeChallenge) || !string.Equals(request.CodeChallengeMethod, CodeChallengeMethods.Sha256, StringComparison.Ordinal))
    {
        return Results.BadRequest(new { error = Errors.InvalidRequest, error_description = "PKCE S256 is required." });
    }

    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    identity.SetClaim(Claims.Subject, "demo-user");
    identity.SetClaim(Claims.Name, "Demo Local User");
    identity.SetScopes(request.GetScopes());
    identity.SetResources("mcp-examples-remote");
    foreach (var claim in identity.Claims)
    {
        claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
    }

    return Results.SignIn(new ClaimsPrincipal(identity), null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapPost("/connect/token", async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenID Connect request not found.");
    if (!request.IsAuthorizationCodeGrantType())
    {
        return Results.BadRequest(new { error = Errors.UnsupportedGrantType });
    }

    var principal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
    if (principal is null)
    {
        return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme], properties: new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is invalid or expired."
        }));
    }

    principal.SetResources("mcp-examples-remote");
    principal.SetScopes(principal.GetScopes());
    foreach (var claim in principal.Claims)
    {
        claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
    }

    return Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

await app.RunAsync();

public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict();
    }
}

public sealed class AuthorizationSeedHostedService(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthorizationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await manager.FindByClientIdAsync("mcp-examples-public", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "mcp-examples-public",
                DisplayName = "MCP Examples Public Local Client",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                RedirectUris = { new Uri(configuration["OAuth:LoopbackRedirectUri"] ?? "http://127.0.0.1:37645/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Prefixes.Scope + "catalog.read",
                    Permissions.Prefixes.Scope + "orders.read",
                    Permissions.Prefixes.Scope + "orders.write"
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public partial class Program;


