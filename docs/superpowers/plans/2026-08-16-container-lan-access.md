# Container LAN Access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow each container with saved published-port configuration to switch between local-only and local-network access while preserving its run configuration.

**Architecture:** Keep port conversion and stop/remove/recreate orchestration in a focused runtime service. Expose one authenticated service endpoint and client method, then keep confirmation, endpoint display, and UI state in the container detail view model/page. Extend the existing JSON configuration store with backward-compatible network and access fields.

**Tech Stack:** C#/.NET 10, ASP.NET Core minimal APIs, WSLC through `IWslcDriver`, WinUI 3/XAML, CommunityToolkit.Mvvm, xUnit/FluentAssertions.

---

## File map

- Create `src/WinContainers.Runtime/ContainerAccessService.cs`: validate access requests, convert bindings, recreate containers, and return explicit results.
- Create `src/WinContainers.Runtime/PortBindingConverter.cs`: parse and normalize published bindings without WSLC or UI dependencies.
- Modify `src/WinContainers.Runtime/ContainerConfigStore.cs`: persist `Network` and `AllowLocalNetworkAccess`, with safe defaults for old JSON.
- Modify `src/WinContainers.Runtime/IWslcDriver.cs` and `src/WinContainers.Runtime/WslcDriver.cs`: expose the service's required operations through existing WSLC methods only.
- Modify `src/WinContainers.Service/Host/ServiceHost.cs`: register the access service and map the authenticated access endpoint.
- Modify `src/WinContainers.App/Services/IWslcServiceClient.cs` and `WslcServiceClient.cs`: add the access request.
- Modify `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs`: expose access state, endpoint data, progress, confirmation request, and errors.
- Modify `src/WinContainers.App/Pages/ContainerDetailPage.xaml` and `.xaml.cs`: render the toggle, endpoint list, copy action, and confirmation dialog.
- Modify all existing `ContainerRunConfig` save sites in `QuickActionsViewModel.cs` and `ImagesViewModel.cs`: save network and local-only state with the actual bindings.
- Add focused tests under `tests/WinContainers.Tests.Unit` for conversion, persistence, service orchestration, endpoint generation, and view-model state.
- Add or extend service integration tests only if the existing test host can inject the driver and exercise the new endpoint.

### Task 1: Add failing port conversion tests

**Files:**
- Create: `tests/WinContainers.Tests.Unit/PortBindingConverterTests.cs`
- Test target: `src/WinContainers.Runtime/PortBindingConverter.cs`

- [ ] **Step 1: Write tests for supported forms and preservation.**

Cover `8080:80/tcp`, `127.0.0.1:8080->80/tcp`, `0.0.0.0:8080->80/tcp`, and comma-separated bindings. Assert local mode emits `127.0.0.1`, LAN mode emits `0.0.0.0`, and host/container ports plus protocol remain unchanged.

- [ ] **Step 2: Write rejection tests.**

Assert malformed entries, missing ports, invalid numeric ranges, and unsupported host addresses return a failure result with a non-empty error. Assert no partial list is returned on failure.

- [ ] **Step 3: Run the focused tests.**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~PortBindingConverterTests`

Expected: FAIL because `PortBindingConverter` does not exist.

### Task 2: Implement the binding converter

**Files:**
- Create: `src/WinContainers.Runtime/PortBindingConverter.cs`
- Modify: `src/WinContainers.Runtime/WinContainers.Runtime.csproj` only if required by project conventions
- Test: `tests/WinContainers.Tests.Unit/PortBindingConverterTests.cs`

- [ ] **Step 1: Add a typed conversion result.**

Use a result such as `PortBindingConversionResult(bool Success, IReadOnlyList<string> Bindings, string? Error)` so callers can surface validation errors without exceptions or silent fallback.

- [ ] **Step 2: Implement deterministic parsing and conversion.**

Accept the repository's WSLC/Docker-style published forms, normalize unqualified host bindings, preserve protocol, and replace only the host bind address. Reject malformed values before returning any converted bindings.

- [ ] **Step 3: Run the focused tests.**

Run the Task 1 command. Expected: PASS.

- [ ] **Step 4: Commit the converter.**

Run: `git add src/WinContainers.Runtime/PortBindingConverter.cs tests/WinContainers.Tests.Unit/PortBindingConverterTests.cs && git commit -m "feat: add container port binding conversion" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"`

