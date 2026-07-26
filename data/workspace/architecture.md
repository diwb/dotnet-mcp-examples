# Workspace Server Notes

This trusted sample document describes a local MCP server exposing bounded tools, resources and prompts over stdio.

Security expectations:

- no shell execution;
- no absolute client paths;
- logs must use stderr;
- stdout must contain only JSON-RPC messages.
