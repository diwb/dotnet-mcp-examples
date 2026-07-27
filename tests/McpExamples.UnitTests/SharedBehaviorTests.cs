using System.Security.Claims;
using System.Text.Json;
using McpExamples.Shared;

namespace McpExamples.UnitTests;

public sealed class SharedBehaviorTests
{
    [Fact]
    public void Workspace_tools_surface_catalog_operations()
    {
        var tools = new WorkspaceTools(new WorkspaceCatalog());
        Assert.Contains("architecture.md", tools.ListDocuments(CancellationToken.None));
        Assert.Contains(tools.SearchText("MCP", 5, CancellationToken.None), match => match.Document.EndsWith("architecture.md", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("text/plain", tools.DocumentMetadata("runbook.txt").MimeType);
    }

    [Fact]
    public void Workspace_tools_respect_cancellation_before_io()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var tools = new WorkspaceTools(new WorkspaceCatalog());
        Assert.Throws<OperationCanceledException>(() => tools.ListDocuments(source.Token));
        Assert.Throws<OperationCanceledException>(() => tools.CreateNote("cancelled.md", "x", source.Token));
    }

    [Fact]
    public void Workspace_note_update_overwrites_only_with_confirmation()
    {
        var catalog = new WorkspaceCatalog(NewWorkspaceRoot());
        var tools = new WorkspaceTools(catalog);
        tools.CreateNote("ops.md", "one", CancellationToken.None);
        var result = tools.UpdateNote("ops.md", "two", confirm: true, CancellationToken.None);
        Assert.Equal("ops.md", result.Name);
        Assert.Equal("two", catalog.ReadDocument("notes/ops.md"));
    }

    [Fact]
    public void Workspace_document_read_truncates_large_text()
    {
        var root = NewWorkspaceRoot();
        File.WriteAllText(Path.Combine(root, "large.txt"), new string('a', 9000));
        Assert.Equal(8192, new WorkspaceCatalog(root).ReadDocument("large.txt").Length);
    }

    [Fact]
    public void Workspace_search_clamps_page_size_to_twenty()
    {
        var root = NewWorkspaceRoot();
        File.WriteAllLines(Path.Combine(root, "many.txt"), Enumerable.Range(1, 40).Select(i => $"needle {i}"));
        var matches = new WorkspaceCatalog(root).Search("needle", 200, CancellationToken.None);
        Assert.Equal(20, matches.Count);
        Assert.Equal(1, matches[0].Line);
    }

    [Fact]
    public void Workspace_resources_return_bounded_typed_content()
    {
        var resources = new WorkspaceResources(new WorkspaceCatalog());
        Assert.Equal("workspace://documents/architecture.md", resources.Architecture().Uri);
        Assert.Equal("text/plain", resources.Runbook().MimeType);
        Assert.Contains("MCP", resources.Document("architecture.md").Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Business_tools_enforce_mutation_confirmation_and_idempotency()
    {
        var tools = new BusinessTools(new BusinessCatalog());
        var order = tools.CreateDemoOrder("CUST-100", "SKU-BETA", 999, "idem-key-999", confirm: true, CancellationToken.None);
        var repeated = tools.CreateDemoOrder("CUST-100", "SKU-BETA", 2, "idem-key-999", confirm: true, CancellationToken.None);
        Assert.Equal(order.OrderId, repeated.OrderId);
        Assert.Equal(500, order.Quantity);
        Assert.Equal("cancelled", tools.CancelDemoOrder("PO-1001", confirm: true, CancellationToken.None).Status);
        Assert.Throws<InvalidOperationException>(() => tools.CreateDemoOrder("CUST-100", "SKU-BETA", 1, "idem-key-2", false, CancellationToken.None));
    }

    [Fact]
    public void Business_tools_and_resources_return_demo_only_data()
    {
        var catalog = new BusinessCatalog();
        var tools = new BusinessTools(catalog);
        var resources = new BusinessResources(catalog);
        Assert.Equal("CUST-100", tools.GetCustomer("").CustomerId);
        Assert.Single(tools.ListOrders("held"));
        Assert.True(tools.CheckAvailability("SKU-ALPHA", 120, CancellationToken.None).CanFulfill);
        Assert.Equal("USD", tools.QuoteOrder("CUST-100", "SKU-BETA", 3).Currency);
        Assert.Contains("SKU-ALPHA", resources.Catalog().Text);
        Assert.Contains("PO-1001", resources.Order("PO-1001").Text);
    }

    [Fact]
    public void Prompts_preserve_untrusted_content_boundaries()
    {
        Assert.Contains("untrusted", new WorkspacePrompts().SummarizeDocument("architecture.md").ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Compare", new WorkspacePrompts().CompareDocuments("a.md", "b.md").ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checklist", new WorkspacePrompts().PrepareChecklist("runbook.txt").ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approval", new BusinessPrompts().ReviewOrder("PO-1001").ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fulfillment", new BusinessPrompts().PrepareChecklist("PO-1001").ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("business.get_customer", "orders.read")]
    [InlineData("business.list_orders", "orders.read")]
    [InlineData("business.get_order", "orders.read")]
    [InlineData("business.check_availability", "catalog.read")]
    [InlineData("business.quote_order", "catalog.read")]
    [InlineData("business.create_demo_order", "orders.write")]
    [InlineData("business.cancel_demo_order", "orders.write")]
    public void Authorization_policy_maps_tools_to_required_scopes(string toolName, string scope) =>
        Assert.Equal(scope, McpAuthorizationPolicy.RequiredScopeForTool(toolName));

    [Fact]
    public void Authorization_policy_allows_and_denies_tools_by_scope()
    {
        var readPrincipal = PrincipalWith("orders.read", McpAuthorizationPolicy.ResourceAudience);
        var writePrincipal = PrincipalWith("orders.write", McpAuthorizationPolicy.ResourceAudience);
        Assert.True(McpAuthorizationPolicy.HasScope(readPrincipal, McpAuthorizationPolicy.RequiredScopeForTool("business.get_order")!));
        Assert.False(McpAuthorizationPolicy.HasScope(readPrincipal, McpAuthorizationPolicy.RequiredScopeForTool("business.cancel_demo_order")!));
        Assert.True(McpAuthorizationPolicy.HasScope(writePrincipal, McpAuthorizationPolicy.RequiredScopeForTool("business.cancel_demo_order")!));
        Assert.False(McpAuthorizationPolicy.HasScope(writePrincipal, "catalog.read"));
    }

    [Fact]
    public void Authorization_policy_validates_audience_and_rejects_unknown_scope()
    {
        var principal = PrincipalWith("orders.read unknown.scope", "other-resource");
        Assert.False(McpAuthorizationPolicy.HasAudience(principal, McpAuthorizationPolicy.ResourceAudience));
        Assert.False(McpAuthorizationPolicy.HasScope(principal, "unknown.scope"));
        Assert.Null(McpAuthorizationPolicy.RequiredScopeForTool("workspace.list_documents"));
    }

    private static ClaimsPrincipal PrincipalWith(string scopes, string audience)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("scope", scopes));
        identity.AddClaim(new Claim("aud", audience));
        return new ClaimsPrincipal(identity);
    }

    private static string NewWorkspaceRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-examples-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "notes"));
        File.WriteAllText(Path.Combine(root, "seed.md"), "MCP seed document");
        return root;
    }
}

