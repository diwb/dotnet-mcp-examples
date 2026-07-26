using System.Text.Json;
using McpExamples.Shared;

namespace McpExamples.SecurityTests;

public sealed class SecurityPolicyTests
{
    [Fact]
    public void Workspace_update_note_requires_confirmation()
    {
        var catalog = new WorkspaceCatalog(RepositoryPaths.WorkspaceRoot);
        using var args = JsonDocument.Parse("""{"name":"audit.md","content":"x","confirm":false}""");
        var result = catalog.Call("workspace.update_note", args.RootElement, CancellationToken.None);
        Assert.True(result["isError"]!.GetValue<bool>());
    }

    [Fact]
    public void Business_create_order_requires_confirmation()
    {
        var catalog = new BusinessCatalog();
        using var args = JsonDocument.Parse("""{"customerId":"CUST-100","sku":"SKU-ALPHA","quantity":1,"idempotencyKey":"abcdefghi","confirm":false}""");
        var result = catalog.Call("business.create_demo_order", args.RootElement, CancellationToken.None);
        Assert.True(result["isError"]!.GetValue<bool>());
    }
}
