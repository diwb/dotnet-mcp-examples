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
    [Fact] public void Remote_server_no_longer_contains_demo_tokens() { var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs")); Assert.DoesNotContain("demo" + "-read", text, StringComparison.Ordinal); Assert.DoesNotContain("demo" + "-write", text, StringComparison.Ordinal); }
    [Fact] public void Remote_server_uses_origin_validation() => Assert.Contains("origin_not_allowed", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Remote_server_uses_payload_limit() => Assert.Contains("payload_too_large", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Stdio_logs_are_configured_for_stderr() => Assert.Contains("LogToStandardErrorThreshold", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Workspace", "Program.cs")), StringComparison.Ordinal);
}

