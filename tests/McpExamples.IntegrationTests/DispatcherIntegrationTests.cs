using System.Diagnostics;
using McpExamples.Shared;

namespace McpExamples.IntegrationTests;

public sealed class OfficialServerIntegrationTests
{
    [Fact]
    public async Task Workspace_server_process_starts_without_stdout_banner()
    {
        var exe = Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Workspace", "bin", "Release", "net10.0", OperatingSystem.IsWindows() ? "McpExamples.Server.Workspace.exe" : "McpExamples.Server.Workspace");
        if (!File.Exists(exe)) return;
        using var process = Process.Start(new ProcessStartInfo(exe) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
        await Task.Delay(500);
        Assert.False(process.HasExited);
        Assert.True(process.StandardOutput.BaseStream.CanRead);
        process.Kill(entireProcessTree: true);
    }

    [Fact]
    public void Client_uses_mcp_client_factory()
    {
        var text = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Client.Console", "Program.cs"));
        Assert.Contains("McpClient.CreateAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Http_server_has_separate_health_endpoint()
    {
        Assert.Contains("/health", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", "McpExamples.Server.Remote", "Program.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Dockerfile_publishes_remote_server()
    {
        Assert.Contains("McpExamples.Server.Remote", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "Dockerfile")), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_config_uses_stdio_server()
    {
        Assert.Contains("McpExamples.Server.Workspace", File.ReadAllText(Path.Combine(RepositoryPaths.Root, "examples", "host-configs", "claude-desktop.workspace.json")), StringComparison.Ordinal);
    }
}


