# Final Audit

Status: Project 4 final hardening implemented locally on 2026-07-27.

## Commits And Branches

- Validated base commit from `main`: `3e8e603f1e9f8a7f7fc456a1b43d10350a260ffa`.
- Hardening branch: `fix/mcp-security-and-release-hardening`.
- Final implementation commit validated before audit finalization: `ca8295714a3fd86943daf60fdf6a761e6f6bf7b7`.
- GitHub default branch confirmed: `main` via `gh repo view diwb/dotnet-mcp-examples --json defaultBranchRef`.

## Versions

- MCP protocol selected: `2025-11-25`.
- MCP SDK: `ModelContextProtocol 1.4.1` and `ModelContextProtocol.AspNetCore 1.4.1`.
- OAuth/OIDC library: `OpenIddict 7.0.0`.
- Authorization storage package: `OpenIddict.EntityFrameworkCore 7.0.0` with EF Core 9.0.7 in-memory demo storage.
- Inspector: `@modelcontextprotocol/inspector 1.0.0`, pinned in `tools/inspector/package-lock.json`.

## OAuth/PKCE

Implemented project: `McpExamples.AuthorizationServer`.

- Authorization Code Flow implemented by OpenIddict.
- PKCE S256 required.
- Discovery endpoint: `/.well-known/openid-configuration`.
- JWKS endpoint: `/.well-known/jwks`.
- Protected resource metadata endpoint: `/.well-known/oauth-protected-resource`.
- Public local client: `mcp-examples-public`.
- Loopback redirect: `http://127.0.0.1:37645/callback`.
- Demo-only subject/client storage; no real secrets committed.
- Scopes: `catalog.read`, `orders.read`, `orders.write`.
- Remote MCP server validates issuer, audience/resource `mcp-examples-remote`, token expiration and per-tool scope.
- Missing/invalid token returns `401` with `WWW-Authenticate`.
- Insufficient scope returns `403` with `insufficient_scope`.

## Tests And Coverage

Final local command:

```powershell
dotnet build DotNetMcpExamples.slnx -c Release --no-restore
dotnet test DotNetMcpExamples.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
```

Result: 76 passed, 0 failed.

By category:

- Unit tests: 35 passed.
- Security tests: 24 passed.
- Protocol tests: 12 passed.
- Integration tests: 5 passed.

Coverage evidence from final Cobertura reports:

- `McpExamples.Shared`: 98.27% line coverage in unit-test report.
- `McpExamples.AuthorizationServer`: 94.87% line coverage in security-test report.

Coverage includes tools, resources, prompts, pagination, cancellation, domain handlers, security policies, client auth behavior by source assertion, OAuth discovery, PKCE, invalid redirect, invalid scope, token issuance and scope/audience/expiration payload validation. Entry points, ASP.NET composition glue and generated code are not forced above 70%; behavior is exercised through extracted policies, WebApplicationFactory, protocol tests and CI Docker flow.

NuGet vulnerability check:

```powershell
dotnet list DotNetMcpExamples.slnx package --vulnerable --include-transitive
```

Result: no vulnerable NuGet packages reported by configured sources.

## Inspector

Pinned Inspector dependency:

```powershell
cd tools/inspector
npm ci
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method resources/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method prompts/list
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/call --tool-name workspace.search_text --tool-arg query=MCP --tool-arg maxResults=2
npm exec -- mcp-inspector --cli <workspace-server-exe> --method tools/call --tool-name workspace.search_text --tool-arg query=x --tool-arg maxResults=2
```

Validated: STDIO connection, capabilities surfaced through list methods, tools, resources, prompts, successful tool call and controlled error (`isError: true`).

Not claimed: full Inspector UI conformance or screenshots. The UI was not opened in this environment. `npm audit` reports 13 vulnerabilities in official Inspector transitive development dependencies; runtime applications do not depend on the Inspector package.

## Docker

Files:

- `Dockerfile`: HTTPS remote MCP server image.
- `Dockerfile.authorization`: HTTPS Authorization Server image.
- `docker-compose.yml`: `authorization-server` plus `mcp-remote`, local cert volume, healthchecks, non-root app users.

