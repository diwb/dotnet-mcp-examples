# Authorization

Project 4 now includes a local OAuth/OIDC authorization server implemented with OpenIddict, not handwritten tokens.

## Local Issuer

- Project: `src/McpExamples.AuthorizationServer/McpExamples.AuthorizationServer.csproj`.
- Default issuer: `https://localhost:7001/`.
- Discovery: `https://localhost:7001/.well-known/openid-configuration`.
- JWKS: `https://localhost:7001/.well-known/jwks`.
- Protected resource metadata: `https://localhost:7001/.well-known/oauth-protected-resource`.
- Public client id: `mcp-examples-public`.
- Loopback redirect URI: `http://127.0.0.1:37645/callback`.

No real secrets or production identities are stored. The authorization server seeds only a public local client and deterministic demo subject data.

## Flow

The client uses Authorization Code Flow with PKCE S256:

```powershell
dotnet run --project src/McpExamples.Client.Console --configuration Release -- auth-code https://localhost:7001 "catalog.read orders.read orders.write"
```

The command opens the authorization URL, listens on the loopback callback, redeems the code with the verifier, and prints the token response. Use the returned access token for HTTP MCP calls:

```powershell
$env:MCP_EXAMPLES_ACCESS_TOKEN = "<access_token>"
dotnet run --project src/McpExamples.Client.Console --configuration Release -- http https://localhost:8081/mcp tools
```

## Scopes

- `catalog.read`: availability and quote tools.
- `orders.read`: customer/order read tools.
- `orders.write`: demo order create/cancel tools.

The remote MCP endpoint validates issuer, audience/resource (`mcp-examples-remote`), token expiration and required tool scope. Missing or invalid tokens return `401` with `WWW-Authenticate`; insufficient scope returns `403` with `insufficient_scope`.
