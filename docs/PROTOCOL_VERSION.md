# Protocol Version

Date checked: 2026-07-26.

| Item | Adopted value | Evidence |
| --- | --- | --- |
| MCP protocol | `2025-11-25` | SDK 1.4.1 XML documentation links Streamable HTTP to the official `2025-11-25` transport specification. |
| C# SDK | `ModelContextProtocol 1.4.1` and `ModelContextProtocol.AspNetCore 1.4.1` | NuGet restore and project references use the stable 1.4.1 line. |
| Pre-release excluded | `2.0.0-rc.1` | Release is marked pre-release and is not used in the main path. |
| HTTP transport | Official Streamable HTTP | Server uses `WithHttpTransport()` and `app.MapMcp("/mcp")`; client uses `HttpClientTransport` with `HttpTransportMode.StreamableHttp`. |

Protocol framing, initialization, capability negotiation, tools/resources/prompts list/read/call handling and Streamable HTTP request handling are delegated to the official SDK.
