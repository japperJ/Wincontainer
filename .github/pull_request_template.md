## Why
Issue #102 is about making Wincontainer MCP safer for deploy work. The main risk is hidden-session deploys and stale redeploys that look successful but do not affect the visible admin session or the running app.

## What changed
- Added session-aware `load_image` support and clearer deployment guidance in the Wincontainer skill.
- Exposed `load_image` through the MCP surface with tar-path and base64-tar handling.
- Added chunked image upload support and safer temp-file handling in the runtime.
- Updated docs and tests around the new image import flow.

## Notes
- The current branch does not yet cover every #102 item. The remaining work is the broader MCP safety and redeploy flow from the issue text.
- I did not add visuals.

## Testing
- `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests|FullyQualifiedName~ContainerAgentTests"`
- `dotnet test tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~ServiceHost_ShouldExposeMcpToolsForAuthorizedRequests"`
- `dotnet build WinContainers.slnx -c Debug --nologo -v q`
