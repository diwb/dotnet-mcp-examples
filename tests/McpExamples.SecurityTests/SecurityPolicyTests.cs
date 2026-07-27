using McpExamples.Shared;

namespace McpExamples.SecurityTests;

public sealed class SecurityPolicyTests
{
    [Theory]
    [InlineData("../secret.md")]
    [InlineData("..\\secret.md")]
    [InlineData("C:\\temp\\secret.md")]
    [InlineData("\\\\server\\share\\secret.md")]
    [InlineData("notes/evil.exe")]
    public void Workspace_rejects_unsafe_paths(string path) => Assert.Null(new WorkspaceCatalog().Resolve(path, mustExist: false));

    [Fact] public void Workspace_update_note_requires_confirmation() => Assert.Throws<InvalidOperationException>(() => new WorkspaceTools(new WorkspaceCatalog()).UpdateNote("audit.md", "x", false, CancellationToken.None));
    [Fact] public void Business_create_order_requires_confirmation() => Assert.Throws<InvalidOperationException>(() => new BusinessCatalog().CreateOrder("CUST-100", "SKU-ALPHA", 1, "abcdefghi", false));
    [Fact] public void Business_create_order_requires_bounded_idempotency_key() => Assert.Throws<InvalidOperationException>(() => new BusinessCatalog().CreateOrder("CUST-100", "SKU-ALPHA", 1, "short", true));
    [Fact] public void Business_cancel_order_requires_confirmation() => Assert.Throws<InvalidOperationException>(() => new BusinessCatalog().CancelOrder("PO-1001", false));
    [Fact] public void Tool_metadata_marks_workspace_update_as_destructive() => Assert.Contains("Destructive = true", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Shared", "McpRuntime.cs")));

    [Fact]
    public void Remote_server_uses_oidc_validation_instead_of_demo_tokens()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs"));
        Assert.Contains("AddOpenIddict", text, StringComparison.Ordinal);
        Assert.Contains("SetIssuer", text, StringComparison.Ordinal);
        Assert.Contains("AddAudiences", text, StringComparison.Ordinal);
        Assert.Contains("UseSystemNetHttp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("demo" + "-read", text, StringComparison.Ordinal);
        Assert.DoesNotContain("demo" + "-write", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Remote_server_returns_401_with_www_authenticate_and_403_for_scope_failures()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs"));
        Assert.Contains("WWWAuthenticate", text, StringComparison.Ordinal);
        Assert.Contains("Status401Unauthorized", text, StringComparison.Ordinal);
        Assert.Contains("Status403Forbidden", text, StringComparison.Ordinal);
        Assert.Contains("insufficient_scope", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Remote_server_uses_origin_validation_and_payload_limit()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs"));
        Assert.Contains("origin_not_allowed", text, StringComparison.Ordinal);
        Assert.Contains("payload_too_large", text, StringComparison.Ordinal);
        Assert.Contains("64 * 1024", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Authorization_server_uses_openiddict_and_public_pkce_client()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.AuthorizationServer", "Program.cs"));
        Assert.Contains("OpenIddict", text, StringComparison.Ordinal);
        Assert.Contains("RequireProofKeyForCodeExchange", text, StringComparison.Ordinal);
        Assert.Contains("CodeChallengeMethods.Sha256", text, StringComparison.Ordinal);
        Assert.Contains("ClientTypes.Public", text, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientSecret", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_uses_pkce_loopback_and_bearer_header_for_http()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Client.Console", "Program.cs"));
        Assert.Contains("code_challenge_method=S256", text, StringComparison.Ordinal);
        Assert.Contains("HttpListener", text, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:37645/callback", text, StringComparison.Ordinal);
        Assert.Contains("MCP_EXAMPLES_ACCESS_TOKEN", text, StringComparison.Ordinal);
        Assert.Contains("Authorization", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Stdio_logs_are_configured_for_stderr() => Assert.Contains("LogToStandardErrorThreshold", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Workspace", "Program.cs")), StringComparison.Ordinal);
}