### Task 3: Extend saved configuration safely

**Files:**
- Modify: `src/WinContainers.Runtime/ContainerConfigStore.cs`
- Modify: `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs`
- Modify: `src/WinContainers.App/ViewModels/ImagesViewModel.cs`
- Test: `tests/WinContainers.Tests.Unit/ContainerConfigStoreTests.cs`

- [ ] **Step 1: Add legacy-deserialization tests.**

Deserialize JSON with only `Image`, `Ports`, `Volumes`, and `Env`; assert `Network` is empty and `AllowLocalNetworkAccess` is `false`. Add a round-trip test for the new fields.

- [ ] **Step 2: Extend `ContainerRunConfig`.**

Add `public string? Network { get; init; }` and `public bool AllowLocalNetworkAccess { get; init; }`. Keep missing JSON values mapped to the safe defaults.

- [ ] **Step 3: Update save sites.**

When a container is created or recreated, save the actual port list, environment, volumes, network, and `AllowLocalNetworkAccess = false` unless the operation explicitly preserves an existing enabled state. Do not add a second config store.

- [ ] **Step 4: Run persistence tests.**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerConfigStoreTests`

Expected: PASS.

### Task 4: Add the runtime access service

**Files:**
- Create: `src/WinContainers.Runtime/ContainerAccessService.cs`
- Modify: `src/WinContainers.Runtime/IWslcDriver.cs` only if an existing operation is missing
- Modify: `src/WinContainers.Runtime/WslcDriver.cs` only if the interface change is required
- Test: `tests/WinContainers.Tests.Unit/ContainerAccessServiceTests.cs`

- [ ] **Step 1: Add fake-driver tests for guard conditions.**

Assert missing config returns an unavailable result without WSLC calls, no published ports returns `No published ports`, and malformed saved bindings return validation failure without stopping the container.

- [ ] **Step 2: Add fake-driver tests for successful recreation.**

Assert the service calls stop, remove, then run in order; passes the same image/name/volumes/environment/network and converted ports; saves the requested access state; and returns success.

- [ ] **Step 3: Add failure propagation tests.**

Make stop, remove, and run each return a WSLC error in separate tests. Assert the service returns failure and does not continue after the failing operation.

- [ ] **Step 4: Implement the service.**

Load config by container name, validate ports, convert them, call existing driver methods, and save only after successful recreation. Return explicit status/message/access state. Do not catch broad exceptions or report success after a failed WSLC command.

- [ ] **Step 5: Run service tests.**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerAccessServiceTests`

Expected: PASS.

- [ ] **Step 6: Commit runtime changes.**

Run: `git add src/WinContainers.Runtime tests/WinContainers.Tests.Unit && git commit -m "feat: add container LAN access orchestration" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"`

### Task 5: Wire the service endpoint and client

**Files:**
- Modify: `src/WinContainers.Service/Host/ServiceHost.cs`
- Modify: `src/WinContainers.App/Services/IWslcServiceClient.cs`
- Modify: `src/WinContainers.App/Services/WslcServiceClient.cs`
- Test: `tests/WinContainers.Tests.Unit/ServiceEndpointContractTests.cs` or the existing runtime contract test file

- [ ] **Step 1: Add the request/response contract.**

Use a request containing `ContainerId` and `AllowLocalNetworkAccess`, and return `Success`, `Message`, `AllowLocalNetworkAccess`, and converted `Ports`. Keep naming camel-case through the existing JSON options.

- [ ] **Step 2: Register and map the endpoint.**

Register `ContainerAccessService` with the existing driver and config store dependencies. Map `POST /api/containers/{id}/access` after the existing `/api` authorization middleware. Return a bad request for validation/unavailable results and an internal error only for unexpected failures.

- [ ] **Step 3: Add the client method.**

POST the request with existing authentication handling and parse the explicit response. Do not return a success-shaped string when the server reports failure.

- [ ] **Step 4: Add contract assertions and run tests.**

Assert the route, HTTP verb, request property names, and client payload. Run the focused unit test selector and the existing integration project if endpoint hosting is covered there.

