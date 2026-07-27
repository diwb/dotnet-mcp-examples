using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using McpExamples.Shared;

namespace McpExamples.SecurityTests;

public sealed class AuthorizationServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationServerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Discovery_exposes_authorization_code_endpoints_and_scopes()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://localhost:7001/", document.RootElement.GetProperty("issuer").GetString());
        Assert.Contains("/connect/authorize", document.RootElement.GetProperty("authorization_endpoint").GetString());
        Assert.Contains("/connect/token", document.RootElement.GetProperty("token_endpoint").GetString());
        var scopes = document.RootElement.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("catalog.read", scopes);
        Assert.Contains("orders.read", scopes);
        Assert.Contains("orders.write", scopes);
    }

    [Fact]
    public async Task Protected_resource_metadata_exposes_local_resource_and_bearer_method()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/.well-known/oauth-protected-resource");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("mcp", document.RootElement.GetProperty("resource").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://localhost:7001", document.RootElement.GetProperty("authorization_servers")[0].GetString());
        Assert.Equal("header", document.RootElement.GetProperty("bearer_methods_supported")[0].GetString());
    }

    [Fact]
    public async Task Authorization_code_flow_requires_pkce_s256()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        var url = BuildAuthorizeUrl("orders.read", codeChallenge: null, method: null);
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_request", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authorization_code_flow_rejects_plain_pkce_method()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        using var response = await client.GetAsync(BuildAuthorizeUrl("orders.read", "plain-challenge", "plain"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authorization_code_flow_rejects_unregistered_redirect_uri()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        using var response = await client.GetAsync(BuildAuthorizeUrl("orders.read", CodeChallenge("verifier-verifier-verifier"), "S256", redirectUri: "http://127.0.0.1:49999/callback"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_request_rejects_wrong_code_verifier()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        var code = await AuthorizeAsync(client, "orders.read", "correct-verifier-correct-verifier");
        using var response = await RedeemAsync(client, code, "wrong-verifier-wrong-verifier");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Token_request_issues_access_token_for_valid_scope_and_resource()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        var verifier = "valid-verifier-valid-verifier-valid-verifier";
        var code = await AuthorizeAsync(client, "catalog.read orders.read", verifier);
        using var response = await RedeemAsync(client, code, verifier);
        response.EnsureSuccessStatusCode();
        using var tokenDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        var payload = DecodeJwtPayload(accessToken!);
        Assert.Equal("https://localhost:7001/", payload.GetProperty("iss").GetString());
        Assert.Contains(McpAuthorizationPolicy.ResourceAudience, Audiences(payload.GetProperty("aud")));
        Assert.Contains("catalog.read", payload.GetProperty("scope").GetString());
        Assert.Contains("orders.read", payload.GetProperty("scope").GetString());
        Assert.True(DateTimeOffset.FromUnixTimeSeconds(payload.GetProperty("exp").GetInt64()) > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Unknown_scope_is_rejected_before_token_issuance()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        using var response = await client.GetAsync(BuildAuthorizeUrl("admin.write", CodeChallenge("scope-verifier-scope-verifier"), "S256"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient(bool allowAutoRedirect = true) => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect, BaseAddress = new Uri("https://localhost:7001") });

    private static async Task<string> AuthorizeAsync(HttpClient client, string scope, string verifier)
    {
        using var response = await client.GetAsync(BuildAuthorizeUrl(scope, CodeChallenge(verifier), "S256"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location ?? throw new InvalidOperationException("Authorization response did not redirect.");
        var query = QueryHelpers.ParseQuery(location.Query);
        return query["code"].ToString();
    }

    private static Task<HttpResponseMessage> RedeemAsync(HttpClient client, string code, string verifier) => client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["client_id"] = "mcp-examples-public",
        ["redirect_uri"] = "http://127.0.0.1:37645/callback",
        ["code"] = code,
        ["code_verifier"] = verifier
    }));

    private static string BuildAuthorizeUrl(string scope, string? codeChallenge, string? method, string redirectUri = "http://127.0.0.1:37645/callback")
    {
        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = "mcp-examples-public",
            ["redirect_uri"] = redirectUri,
            ["scope"] = scope
        };
        if (codeChallenge is not null) query["code_challenge"] = codeChallenge;
        if (method is not null) query["code_challenge_method"] = method;
        return QueryHelpers.AddQueryString("/connect/authorize", query);
    }

    private static string CodeChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static IEnumerable<string?> Audiences(JsonElement audience) => audience.ValueKind == JsonValueKind.Array ? audience.EnumerateArray().Select(x => x.GetString()) : [audience.GetString()];

    private static JsonElement DecodeJwtPayload(string token)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
        return document.RootElement.Clone();
    }
}



