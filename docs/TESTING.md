# Testing

Validated commands on 2026-07-27:

```powershell
dotnet build DotNetMcpExamples.slnx -c Release --no-restore
dotnet test DotNetMcpExamples.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
```

Result: 76 tests passed, 0 failed.

## Test Categories

- Unit tests: 35 passed. Covers tools, resources, prompts, pagination bounds, cancellation, catalog/domain handlers and shared authorization policy.
- Security tests: 24 passed. Covers unsafe paths, mutation confirmation, OpenIddict configuration, Authorization Code + PKCE S256, discovery, protected resource metadata, invalid PKCE, invalid redirect URI, invalid scope, token expiration metadata, issuer/audience payload and 401/403 source behavior.
- Protocol tests: 12 passed. Confirms official SDK transports/attributes remain in use and the manual JSON-RPC dispatcher is absent.
- Integration tests: 5 passed. Covers STDIO process startup, client SDK usage, health endpoint separation, Docker publication target and host configuration.

## Coverage

Cobertura files are emitted per test assembly. The meaningful project-level reports from the final run were:

- `McpExamples.Shared`: 98.27% line coverage in the unit-test Cobertura report.
- `McpExamples.AuthorizationServer`: 94.87% line coverage in the security-test Cobertura report.

Entry points and ASP.NET composition glue are intentionally not chased to 70% line coverage. Behavior reachable from those entry points is covered through policy extraction, source assertions, WebApplicationFactory OAuth tests, SDK protocol tests and CI Docker validation.