- [ ] **Step 5: Commit API wiring.**

Run: `git add src/WinContainers.Service src/WinContainers.App/Services tests/WinContainers.Tests.Unit && git commit -m "feat: expose container LAN access endpoint" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"`

### Task 6: Add view-model access state and network endpoint discovery

**Files:**
- Modify: `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs`
- Modify: `src/WinContainers.App/Models/ContainerViewModel.cs` if the detail model needs access metadata
- Test: `tests/WinContainers.Tests.Unit/ContainerDetailAccessTests.cs`

- [ ] **Step 1: Add state tests.**

Test no ports and missing saved configuration as disabled states, local-only default, confirmation required only when enabling, progress state during the request, successful refresh state, and visible error state on failure.

- [ ] **Step 2: Add IPv4 endpoint tests.**

Use a testable provider or internal helper for active non-loopback IPv4 addresses. Assert all usable addresses are included, loopback/link-local addresses are excluded, and endpoint strings preserve host ports and protocol display rules.

- [ ] **Step 3: Implement view-model state.**

Add observable properties for `AllowLocalNetworkAccess`, `IsAccessChangeRunning`, `CanChangeAccess`, `AccessStatusText`, `AccessEndpoints`, and the existing error pattern. Add an async method that receives confirmation from the page before enabling, calls the client, and reloads container data after success.

- [ ] **Step 4: Implement endpoint discovery.**

Read active network interfaces with `System.Net.NetworkInformation`, select non-loopback IPv4 addresses, and keep the provider isolated so tests do not depend on the machine's network state.

- [ ] **Step 5: Run view-model tests.**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerDetailAccessTests`

Expected: PASS.

### Task 7: Add the WinUI toggle, confirmation, and copy actions

**Files:**
- Modify: `src/WinContainers.App/Pages/ContainerDetailPage.xaml`
- Modify: `src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs`
- Modify: `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs`
- Test: existing UI source contract tests and `tests/WinContainers.Tests.Ui` only if the existing runner supports this page

- [ ] **Step 1: Add the access panel in XAML.**

Place the toggle beside published-port information. Bind enabled state and access state with `x:Bind`; show `No published ports` or missing-config text when the control is unavailable; render endpoint rows with copy buttons; bind progress and error text.

- [ ] **Step 2: Add code-behind handlers.**

Handle the toggle event, revert the toggle while confirmation is pending or declined, show a `ContentDialog` only when enabling, call the view model, and handle endpoint copy with the existing WinUI clipboard pattern.

- [ ] **Step 3: Preserve page lifecycle behavior.**

Initialize access state when `LoadContainer` runs, refresh it after successful recreation, and cancel or ignore stale operations when the page navigates away. Do not change the existing logs timer or detail tabs.

- [ ] **Step 4: Run existing UI/source contract tests.**

Run the smallest existing UI test selector available. If no focused selector exists, run `dotnet test tests/WinContainers.Tests.Ui/WinContainers.Tests.Ui.csproj -c Debug --nologo -v q`.

- [ ] **Step 5: Commit UI changes.**

Run: `git add src/WinContainers.App tests/WinContainers.Tests.Ui && git commit -m "feat: add container LAN access controls" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"`

### Task 8: Final validation and documentation check

**Files:**
- Modify: `docs/superpowers/specs/2026-08-16-container-lan-access-design.md` only if implementation decisions require correction
- Modify: `README.md` only if the user-facing feature needs documentation beyond the detail-page UI

- [ ] **Step 1: Run the full existing unit test project.**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: PASS.

- [ ] **Step 2: Run the Debug solution build.**

Run: `dotnet build WinContainers.slnx -c Debug --nologo -v q`

Expected: Build succeeds with warnings treated as errors.

- [ ] **Step 3: Review the diff against issue #119.**

Run: `git --no-pager diff master...HEAD --stat && git --no-pager diff master...HEAD --check`

Confirm the diff contains only the approved container access feature and design/plan documentation, with no API bind or firewall changes.

- [ ] **Step 4: Commit any documentation correction.**

Run only if needed: `git add docs README.md && git commit -m "docs: document container LAN access" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"`
