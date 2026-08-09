# Chunked MCP Image Upload

## Goal

Allow MCP clients that cannot send large JSON tool arguments to load large
container image tar archives without changing the existing `load_image` path
workflow.

## Scope

- Keep `load_image` for host paths and small base64 payloads.
- Add an upload lifecycle for large archives:
  `start_image_upload`, `upload_image_chunk`, and `finish_image_upload`.
- Keep each base64 chunk at or below 3 KB decoded so the JSON tool argument
  remains below the observed Copilot client limit.
- Limit the completed archive to 512 MB.
- Store upload data in a temporary file, not in process memory.
- Require ordered chunks and remove incomplete uploads after expiration.

## Architecture

Add an `ImageUploadStore` runtime service that owns upload IDs, temporary
files, expected sequence numbers, byte counts, and expiration. Register it as
a singleton beside `IWslcDriver`.

`start_image_upload` creates a random upload ID and an exclusive temporary
`.tar` file. `upload_image_chunk` validates the upload ID, exact next sequence
number, base64 data, and per-chunk/total limits before appending decoded bytes.
`finish_image_upload` closes the file, invokes the existing image-load command
against it, and deletes the upload state and file in all terminal paths.

The store is process-local because the MCP server already runs as one local
Wincontainer process and the transport is stateless only at the HTTP request
level. Each tool call carries the upload ID needed to recover the operation's
state.

## Error handling and cleanup

Invalid IDs, wrong sequence numbers, malformed base64, chunks over 3 KB
decoded, and totals over 512 MB return explicit validation errors without
invoking WSLC. Finish and cancellation paths always remove the temporary file.
A periodic cleanup check runs when upload tools are called and removes uploads
that have been inactive for 15 minutes. Cleanup failures are surfaced through
the existing runtime logging pattern and do not hide the primary operation
error.

## Testing

- Unit-test upload ID creation, sequence enforcement, base64 validation,
  per-chunk and total limits, expiration, and file cleanup.
- Test that a completed upload delegates its temporary `.tar` path to the
  existing WSLC load operation.
- Extend MCP integration tool discovery to require all three upload tools.
- Add an integration test that sends several small chunks and verifies the
  final WSLC call path without requiring a large JSON request.

## Non-goals

- Raising the Copilot client's JSON argument limit.
- Replacing `tarPath` mode.
- Supporting resumable uploads across application restarts.
- Adding a browser or WinUI upload page.
