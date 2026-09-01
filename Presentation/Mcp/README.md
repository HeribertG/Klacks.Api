# Klacks MCP Server

**Klacks** exposes an authenticated [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) endpoint over Streamable HTTP, so external AI assistants can connect and operate the Klacks scheduling application directly.

- **Endpoint:** `POST /mcp`
- **Transport:** Streamable HTTP (stateless)
- **Auth:** OAuth 2.1 Resource Owner / JWT Bearer / Personal Access Token (PAT)
- **Spec:** RFC 9728
- **Rate Limiting:** Fixed window per user/IP

## Quick Start

```bash
# Connect with an MCP-compatible client (e.g. Claude Desktop, Cursor)
# Configure the server URL and authentication in your client's MCP settings.
```

## Authentication

The `/mcp` endpoint requires authentication via one of:

| Method | Scheme | Description |
|---|---|---|
| **JWT Bearer** | `Authorization: Bearer <jwt>` | Standard JWT token from Klacks identity provider |
| **Personal Access Token** | `Authorization: Bearer <pat>` | PAT generated in Klacks user settings |
| **OAuth 2.1 (MCP native)** | MCP auth flow | Resource metadata at `/.well-known/oauth-protected-resource` |

### OAuth 2.1 Resource Metadata

```
GET /.well-known/oauth-protected-resource
```

Returns:
```json
{
  "resource": "https://your-klacks-domain/mcp",
  "authorization_servers": ["https://your-klacks-domain"],
  "scopes_supported": ["mcp.tools"]
}
```

## Available Tools

Tools mirror Klacks's built-in assistant skills, filtered by the authenticated user's permissions. The tool list is dynamic — `tools/list` returns only the tools the user is authorized to call.

### Tool Categories

| Category | Examples | Effect |
|---|---|---|
| **Navigation** | `navigate_to`, `get_page_controls` | Read |
| **System** | `get_current_time`, `get_system_info`, `get_user_context` | Read |
| **Planning** | `create_shift`, `assign_client`, `optimize_routes` | Read/Write |
| **CRUD** | `create_employee`, `update_group`, `delete_shift` | Write |
| **Settings** | `validate_calendar_rule`, `add_ai_memory` | Read/Write |

Write actions may return a **confirmation request** instead of executing immediately. Confirm them by calling the `confirm_pending_action` tool with the provided confirmation token.

## Resources

| URI | Description |
|---|---|
| `klacks://docs/` | Klacks documentation and prompts |

List all resources:
```json
{"jsonrpc":"2.0","id":1,"method":"resources/list","params":{}}
```

## Prompts

Klacks provides guided workflow prompts for common scheduling scenarios.

List all prompts:
```json
{"jsonrpc":"2.0","id":1,"method":"prompts/list","params":{}}
```

Get a specific prompt:
```json
{"jsonrpc":"2.0","id":1,"method":"prompts/get","params":{"name":"planning-profile-setup"}}
```

## Server Info

| Field | Value |
|---|---|
| **Name** | `klacks-mcp` |
| **Title** | `Klacks MCP Server` |
| **Version** | `1.0.0` |
| **Instructions** | Klacks workforce planning and scheduling. Tools mirror the built-in assistant skills, filtered by the authenticated user's permissions. Write actions may return a confirmation request instead of executing immediately. |

## Rate Limiting

| Policy | Limit | Window |
|---|---|---|
| MCP requests | Per user/IP | Fixed window |

Exceeding the rate limit returns a 429 status code.

## Security

- All tools are filtered by the user's permissions (Assistant access required)
- Sensitive skills are excluded from the MCP tool list via `McpSkillExposurePolicy`
- The MCP principal is capped to authorized permissions only (no privilege escalation)
- Write operations require explicit confirmation via `confirm_pending_action`

## Example: Tool Call Flow

1. **List available tools:**
   ```json
   {"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}
   ```

2. **Call a tool:**
   ```json
   {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_current_time","arguments":{"format":"full"}}}
   ```

3. **If confirmation is required:**
   ```json
   {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"confirm_pending_action","arguments":{"confirmationToken":"abc123"}}}
   ```

## Implementation

The MCP server is implemented in:
- `Presentation/Mcp/McpEndpointExtensions.cs` — ASP.NET Core pipeline wiring
- `Presentation/Mcp/McpServerConstants.cs` — Server identity and route
- `Presentation/Mcp/McpSkillExposurePolicy.cs` — Tool exposure filtering
- `Application/Skills/Meta/SkillRiskClassifier.cs` — Skill risk classification

## Links

- [MCP Specification](https://modelcontextprotocol.io/specification)
- [RFC 9728 — Model Context Protocol](https://www.rfc-editor.org/rfc/rfc9728)
- [Klacks Documentation](https://klacks-software.ch)
