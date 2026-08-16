# Container LAN access design

## Context

Issue #119 asks for access to a container from devices other than the endpoint running Wincontainer. The feature must be switchable for each container.

Wincontainer already stores run configuration for later recreation, parses published port links, exposes WSLC operations through `IWslcDriver`, and has a WinUI container detail page. WSLC published bindings are creation-time configuration, so changing access requires recreation. The existing Wincontainer API remote-access setting and Windows Firewall configuration are separate concerns and remain unchanged.

## Goals

- Provide one access toggle per container for all existing published ports.
- Keep containers local-only by default.
- Allow explicit LAN access by binding published host ports to `0.0.0.0`.
- Preserve image, name, volumes, environment, network, host ports, container ports, and protocols during recreation.
- Show usable LAN endpoint values for all detected non-loopback IPv4 addresses.
- Give a clear confirmation before enabling LAN access and clear errors when the operation fails.

## Non-goals

- Adding or editing port mappings.
- Changing the Wincontainer service API bind, API authorization, or firewall rules.
- Exposing a complete container network without published ports.
- Reconstructing containers that have no saved run configuration.

## Selected approach

Add a focused shared access service for binding conversion and stop/remove/recreate behavior. Extend the saved configuration and expose one authenticated API operation to the WinUI client. This keeps WSLC orchestration out of the page view model, makes conversion independently testable, and fits the current runtime-to-service-to-client architecture.

## Architecture

For each valid published binding, local-only mode uses `127.0.0.1:host:container/protocol`; LAN mode uses `0.0.0.0:host:container/protocol`. Host port, container port, and protocol remain unchanged. Absent host IPs are normalized. Unsupported or malformed bindings return a validation error and are not silently modified.

Extend `ContainerRunConfig` with `Network` and `AllowLocalNetworkAccess`. Missing fields deserialize as local-only defaults, so existing configuration files remain safe and compatible.

The access operation validates the target, ports, and saved configuration, converts all bindings, stops the container, removes it, recreates it with the same image, name, converted ports, volumes, environment, and network, then saves the updated state. Stop, remove, and run errors propagate as explicit failure results. The UI must state that recovery may be required if recreation fails after removal.

Add one authenticated service endpoint and matching client method. The endpoint does not change API remote access, service listening addresses, authentication, or firewall rules.

## UI behavior

Add one access toggle to the container detail page near published ports. Local-only is the safe default. Enabling requires confirmation that other local-network devices can reach the published ports; disabling acts immediately. The control is unavailable for no ports or missing saved configuration. Progress prevents repeat operations, errors use the existing detail-page error surface, and success refreshes container data.

Detect all usable non-loopback IPv4 addresses from active network interfaces. Show each address with the host port for each published port and provide copy actions. Use `http://` only for HTTP ports; do not guess protocols for other ports.

## Testing

Add focused tests for binding conversion, preservation of port details, malformed input, legacy configuration defaults, network/configuration preservation, no-port and missing-config responses, WSLC failure propagation, successful recreation, confirmation, unavailable UI states, endpoint generation, and error presentation. Use the existing Unit test project and Debug solution build.

## Scope boundary

This design covers issue #119 only. It does not add a port editor or change API and firewall controls.
