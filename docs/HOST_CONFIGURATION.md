# Host Configuration

The sample config in `examples/host-configs/claude-desktop.workspace.json` uses `dotnet run` and relative repository paths. It is intended as a copyable starting point; it does not modify user-global host settings.

Troubleshooting:

- executable not found: run from the repository root or publish the server and use the absolute executable path;
- stdout contaminated: STDIO servers use the SDK transport and configure logs for stderr;
- protocol mismatch: verify `docs/PROTOCOL_VERSION.md`;
- timeout: run through `McpExamples.Client.Console` and inspect stderr;
- HTTP authentication: a real OAuth/OIDC environment is not completed in this pass, so do not deploy the HTTP sample as a protected production resource without adding one.
