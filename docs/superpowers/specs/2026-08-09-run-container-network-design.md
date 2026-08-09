# Run Container Network Selection

## Goal

Allow callers of the Wincontainer container-run operation to attach a new container
to a named WSLC network. This is required for containers that must communicate with
services on a user-created network.

## Design

Add an optional `network` parameter to the existing run-container flow:

- `WslcCommands.Run` adds `--network <name>` when a non-empty network is provided.
- `IWslcDriver.RunContainerAsync` and `WslcDriver` pass the value through.
- MCP `run_container` exposes the parameter with a clear description.
- The REST request and AI tool expose the same optional parameter.
- Existing callers that omit the value keep the WSLC default network behavior.

Network values use the existing command quoting helper. The driver does not create
or validate networks; callers can use the existing network list and create tools
first. WSLC remains responsible for reporting an unknown network.

## Verification

Unit tests will verify command construction, MCP delegation, and that omitted or
blank network values do not add a flag. Existing unit and integration tests will
also be run.
