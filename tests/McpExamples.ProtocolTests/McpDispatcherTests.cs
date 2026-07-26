using McpExamples.Shared;

namespace McpExamples.ProtocolTests;

public sealed class McpDispatcherTests
{
    [Fact]
    public void Initialize_advertises_expected_protocol_version()
    {
        var dispatcher = new McpDispatcher();
        var json = dispatcher.DispatchToJson("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""", McpServerKind.Workspace);
        Assert.Contains("\"protocolVersion\":\"2025-06-18\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_method_maps_to_json_rpc_method_not_found()
    {
        var dispatcher = new McpDispatcher();
        var json = dispatcher.DispatchToJson("""{"jsonrpc":"2.0","id":2,"method":"missing","params":{}}""", McpServerKind.Workspace);
        Assert.Contains("\"code\":-32601", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Tools_list_supports_cursor_pagination()
    {
        var dispatcher = new McpDispatcher();
        var json = dispatcher.DispatchToJson("""{"jsonrpc":"2.0","id":3,"method":"tools/list","params":{"cursor":"0"}}""", McpServerKind.Business);
        Assert.Contains("nextCursor", json, StringComparison.Ordinal);
    }
}
