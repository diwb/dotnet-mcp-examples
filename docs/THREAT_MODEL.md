# Threat Model

Primary threats covered by code:

- Path traversal and absolute path escape in workspace resources and tools.
- STDIO contamination by keeping operational logs on stderr.
- Destructive mutation without explicit confirmation.
- Oversized HTTP payloads.
- DNS rebinding exposure through Origin validation.
- Scope confusion for demo HTTP tools.
- Response bloat through bounded text/resource output.

Residual risks:

- The HTTP authorization flow uses deterministic local demo bearer tokens, not a complete OAuth 2.1/OIDC server.
- The project does not claim full prompt injection prevention; untrusted document content is separated in prompt wording and documented as model-controlled risk.
- MCP Inspector evidence was not generated in this pass.
