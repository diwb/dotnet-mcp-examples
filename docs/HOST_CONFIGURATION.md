# Host Configuration

The sample config in `examples/host-configs/claude-desktop.workspace.json` uses `dotnet run` and relative repository paths. It is intended as a copyable starting point; it does not modify user-global host settings.

Troubleshooting:

- executable not found: run from the repository root or publish the server and use the absolute executable path;
- stdout contaminated: server logs must remain on stderr;
- protocol mismatch: verify `docs/PROTOCOL_VERSION.md`;
- timeout: run the server with `--version` and check stderr;
- HTTP authentication: use `demo-read` for read tools and `demo-write` for mutation tools in local demos.
