using McpExamples.Shared;

namespace McpExamples.IntegrationTests;

public sealed class DispatcherIntegrationTests
{
    [Fact]
    public async Task Tool_call_round_trip_returns_content()
    {
        var dispatcher = new McpDispatcher();
        var response = await dispatcher.DispatchAsync("""{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"business.check_availability","arguments":{"sku":"SKU-ALPHA","quantity":2}}}""", McpServerKind.Business);
        Assert.NotNull(response);
        Assert.Contains("canFulfill", response!.ToJsonString(McpProtocol.JsonOptions), StringComparison.Ordinal);
    }
}
