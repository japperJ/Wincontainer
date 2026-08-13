# MCP Human Approval for Destructive Operations

## Goal

Require a real WinContainers user decision before any destructive MCP operation
can execute. The existing `operationId`, expiry, tool-name, canonical-argument,
and one-time-use checks remain the final server-side authority. The existing
automation path remains available only when
`McpDestructiveConfirmationEnabled` is explicitly disabled.

The destructive tools in scope are `remove_container`, `remove_image`,
`remove_volume`, `remove_network`, and `redeploy_web_only`.

## Approaches considered

1. **Static policy event with an App-side coordinator (recommended).** The
   service owns the operation state and raises a request event. The WinUI app
   subscribes after its main window exists and displays the decision dialog.
   This keeps the service authoritative and avoids a Service-to-App reference.
2. **Pass an approval callback into `ServiceHost.Build`.** This gives the
   service a direct callback but couples service startup and tests to the UI
   lifecycle. It also makes alternate hosts harder to support.
3. **Add a separate approval HTTP endpoint.** This would allow an external
   client to approve an operation and would add another security boundary and
   endpoint to protect. It is not needed because the local WinUI app is the
   human approval surface.

## Architecture

### Server policy

`McpDestructiveConfirmationPolicy` remains a process-local static policy. Each
issued operation stores:

- tool name and canonical arguments;
- a short, sanitized display summary;
- session visibility and context for the approval dialog;
- expiry time;
- state: `pending`, `approved`, or `denied`;
- one-time consumption state.

The policy exposes an approval-request event and `TryApprove` and `TryReject`
methods. Issuing an operation raises the event only when a subscriber exists.
If no subscriber exists, the operation remains pending and cannot be approved;
this is a fail-closed condition. Event handlers and dialog failures also
result in rejection, never execution.

`TryConsume` first checks the master setting. When enabled, it rejects unknown,
expired, mismatched, pending, denied, or already-used operations. Only an
approved operation with matching tool name and canonical arguments can be
marked consumed. The record lock makes approval, rejection, expiry, and
consumption atomic under concurrent requests.

### MCP tool protocol

The first call to a destructive tool issues an operation and returns an
envelope containing:

- `requiresConfirmation: true`;
- `humanApprovalRequired: true`;
- `approvalStatus: "pending"`;
- `approvalSummary`;
- `operationId` and `expiresAtUtc`;
- the existing session context and hidden-session warning.

The response guidance tells the client to wait for the human decision and then
repeat the exact call with `confirm: true` and the returned `operationId`.
The second call never executes while the operation is pending or denied.

Summaries are made from safe identifiers only. Remove operations show the
action and target. Redeploy shows the web container, replacement image, and
counts or presence flags for optional settings. It never includes environment
values, tar data, or complete volume mount strings. User-controlled display
values are trimmed, control characters are removed, and long values are
bounded.

### WinUI approval coordinator

The App registers a singleton
`McpDestructiveApprovalCoordinator`. After `MainWindow` is created and
activated, App starts the coordinator with the window dispatcher and XamlRoot
provider. The coordinator subscribes to the service policy event without
adding a reverse project dependency.

The event handler marshals to the UI dispatcher. It verifies that the
dispatcher and XamlRoot are available, then uses the existing
`IDialogService.ShowConfirmAsync` with **Allow** and **Deny** buttons. The
dialog shows the safe action, target, session name, visible/hidden and
admin/non-admin context, any hidden-session warning, and the expiry time.

Only a Primary result calls `TryApprove`. A close, Deny result, missing UI
resource, dispatcher enqueue failure, or dialog exception calls `TryReject`
and writes a diagnostic log entry. The coordinator does not call the runtime.

### Settings and lifecycle

`McpDestructiveConfirmationEnabled` stays the master setting and defaults to
`true`. The existing startup environment override and Settings toggle continue
to set the policy. When disabled, the current explicit one-call automation
bypass remains in place. When enabled, both the human approval and the
`confirm` plus `operationId` token are required.

The service project does not reference the App project. A small optional
driver override in `ServiceHost.Build` is used only by integration tests so
the MCP protocol can be tested without requiring WSLC.

## Error handling

The policy rejects all invalid or incomplete approval states with a clear
reason in the MCP envelope. Expired records are removed after their rejection.
Approval is never inferred from a `confirm` flag alone. No UI subscriber,
missing XamlRoot, closed dialog, approval race, or handler failure can mark an
operation approved.

The existing session wrapper remains on confirmation and failure responses.
Successful destructive tool behavior is unchanged after policy consumption.

## Verification

Focused unit tests will cover:

- pending operations cannot be consumed;
- approved operations consume once;
- denied operations cannot be consumed;
- expiry, wrong tool, wrong arguments, and concurrent consumption;
- approval and rejection races;
- fail-closed behavior without a subscriber;
- safe summaries that omit environment values, tar data, and full mounts;
- the enabled and explicit disabled-setting paths.

Integration coverage will call the MCP endpoint with a test driver. It will
verify that the first call and a pre-approval confirmation cannot invoke the
driver, and that the same operation invokes it only after the test approval
handler calls `TryApprove`.

README and MCP tool descriptions will document the human decision as the
first step in the two-call protocol.
