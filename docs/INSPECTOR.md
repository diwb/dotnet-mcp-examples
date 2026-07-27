# MCP Inspector

The MCP Inspector is pinned as a reproducible development dependency:

- Package: `@modelcontextprotocol/inspector`.
- Version: `1.0.0`.
- Location: `tools/inspector/package.json` and `tools/inspector/package-lock.json`.

Install from the lockfile:

```powershell
cd tools/inspector
npm ci
```

Validated on 2026-07-27 with the compiled STDIO workspace server:

```powershell
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method resources/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method prompts/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/call --tool-name workspace.search_text --tool-arg query=MCP --tool-arg maxResults=2
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/call --tool-name workspace.search_text --tool-arg query=x --tool-arg maxResults=2
```

Evidence observed:

- STDIO connection initialized successfully.
- `tools/list` returned five workspace tools with schemas and annotations.
- `resources/list` returned `workspace://documents/architecture.md` and `workspace://documents/runbook.txt`.
- `prompts/list` returned three workspace prompts.
- Successful tool call returned structured search results.
- Controlled error returned `isError: true` for an invalid one-character search query.

HTTP Inspector validation is not claimed locally. Authenticated HTTP MCP validation is covered by the Docker GitHub Actions job using the console client and OAuth token. UI screenshots were not generated because no browser UI automation was opened in this environment.

Note: `npm audit` currently reports 13 vulnerabilities in Inspector transitive development dependencies from the official package lock resolution. The package remains pinned for reproducibility; no production runtime depends on it.
