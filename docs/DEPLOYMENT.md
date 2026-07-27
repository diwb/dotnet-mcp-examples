# Deployment

Docker Compose now runs two local services:

- `authorization-server`: OpenIddict OAuth/OIDC issuer on HTTPS port `7001`.
- `mcp-remote`: official SDK Streamable HTTP MCP server on HTTPS port `8081`.

Both images run as a non-root `appuser` after build-time package installation. No credentials or certificates are committed. Compose expects local development certificates in `.certs`, which is ignored by git.

Required files for local compose:

- `.certs/mcp-examples.pfx`
- `.certs/mcp-examples-ca.crt`

Required environment:

```powershell
$env:MCP_EXAMPLES_CERT_DIR = ".\.certs"
$env:MCP_EXAMPLES_CERT_PASSWORD = "<local password>"
docker compose up --build
```

Health endpoints:

```powershell
curl.exe --insecure https://localhost:7001/health
curl.exe --insecure https://localhost:8081/health
```

OAuth discovery:

```powershell
curl.exe --insecure https://localhost:7001/.well-known/openid-configuration
curl.exe --insecure https://localhost:7001/.well-known/oauth-protected-resource
```

Local Docker Desktop was not available during the 2026-07-27 hardening run, so full container startup was delegated to GitHub Actions. The `docker-oauth-mcp` CI job generates ephemeral CA/cert material, builds compose, checks health/discovery, emits an OAuth token with PKCE, calls MCP over HTTPS with the console client, and shuts the stack down.
