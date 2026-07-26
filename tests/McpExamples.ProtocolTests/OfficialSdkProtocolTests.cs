using McpExamples.Shared;

namespace McpExamples.ProtocolTests;

public sealed class OfficialSdkProtocolTests
{
    private static string Root => RepositoryPaths.Root;

    [Fact] public void Protocol_version_is_2025_11_25() => Assert.Equal("2025-11-25", McpProtocol.Version);
    [Fact] public void Sdk_version_is_stable_1_4_1() => Assert.Equal("1.4.1", McpProtocol.SdkVersion);
    [Fact] public void Shared_runtime_does_not_define_manual_protocol_router() => Assert.DoesNotContain("class Mcp" + "Dispatcher", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Shared", "McpRuntime.cs")), StringComparison.Ordinal);
    [Fact] public void Shared_runtime_does_not_dispatch_json_rpc_methods() { var text = File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Shared", "McpRuntime.cs")); Assert.DoesNotContain("tools" + "/list", text, StringComparison.Ordinal); Assert.DoesNotContain("resources" + "/read", text, StringComparison.Ordinal); }
    [Fact] public void Workspace_server_uses_official_stdio_transport() => Assert.Contains("WithStdioServerTransport", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Server.Workspace", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Business_server_uses_official_stdio_transport() => Assert.Contains("WithStdioServerTransport", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Server.Business", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Remote_server_uses_official_http_transport() => Assert.Contains("WithHttpTransport", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Remote_server_maps_mcp_with_sdk() => Assert.Contains("app.MapMcp", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Remote_server_does_not_map_manual_mcp_post() => Assert.DoesNotContain("MapPost(\"/mcp\"", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    [Fact] public void Tools_use_mcp_server_tool_attribute() => Assert.Contains("McpServerTool", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Shared", "McpRuntime.cs")), StringComparison.Ordinal);
    [Fact] public void Resources_use_mcp_server_resource_attribute() => Assert.Contains("McpServerResource", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Shared", "McpRuntime.cs")), StringComparison.Ordinal);
    [Fact] public void Prompts_use_mcp_server_prompt_attribute() => Assert.Contains("McpServerPrompt", File.ReadAllText(Path.Combine(Root, "src", "McpExamples.Shared", "McpRuntime.cs")), StringComparison.Ordinal);
}


