# MCP Human Approval for Destructive Operations

## Goal

Require a real WinContainers user decision before any destructive MCP operation
can execute. Approval is obtained with MCP form elicitation during the same
stateful MCP request that invoked the destructive tool. The existing automation
path remains available only when `McpDestructiveConfirmationEnabled` is explicitly
disabled.

The destructive tools in scope are `remove_container`, `remove_image`,
`remove_volume`, `remove_network`, and `redeploy_web_only`.

## Approaches considered

1. **In-request MCP elicitation (implemented).** The service asks the connected
   MCP client to present the approval prompt and waits for the response before
   invoking the driver. The client owns the prompt surface; the service remains
   fail-closed and never trusts a client-side flag without the elicitation result.
2. **A separate approval HTTP endpoint.** This would add an unnecessary second
   security boundary and an operation-token round trip.

## Architecture

### Server policy

`McpDestructiveConfirmationPolicy` remains the process-local master switch.
When enabled, each destructive tool checks the connected client's elicitation
capability and sends a form request over the originating stateful MCP stream.
The request contains a sanitized action summary and session visibility/context.
The tool proceeds only for an `accept` result whose `Allow` field is exactly
`allow`; all other results and failures are rejected.

### MCP tool protocol

The destructive tool call sends an in-request `elicitation/create` request and
waits for the client's response. There is no `operationId`, `confirm` argument,
second tool call, or approval HTTP endpoint. The MCP transport must be
stateful (`Stateless = false`) so the originating request stream remains
available for the server-to-client prompt and its response.

Summaries are made from safe identifiers only. Remove operations show the
action and target. Redeploy shows the web container, replacement image, and
counts or presence flags for optional settings. It never includes environment
values, tar data, or complete volume mount strings. User-controlled display
values are trimmed, control characters are removed, and long values are
bounded.

### Client-owned approval prompt

The connected MCP client owns the human prompt. It receives the elicitation
schema (`Allow` = `allow` or `deny`) and decides how to render it. The server
does not reference WinUI, subscribe to an App event, or invoke a local dialog.
Clients that cannot perform elicitation are rejected before any driver call.

### Settings and lifecycle

`McpDestructiveConfirmationEnabled` stays the master setting and defaults to
`true`. The existing startup environment override and Settings toggle continue
to set the policy. When disabled, the current explicit one-call automation
bypass remains in place. When enabled, the in-request human elicitation response is required.

The service project does not reference the App project. A small optional
driver override in `ServiceHost.Build` is used only by integration tests so
the MCP protocol can be tested without requiring WSLC.

## Error handling

The tool rejects all invalid or incomplete elicitation states with a clear
reason in the MCP envelope. Approval is never inferred from a client capability
or a tool argument alone. Missing capability, cancellation, a closed prompt,
or a handler failure can never mark a request approved.

The existing session wrapper remains on successful and failure responses.
Successful destructive tool behavior is unchanged after elicitation.

## Verification

Focused tests cover unsupported capability, denied/cancelled/invalid elicitation
and the accepted `Allow=allow` path. Stateful SDK integration
coverage uses an MCP client that advertises elicitation, handles the
`elicitation/create` request with an accepted Allow response, and verifies
exactly one driver call.

README and MCP tool descriptions document the human decision as an in-request
elicitation rather than a two-call operation-token protocol.