Local validation:

- `docker compose config`: passed.
- `docker compose build`: not executed successfully because Docker Desktop daemon was unavailable on this machine: `open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified`.

GitHub Actions validation added in `docker-oauth-mcp` job:

- Generate ephemeral CA and service certificate.
- Build compose images.
- Start authorization server and MCP HTTP server.
- Check health.
- Check OAuth discovery.
- Emit token using Authorization Code + PKCE.
- Call MCP authenticated over HTTPS with the console client.
- Shutdown compose.

## CI And CodeQL

- CI workflow updated to include build/test and Docker/OAuth/MCP validation.
- CodeQL workflow remains present in `.github/workflows/codeql.yml`.
- CI run `30270710804` passed on `main` at `ca8295714a3fd86943daf60fdf6a761e6f6bf7b7`.
- Docker job `docker-oauth-mcp` passed in CI: build, startup, health, OAuth discovery, token issuance, authenticated MCP call and shutdown.
- CodeQL run `30270713261` passed on `main` at `ca8295714a3fd86943daf60fdf6a761e6f6bf7b7`.
- Final release tag is created after this audit status update; GitHub release metadata records the exact tag target commit.

## Release v1.1.0

Release URL: `https://github.com/diwb/dotnet-mcp-examples/releases/tag/v1.1.0`.

Artifacts generated locally under `artifacts/` for Windows and Linux:

- `McpExamples.AuthorizationServer-linux-x64.zip`
- `McpExamples.AuthorizationServer-win-x64.zip`
- `McpExamples.Client.Console-linux-x64.zip`
- `McpExamples.Client.Console-win-x64.zip`
- `McpExamples.Server.Business-linux-x64.zip`
- `McpExamples.Server.Business-win-x64.zip`
- `McpExamples.Server.Remote-linux-x64.zip`
- `McpExamples.Server.Remote-win-x64.zip`
- `McpExamples.Server.Workspace-linux-x64.zip`
- `McpExamples.Server.Workspace-win-x64.zip`
- `checksums.sha256`

Checksums:

```text
1ed4ed9fad8c6a4c50721afa1a256a05aabd7d9ff3ffde82e9167cf94e18693f  McpExamples.AuthorizationServer-linux-x64.zip
4a95cf69bfd3779b8dcfba38499a8d5cfb777ac7d6e8d6ff685711f5a59513c6  McpExamples.AuthorizationServer-win-x64.zip
4c43192e474068ef72b7d2b0e485cb077c0c592918cdc68fd4e6ea578b3a693d  McpExamples.Client.Console-linux-x64.zip
ed4590f634ef1f692334d624ceb458e96b9d3c87fd1502b2a2dc02ed0eea7cff  McpExamples.Client.Console-win-x64.zip
9c67c8382954ec12378e0e49de602fa9679db2071fdf7223901ea75de958ba8a  McpExamples.Server.Business-linux-x64.zip
bcb31a517491f9f0f1cf5598c138e265257a4dfd99f2b22b35d51a934ae853c5  McpExamples.Server.Business-win-x64.zip
2c0104fd05b5657ba7cc1cc0d4664e0fe0b7a9f4f685cf75cd4bff5c79a103be  McpExamples.Server.Remote-linux-x64.zip
8b520db4e360acae21a9acccdcddf77eb6882dcae9586f0fef179bebc5c273ad  McpExamples.Server.Remote-win-x64.zip
8284e2eb91612fa73299abd62f9c37df4eaf73651bdc2de0d1b413fb56ec209e  McpExamples.Server.Workspace-linux-x64.zip
81724f29c84176c2790abe559400d2ba4cc5a703818cb65150ddbd92d5cb14fe  McpExamples.Server.Workspace-win-x64.zip
```

## Limitations

- Docker build/start was not validated locally because Docker Desktop was unavailable; CI job performs the reproducible validation.
- Inspector UI screenshots are not included; only pinned CLI Inspector evidence is claimed.
- Inspector transitive npm dev dependencies currently report audit findings.
- HTTPS local development requires trusted local certificates or the CI-generated certificate flow.




