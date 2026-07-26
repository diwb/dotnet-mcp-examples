# Final Audit

Status: official SDK refactor completed locally on 2026-07-26.

## Versions

- .NET SDK: `10.0.110`.
- MCP protocol selected: `2025-11-25`.
- MCP SDK: `ModelContextProtocol 1.4.1` and `ModelContextProtocol.AspNetCore 1.4.1`.
- Pre-release SDK line `2.0.0-rc.1` is not used.

## Proof Of Official SDK Use

- STDIO servers use `AddMcpServer(...).WithStdioServerTransport()`.
- HTTP server uses `AddMcpServer(...).WithHttpTransport()` and `app.MapMcp("/mcp")`.
- Client uses `McpClient.CreateAsync`, `StdioClientTransport`, `HttpClientTransport` and `HttpTransportMode.StreamableHttp`.
- Tools, resources and prompts are declared with `McpServerTool`, `McpServerResource` and `McpServerPrompt` attributes.
- The removed manual protocol router no longer exists in source.

## Validation Commands

```powershell
dotnet format DotNetMcpExamples.slnx --verify-no-changes --verbosity minimal
dotnet build DotNetMcpExamples.slnx -c Release --no-restore
dotnet test DotNetMcpExamples.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet run --project src/McpExamples.Client.Console --configuration Release -- stdio .\src\McpExamples.Server.Workspace\bin\Release\net10.0\McpExamples.Server.Workspace.exe tools
dotnet run --project src/McpExamples.Client.Console --configuration Release --no-build -- http http://127.0.0.1:5055/mcp tools
```

## Tests

- Unit tests: 17 passed.
- Security tests: 14 passed.
- Protocol tests: 12 passed.
- Integration tests: 5 passed.
- Total: 48 passed, 0 failed.
- Coverage: 24.76% line coverage across Cobertura instrumented files, 102/412 lines.

## HTTP Validation

A temporary ASP.NET Core process was started on `http://127.0.0.1:5055`. The official SDK HTTP client listed business tools successfully through `HttpClientTransport` with `HttpTransportMode.StreamableHttp`.

## Inspector And Conformance

Attempted:

```powershell
npx @modelcontextprotocol/inspector --version
```

Result: command timed out after 124 seconds without returning version. No Inspector screenshot or official conformance result is claimed.

## OAuth/OIDC

Fixed demo bearer tokens were removed. A complete local OAuth/OIDC authorization server with Authorization Code + PKCE, issuer/audience validation and per-tool scopes is not completed in this pass. The HTTP sample must not be treated as a protected production resource until that is added.

## Docker

Attempted:

```powershell
docker build -t dotnet-mcp-examples-remote .
```

Result:

```text
ERROR: error during connect: Head "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/_ping": open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.
```

The Docker daemon was unavailable, so image build, authorization server container and container health were not validated.

## GitHub

Branch: `fix/official-mcp-implementation`.

Validated local commit: `a2c8d04be577c20cd673b3f7a7395af635cb04d0`.

CI run 30223472039 passed on main. CodeQL run 30223472047 passed on main. Release: https://github.com/diwb/dotnet-mcp-examples/releases/tag/v1.0.1. Artifacts: 8 framework-dependent Windows/Linux zip archives plus checksums.sha256.

Checksums:

`	ext
cf9e6f53491ff8f163e01c128de13cefc49843e4f0f1b7d31aac5cc35df50a48  McpExamples.Client.Console-linux-x64.zip
2068f38f3e3f223f5bcd4779675e6bd3ff9ab381297b282606657a1dcbf5a3c9  McpExamples.Client.Console-win-x64.zip
fcb17a4bc9be7d29caea4c45991383914beb8902630effacbf431a1c2912a671  McpExamples.Server.Business-linux-x64.zip
a7e2ce9af4de944fa24751d0e57f176d8791d695f0e60d004a3667503bedf006  McpExamples.Server.Business-win-x64.zip
23e7bf2dca5adb4b8eeda24768ba0a318c6c7a01b1aa47f0188cacf42f7d41d4  McpExamples.Server.Remote-linux-x64.zip
40c8392727747908c9cfb7cb48585ac66035bccedb535e0318b82a5455355ecc  McpExamples.Server.Remote-win-x64.zip
0ef2e4ac064ff6bb32fd5f771ff27bca5f727730115232d19cb94a584dcedd5f  McpExamples.Server.Workspace-linux-x64.zip
554b2dc5e75121192762583f52f66a0dfcc2738558da2e62156a7ef0fc255cfb  McpExamples.Server.Workspace-win-x64.zip
` 

Final implementation commit before release: $hash.


