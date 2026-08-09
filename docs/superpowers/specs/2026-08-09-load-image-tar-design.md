# Load Local Image Tar Through MCP

## Goal

Add an MCP tool that loads a local container image tar archive into the WSLC
image store. The tool supports both a path on the Wincontainer host and
base64-encoded tar data for clients that cannot provide a host path.

## Scope

- Add one `load_image` MCP tool.
- Accept exactly one of `tarPath` and `tarData`.
- Accept existing `.tar` files for path input.
- Accept base64-encoded tar data up to 512 MB after decoding.
- Use the existing WSLC runtime and process execution path.
- Do not add another container runtime or a separate HTTP upload endpoint.

## Architecture

`IWslcDriver` will expose `LoadImageAsync`. `WslcCommands` will generate the
WSLC command `image load --input <path>`. `WincontainerTools.LoadImage` will
validate the MCP arguments and delegate to the driver.

For `tarPath`, the driver passes the validated path to WSLC. For `tarData`, the
driver creates a uniquely named temporary `.tar` file under the system
temporary directory, writes the decoded bytes, runs WSLC, and deletes the file
in a `finally` block. The existing `WslcDriver` command execution and error
format remain in use.

## Validation and errors

The tool returns a clear validation error without invoking WSLC when:

- both inputs are missing;
- both inputs are provided;
- the path is not an existing file;
- the path does not end in `.tar`;
- base64 data is invalid; or
- decoded data is larger than 512 MB.

WSLC failures use the current `wslc error (<exit code>): ...` result format.
Cancellation continues to use the supplied `CancellationToken`.

The host path is intentionally not restricted to one directory because MCP
clients need to load archives selected from normal user locations. The
existing MCP bearer-token and loopback authorization rules protect access to
the service.

## Testing

- Unit-test `WslcCommands.ImageLoad`.
- Extend the runtime contract tests for `IWslcDriver.LoadImageAsync`.
- Test MCP input validation, including exclusive inputs, extension, file
  existence, malformed base64, and the 512 MB limit.
- Test temporary archive cleanup for base64 input.
- Extend the MCP integration tool-list test to require `load_image`.

## Non-goals

- Support for `.tar.gz`, `.tgz`, or arbitrary file extensions.
- Browser or WinUI upload controls.
- Progress streaming for the load operation.
- Image import with an optional tag; this feature loads Docker/OCI image
  archives using WSLC's `image load` command.
