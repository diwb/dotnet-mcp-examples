# Authorization

The previous fixed local bearer token demonstration was removed during the official SDK refactor.

Current status:

- The remote MCP endpoint is mapped by the official SDK with `app.MapMcp("/mcp")`.
- Origin validation, payload limits and rate limiting run before the MCP endpoint.
- A complete OAuth/OIDC provider with Authorization Code + PKCE, issuer/audience validation, token expiration and per-tool scopes remains a required hardening item.

Do not deploy the HTTP sample as a protected production resource until a real local authorization server such as OpenIddict or Keycloak is added and validated.
