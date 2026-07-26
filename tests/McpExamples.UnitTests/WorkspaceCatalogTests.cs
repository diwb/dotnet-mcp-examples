using McpExamples.Shared;

namespace McpExamples.UnitTests;

public sealed class WorkspaceCatalogTests
{
    [Fact] public void Lists_seed_documents() => Assert.Contains("architecture.md", new WorkspaceCatalog().ListDocuments());
    [Fact] public void Resolve_rejects_path_traversal() => Assert.Null(new WorkspaceCatalog().Resolve("../secret.md"));
    [Fact] public void Resolve_rejects_absolute_path() => Assert.Null(new WorkspaceCatalog().Resolve(Path.GetFullPath("secret.md")));
    [Fact] public void Resolve_rejects_unsupported_extension() => Assert.Null(new WorkspaceCatalog().Resolve("script.ps1", mustExist: false));
    [Fact] public void Metadata_reports_markdown_mime_type() => Assert.Equal("text/markdown", new WorkspaceCatalog().GetMetadata("architecture.md").MimeType);
    [Fact] public void Search_returns_bounded_matches() => Assert.InRange(new WorkspaceCatalog().Search("MCP", 2, CancellationToken.None).Count, 1, 2);
    [Fact] public void Search_rejects_short_query() => Assert.Throws<InvalidOperationException>(() => new WorkspaceCatalog().Search("x", 2, CancellationToken.None));
    [Fact] public void Create_note_writes_inside_notes_folder() { var catalog = new WorkspaceCatalog(NewWorkspaceRoot()); var name = $"unit-{Guid.NewGuid():N}.md"; var result = catalog.WriteNote(name, "hello", overwrite: false); Assert.Equal(name, result.Name); }
    [Fact] public void Create_note_rejects_duplicate_without_overwrite() { var catalog = new WorkspaceCatalog(NewWorkspaceRoot()); var name = $"dup-{Guid.NewGuid():N}.md"; catalog.WriteNote(name, "one", false); Assert.Throws<InvalidOperationException>(() => catalog.WriteNote(name, "two", false)); }
    [Fact] public void Read_document_is_bounded_text() => Assert.Contains("STDIO", new WorkspaceCatalog().ReadDocument("architecture.md"), StringComparison.OrdinalIgnoreCase);

    private static string NewWorkspaceRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-examples-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "notes"));
        File.WriteAllText(Path.Combine(root, "seed.md"), "MCP seed document");
        return root;
    }
}

public sealed class BusinessCatalogTests
{
    [Fact] public void Gets_customer_without_personal_data() => Assert.Equal("Contoso Industrial Demo", new BusinessCatalog().GetCustomer("CUST-100").Name);
    [Fact] public void Lists_open_orders() => Assert.All(new BusinessCatalog().ListOrders("open"), order => Assert.Equal("open", order.Status));
    [Fact] public void Gets_seed_order() => Assert.Equal("SKU-ALPHA", new BusinessCatalog().GetOrder("PO-1001").Sku);
    [Fact] public void Availability_caps_requested_quantity() => Assert.Equal(500, new BusinessCatalog().CheckAvailability("SKU-ALPHA", 999).Requested);
    [Fact] public void Quote_is_deterministic() => Assert.Equal(39.00m, new BusinessCatalog().Quote("SKU-ALPHA", 2).Total);
    [Fact] public void Create_order_is_idempotent() { var catalog = new BusinessCatalog(); var one = catalog.CreateOrder("CUST-100", "SKU-ALPHA", 1, "idem-key-001", true); var two = catalog.CreateOrder("CUST-100", "SKU-ALPHA", 1, "idem-key-001", true); Assert.Equal(one.OrderId, two.OrderId); }
    [Fact] public void Cancel_order_requires_known_order() => Assert.Throws<InvalidOperationException>(() => new BusinessCatalog().CancelOrder("missing", true));
}


