using System.Text.Json;
using McpExamples.Shared;

namespace McpExamples.UnitTests;

public sealed class WorkspaceCatalogTests
{
    [Fact]
    public void Resolve_rejects_path_traversal()
    {
        var catalog = new WorkspaceCatalog(RepositoryPaths.WorkspaceRoot);
        Assert.Null(catalog.Resolve("../secret.md"));
    }

    [Fact]
    public void Resolve_rejects_absolute_path()
    {
        var catalog = new WorkspaceCatalog(RepositoryPaths.WorkspaceRoot);
        Assert.Null(catalog.Resolve(Path.GetFullPath("secret.md")));
    }

    [Fact]
    public void Search_returns_bounded_matches()
    {
        var catalog = new WorkspaceCatalog(RepositoryPaths.WorkspaceRoot);
        using var args = JsonDocument.Parse("""{"query":"MCP","maxResults":2}""");
        var result = catalog.Call("workspace.search_text", args.RootElement, CancellationToken.None);
        Assert.False(result["isError"]!.GetValue<bool>());
    }
}
