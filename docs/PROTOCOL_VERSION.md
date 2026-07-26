# Protocol Version

Date checked: 2026-07-26.

| Item | Adopted value | Evidence |
| --- | --- | --- |
| MCP protocol | `2025-06-18` | Official specification page lists protocol revision `2025-06-18`. |
| C# SDK | `ModelContextProtocol 1.4.1` | GitHub releases and NuGet list `v1.4.1` as the latest stable release. |
| Pre-release excluded | `2.0.0-rc.1` | Release is marked pre-release and announces stable `2.0.0` is still expected later. |
| HTTP transport | Streamable HTTP from `2025-06-18` | The spec states Streamable HTTP replaces HTTP+SSE from `2024-11-05`. |

The code intentionally avoids the `2.0.0` preview/RC API line in the main path.
