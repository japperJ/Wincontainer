WinContainers LXC — Implementation Plan
Phase 1: Core / Contracts (WinContainers.Core)
1.1 Add IContainerParser interface
- New file: WinContainers.Core/Contracts/IContainerParser.cs
- Methods: List<ContainerCardData> ParseContainers(string rawOutput), List<FileEntryData> ParseFiles(string rawOutput)
1.2 Add RuntimeType enum
- New file: WinContainers.Core/Models/RuntimeType.cs
- Values: Nerdctl, Lxc
Phase 2: Driver & Scripts (WinContainers.Scripts)
2.1 Create Scripts/Lxc/ directory structure
- New directory alongside existing Scripts/ (or in a subpath the ScriptProvider can target)
- ScriptManifest.Lxc.json — all LXC commands using wsl -u root -d Ubuntu-LXC lxc ...
- Get-Version → wsl -d Ubuntu-LXC lxc --version
- Get-Container → wsl -d Ubuntu-LXC lxc list --format json
- Create-Container → wsl -d Ubuntu-LXC lxc init {image} {name}
- Start-Container → wsl -d Ubuntu-LXC lxc start {name}
- Stop-Container → wsl -d Ubuntu-LXC lxc stop {name}
- Remove-Container → wsl -d Ubuntu-LXC lxc delete {name}
- Get-ContainerLogs → wsl -d Ubuntu-LXC lxc info {name} --show-log
- Exec-Container → wsl -d Ubuntu-LXC lxc exec {name} -- {command}
- List-ContainerFiles → wsl -d Ubuntu-LXC lxc file list {name}/{path}
- Read-ContainerFile → wsl -d Ubuntu-LXC lxc file pull {name}/{path} -
- Write-ContainerFile → wsl -d Ubuntu-LXC lxc file push - {name}/{path}
- List-Images → wsl -d Ubuntu-LXC lxc image list images: --format json
- Pull-Image → wsl -d Ubuntu-LXC lxc image copy {remote}:{image} local:
- Auto-start/health scripts
2.2 Refactor RuntimeDriver into base + subclass
- Either: make RuntimeDriver abstract-ish with a virtual method that returns the WSL distro name + command prefix
- Or: keep RuntimeDriver as-is, create LxcDriver : IRuntimeDriver that delegates to a RuntimeDriver instance with its own ScriptProvider
- Simpler approach: make RuntimeDriver accept the manifest directory as a constructor parameter alongside a distro name. A LxcDriver factory class creates the right RuntimeDriver with LXC config.
2.3 Add LXC .ps1 scripts as needed
- Only for LXC operations that need complex logic (e.g., Create-Container.ps1 for parsing image selection into lxc launch vs lxc init options)
- Most LXC commands can stay in manifest-only (no separate .ps1 file)
Phase 3: Service Layer (WinContainers.Service)
3.1 Register both drivers in DI (ServiceHost.cs)
builder.Services.AddKeyedSingleton<IRuntimeDriver>("nerdctl", sp => {
    // existing logic, pointing to Scripts/Nerdctl/
    return new RuntimeDriver(new ScriptProvider(nerdctlDir));
});
builder.Services.AddKeyedSingleton<IRuntimeDriver>("lxc", sp => {
    return new RuntimeDriver(new ScriptProvider(lxcDir));
});
3.2 Dual-routing for API endpoints
- POST /api/runtime/nerdctl/execute/{scriptName} → routes to keyed service "nerdctl"
- POST /api/runtime/lxc/execute/{scriptName} → routes to keyed service "lxc"
- Existing endpoints (/api/containers/{id}/...) get a ?runtime=lxc query param
3.3 Unified health endpoint (GET /api/health)
- Calls both GetVersionAsync() and returns:
{
  "nerdctlAvailable": true, "nerdctlVersion": "2.0",
  "lxcAvailable": true, "lxcVersion": "6.0.2",
  "lxcDistro": "Ubuntu-LXC",
  "lxcDistroInstalled": true
}
3.4 Keepalive
- Keep pinging nerdtl driver only (same WSL2 VM keeps both distros alive)
3.5 LXC container file endpoints adapters
- Existing /api/containers/{id}/files/* routes check for ?runtime=lxc and route to LXC driver's file scripts
Phase 4: App Services (WinContainers.App/Services)
4.1 Add RuntimeType to ServiceHostStarter
- RunScriptAsync gets an overload with runtime parameter:
- RunScriptAsync("nerdctl", "Start-Container", params) routes to /api/runtime/nerdctl/execute/Start-Container
- RunScriptAsync("lxc", "Start-Container", params) routes to /api/runtime/lxc/execute/Start-Container
4.2 Add LxcContainerParser implementing IContainerParser
- Parses LXC JSON format: name, status (Running/Stopped/Frozen), config, state.memory, snapshots, created_at
- Maps LXC statuses to display statuses: Frozen → Paused, Running → Running, Stopped → Exited (0)
- Adds Runtime = "lxc" badge to each ContainerCardData
4.3 Add Runtime property to ContainerCardData
- New property: string Runtime { get; set; } = "nerdctl" (default for backward compat)
- Used for runtime badge in UI, routing decisions
4.4 Update ContainerService to handle both runtimes
- Or: ContainersViewModel calls both NerdctlContainerParser and LxcContainerParser separately
4.5 Add image browser service (optional)
- LxcImageService — queries images.linuxcontainers.org simplestreams index for available distros
- Returns categorized list of (distro, version, variant) for the create dialog
Phase 5: ViewModels (WinContainers.App/ViewModels)
5.1 ContainersViewModel — parallel polling
- In RefreshAsync(), fire two parallel requests:
- ServiceHostStarter.RunScriptAsync("nerdctl", "Get-Container", ...)
- ServiceHostStarter.RunScriptAsync("lxc", "Get-Container", ...)
- Parse each with the respective parser
- Merge results with a Runtime badge on each card
- Rebuild grouped list as before
5.2 ContainerDetailViewModel — runtime-conditional sections
- Show/hide "Image" label for nerdctl only
- Show/hide "Snapshots" section for LXC only
- Show/hide "Port Mapping" section for nerdctl only
- Add LXC-specific network proxy device section
- File operations pass runtime in the query parameter
5.3 QuickActionsViewModel — LXC creation support
- Runtime picker at top of create dialog
- LXC mode: hide image search/port/volume/env inputs
- LXC mode: show distro browser, container name, resource limits fields
- "Start after creation" checkbox
5.4 SettingsViewModel — LXC runtime status
- Add LxcAvailable, LxcVersion, LxcInstalling observable properties
- "Install LXC Runtime" button → triggers automated setup
- Poll both runtimes for status
5.5 TerminalViewModel — LXC command templates
- Add a second set of command templates keyed by runtime
- When LXC container is selected in dropdown, use LXC templates
- lxc exec {name} -- {command} instead of nerdctl exec {id} {command}
Phase 6: UI / Pages (WinContainers.App/Pages)
6.1 Container list — runtime badge
- ContainersControl.xaml: Add an icon/badge next to each container showing "Docker" or "LXC"
- Use colored badge (blue for Docker, orange for LXC)
6.2 Create dialog — runtime picker + LXC form
- QuickActionsControl.xaml: Add RadioButtons at top of create section
- LXC mode: show distro browser (tree/list of available images) with search
- LXC mode: text field for manual image alias entry
- LXC mode: show CPU/memory limit sliders (optional)
6.3 Container detail — conditional sections
- ContainerDetailPage.xaml: Add runtime badge at top
- Conditionally show/hide XAML sections based on Runtime binding:
- Port links only for nerdctl
- Image info only for nerdctl
- Snapshot management for LXC
- Network proxy device management for LXC
6.4 Settings page — two runtime cards
- SettingsPage.xaml: Two Card elements side by side
- "Docker Runtime" card: status, version, nerdctl info
- "LXC Runtime" card: status, version, WSL2 distro info, Install/Configure button
Phase 7: Setup / Automation (Ubuntu-LXC WSL2 Distro)
7.1 Distro creation
- wsl --install -d Ubuntu-LXC — installs Ubuntu in a named WSL2 distro
- Or: download Ubuntu 24.04 rootfs tarball and wsl --import Ubuntu-LXC <path> <tarball>
7.2 LXC installation inside distro
- wsl -d Ubuntu-LXC apt update && apt install -y lxd
- wsl -d Ubuntu-LXC sudo lxd init --minimal
- Add current user to lxd group
7.3 AppArmor configuration
- Write kernelCommandLine = lsm=apparmor,landlock,lockdown,yama,loadpin,safesetid,integrity,selinux,tomoyo to %UserProfile%\.wslconfig
- Add none /sys/kernel/security securityfs defaults 0 0 to /etc/fstab inside distro
- Notify user to restart WSL
7.4 "Install LXC Runtime" wizard in app
- Step 1: Install/verify WSL2 distro
- Step 2: Install LXD packages
- Step 3: Initialize LXD
- Step 4: Configure AppArmor
- Step 5: Verify installation (run lxc --version), show success
Phase 8: Tests
8.1 Unit tests
- LxcContainerParser tests — parse sample LXC JSON output
- RuntimeDriver LXC config — manifest loading, parameter substitution
- ServiceHostStarter routing — verify URL construction with runtime segment
8.2 Integration tests
- Verify both drivers can be registered in DI
- Verify health endpoint returns both runtime statuses
File Change Summary
Layer	Files to Create
Core	IContainerParser.cs, RuntimeType.cs
Scripts	Scripts/Lxc/ScriptManifest.Lxc.json
Service	—
App Services	LxcContainerParser.cs
ViewModels	—
Pages	—
Setup	LxcSetupService.cs
Tests	LxcContainerParserTests.cs, LxcRuntimeTests.cs