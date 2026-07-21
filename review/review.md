# Deep Code Review — WinContainers

**Scope:** Full application — `src/` (App, Core, Runtime, Service, BuildTasks), `tests/` (Unit, Integration, Playwright, Ui), `tools/`.
**Date:** 2026-07-21
**Reviewer:** Automated deep review

---

## 1. Summary

WinContainers is a .NET 10 WinUI 3 desktop application that manages Windows Subsystem for Linux Containers (WSLC) via an in-process Kestrel API. The architecture is layered: `App` (WinUI UI) → `Service` (Kestrel REST API) → `Runtime` (WslcDriver process execution) → `Core` (shared commands/models).

The codebase shows strong architectural discipline (single runtime abstraction, MVVM pattern, layered separation) but has several critical, high, and medium severity issues across security, error handling, concurrency, and code quality.

**Severity counts:**
- Critical: 5
- High: 12
- Medium: 18
- Low: 12

---

## 2. Critical Issues

### CRITICAL-01: Command injection via shell interpolation in `WslcDriver`

**File:** `src/WinContainers.Runtime/WslcDriver.cs:114-123`

**Problem:** `RunAndCaptureAsync` passes arguments to `wslc.exe` via `ProcessStartInfo` with `UseShellExecute = false`, which is safe. However, `WslcCommands.ContainerExecShell` (line 29-33) constructs a shell command string by interpolating user input:

```csharp
public static string ContainerExecShell(string id, string shellCommand, string shell = "sh")
{
    var escaped = shellCommand.Replace("\"", "\\\"");
    return $"container exec {Quote(id)} {Quote(shell)} -c \"{escaped}\"";
}
```

The `escaped` variable only escapes double quotes but does not prevent shell metacharacter injection. A user can inject arbitrary shell commands via the shell input box in `ContainerDetailPage.xaml.cs` (line 213). The `ContainerExecCommand` method (line 28) has the same issue — it passes `command` directly without any sanitization:

```csharp
public static string ContainerExecCommand(string id, string command) => $"container exec {Quote(id)} {command}";
```

**Impact:** Arbitrary command execution on the host via the shell tab.

**Fix:** Use argument arrays instead of string interpolation. Pass the command as a single argument to `wslc` rather than constructing a shell `-c` string. If shell execution is required, use `sh -c` with proper argument escaping via `System.CommandLine` or manual escaping of all shell metacharacters (`$`, `` ` ``, `\`, `!`, `;`, `&`, `|`, `(`, `)`, `<`, `>`, newlines).

---

### CRITICAL-02: WebView2 XSS via JSON injection in inspect viewer

**File:** `src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs:183-187`

**Problem:** The inspect JSON is injected into a WebView2 via `ExecuteScriptAsync` with only `EncodeJsonForWebView2` (line 805-812 of `ContainerDetailViewModel.cs`), which serializes the JSON string and strips the outer quotes. This does not properly escape single quotes within the JSON content:

```csharp
public static string EncodeJsonForWebView2(string json)
{
    if (string.IsNullOrEmpty(json))
        return "null";
    var encoded = JsonSerializer.Serialize(json);
    return encoded.Substring(1, encoded.Length - 2);
}
```

`JsonSerializer.Serialize` produces a JSON string that escapes `"` and `\` but NOT single quotes. The script injection is `setJson('{encoded}')`. If the JSON content contains a single quote (e.g., a container name like `O'Brien`), it breaks out of the JavaScript string literal, enabling script injection.

**Impact:** XSS in the inspect viewer tab.

**Fix:** Use `ExecuteScriptAsync` with a JSON-serialized argument array instead of string interpolation: `ExecuteScriptAsync($"setJson({jsonEncoded})")` where `jsonEncoded` is the full JSON serialization (not stripped). Alternatively, escape single quotes in the encoded string.

---

### CRITICAL-03: Hardcoded WSL download URL and hash in `OnboardingViewModel`

**File:** `src/WinContainers.App/ViewModels/OnboardingViewModel.cs:256-267`

**Problem:** The WSLC installer URL and SHA256 hash are hardcoded:

```csharp
"$url = 'https://github.com/microsoft/WSL/releases/download/2.9.4/wsl.2.9.4.0.x64.msi'; " +
"$expected = '826D71865B3A45BEE03B8D9BD100D7217DD7389761D75AFA7C77106EAC5CD78E'; "
```

This version is pinned to WSL 2.9.4. When Microsoft releases a newer version, the hash will be stale and installation will fail. More critically, if the GitHub release is compromised or the URL changes, the user is stuck.

**Impact:** Installation failures, potential supply chain risk if the hardcoded URL is not updated.

**Fix:** Fetch the latest release URL dynamically from the GitHub API (similar to `WslcUpdateService` which already does this correctly). Remove the hardcoded hash or fetch it from the API response.

---

### CRITICAL-04: No cancellation token propagation in `App.xaml.cs` background tasks

**File:** `src/WinContainers.App/App.xaml.cs:70-103`

**Problem:** Two background tasks are started with `Task.Run` but have no cancellation mechanism:

```csharp
_ = Task.Run(async () =>
{
    try
    {
        await UpdateService.CheckForUpdatesAsync();
    }
    catch (Exception ex) { ... }
});

_ = Task.Run(() =>
{
    try
    {
        var settingsService = Services.GetRequiredService<AppSettingsService>();
        ...
        ServiceHost.Build([], OutputService.Instance).Run();
    }
    catch (Exception ex) { ... }
});
```

The `ServiceHost.Build().Run()` call blocks indefinitely. When the application shuts down, these tasks are not cancelled, potentially causing the process to hang on exit. The `Run()` method starts Kestrel which listens on a port — if the process doesn't shut down cleanly, the port remains bound.

**Impact:** Application hang on shutdown, port conflicts on restart.

**Fix:** Use a `CancellationTokenSource` that is cancelled in `OnExiting` or `OnUnhandledException`. Pass the token to `ServiceHost.Build().RunAsync(token)` instead of `Run()`.

---

### CRITICAL-05: `HttpClient` instances never disposed in `WslcServiceClient` and `QuickActionsViewModel`

**File:** `src/WinContainers.App/Services/WslcServiceClient.cs:12` and `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs:97`

**Problem:** `WslcServiceClient` creates a `new HttpClient()` and never disposes it. `QuickActionsViewModel` has a `static readonly HttpClient DockerHubClient = new()` which is shared but the class itself doesn't implement `IDisposable` to dispose it. While `HttpClient` is designed for reuse, the `WslcServiceClient` instance is created in `App.xaml.cs` and never disposed, and the `QuickActionsViewModel` is registered as a singleton — the `HttpClient` lives for the entire application lifetime, which is actually the recommended pattern, but the lack of explicit disposal documentation or pattern is concerning.

The real issue is that `WslcServiceClient._http` has no timeout configured. If the API server hangs, requests will hang indefinitely.

**Impact:** Socket exhaustion under certain conditions, indefinite hangs on unresponsive API.

**Fix:** Configure `HttpClient.Timeout` on the `WslcServiceClient._http` instance. Consider using `IHttpClientFactory` for proper lifetime management.

---

## 3. High Issues

### HIGH-01: `ViewModelBase.OnPropertyChanged` causes infinite recursion risk

**File:** `src/WinContainers.App/ViewModels/ViewModelBase.cs:10-16`

**Problem:** The override dispatches `OnPropertyChanged` to the UI thread if not already on it. However, if `App.DispatcherQueue` is null (which can happen during early startup or shutdown), this will throw a `NullReferenceException`. Additionally, the recursive dispatch pattern can cause issues if the dispatcher is shutting down.

```csharp
protected override void OnPropertyChanged(PropertyChangedEventArgs e)
{
    if (!App.DispatcherQueue.HasThreadAccess)
        App.DispatcherQueue.TryEnqueue(() => base.OnPropertyChanged(e));
    else
        base.OnPropertyChanged(e);
}
```

**Impact:** NullReferenceException crashes during startup/shutdown.

**Fix:** Add null check for `App.DispatcherQueue` and fall back to direct invocation.

---

### HIGH-02: Race condition in `ContainersViewModel.RebuildGroupedList`

**File:** `src/WinContainers.App/ViewModels/ContainersViewModel.cs:128-183`

**Problem:** `RebuildGroupedList` is called from `App.DispatcherQueue.TryEnqueue` (line 116-120) after `RefreshAsync`. However, `ContainerItems` is an `ObservableCollection<object>` that is directly modified (Clear/Add/Insert) in `ToggleGroupExpanded` (line 185-210) without any synchronization. If a poll refresh occurs while the user is expanding/collapsing groups, the `IndexOf` calls and `RemoveAt`/`Insert` operations can operate on a stale collection state.

**Impact:** UI corruption, index-out-of-range exceptions.

**Fix:** Serialize access to `ContainerItems` modifications. Use a lock or ensure all modifications happen on the UI thread with proper sequencing.

---

### HIGH-03: `ContainerDetailPage` subscribes to `PropertyChanged` but never unsubscribes

**File:** `src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs:73-83`

**Problem:** In `OnNavigatedTo`, the page subscribes to `_viewModel.PropertyChanged`:

```csharp
_viewModel.LoadContainer(data);

_viewModel.PropertyChanged += async (s, e) =>
{
    if (e.PropertyName == nameof(ContainerDetailViewModel.InspectJson))
    {
        ...
    }
};
```

Since `ContainerDetailViewModel` is registered as `Transient` in the DI container (line 53 of `App.xaml.cs`), a new instance is created each time the page is navigated to. However, the subscription is never removed in `OnNavigatedFrom`. If the same ViewModel instance is reused (which can happen with navigation caching), this creates duplicate subscriptions.

**Impact:** Memory leak, duplicate WebView2 initialization.

**Fix:** Unsubscribe in `OnNavigatedFrom` or use a weak event pattern.

---

### HIGH-04: `WslcDriver.RunAsync` does not dispose `Process` on timeout

**File:** `src/WinContainers.Runtime/WslcDriver.cs:125-154`

**Problem:** When a timeout occurs, `TryKill(process)` is called, but the `using` statement disposes the process. However, `WaitForExitAsync` is called with the linked cancellation token, and when the timeout fires, the `OperationCanceledException` is caught. But there's a subtle issue: the `stdoutTask` and `stderrTask` are started before the timeout check, and if the process is killed, these tasks may not complete before the `using` disposes the process, potentially causing issues with the redirected streams.

More critically, on timeout, the method returns `RunResult(-1, string.Empty, $"Command timed out after {timeoutMs}ms.")` but does not wait for the `stdoutTask` and `stderrTask` to complete. This can leave the process's stdout/stderr pipes in an inconsistent state.

**Impact:** Resource leaks, potential deadlocks on process cleanup.

**Fix:** After killing the process, await `stdoutTask` and `stderrTask` with a short timeout before returning.

---

### HIGH-05: `QuickActionsViewModel` has a 1348-line file with mixed concerns

**File:** `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs`

**Problem:** This file contains:
- Docker Hub search logic
- Compose YAML parsing (including docker run command parsing)
- Conflict detection (port, volume, name conflicts)
- Container creation/management
- Template catalog management
- Image search/debouncing

This violates the Single Responsibility Principle. The file is too large to maintain effectively and has 15+ private helper methods.

**Impact:** Maintainability issues, difficulty testing, high cognitive load.

**Fix:** Split into separate services: `ComposeParserService`, `ConflictDetectorService`, `DockerHubSearchService`, `TemplateCatalogService` (already exists but is thin).

---

### HIGH-06: `BearerTokenValidator.IsAuthorized` uses ordinal comparison for tokens

**File:** `src/WinContainers.Core/Models/BearerTokenValidator.cs:15`

**Problem:** Token comparison uses `StringComparison.Ordinal`, which is correct for security (prevents timing attacks via non-constant-time comparison), but the method does not use `CryptographicOperations.FixedTimeEquals` for constant-time comparison. While ordinal comparison is generally safe for this use case, using `FixedTimeEquals` would be more robust.

**Impact:** Potential timing attack vector (low probability but worth fixing).

**Fix:** Use `CryptographicOperations.FixedTimeEquals` for token comparison.

---

### HIGH-07: `ServiceHost` does not validate route parameters

**File:** `src/WinContainers.Service/Host/ServiceHost.cs:125-185`

**Problem:** Route parameters like `{id}` are passed directly to `WslcDriver` methods without validation. For example, `RemoveContainerAsync(id, ct)` passes `id` to `WslcCommands.ContainerRemove(id)` which calls `Quote(id)`. While `Quote` handles spaces, it does not prevent path traversal or other injection if the `id` contains special characters that could be interpreted by `wslc`.

**Impact:** Potential command injection if `wslc` interprets special characters in container IDs.

**Fix:** Validate that `id` matches expected patterns (alphanumeric, hyphens, underscores) before passing to the driver.

---

### HIGH-08: `ContainerDetailViewModel.WriteFileViaStdin` uses base64 encoding but the script is vulnerable to injection

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:668-675`

**Problem:** The method constructs a shell script that uses `printf` and `base64 -d`:

```csharp
var script = $"printf '%s' '{encodedContent}' | base64 -d > {ShellQuote(filePath)}";
```

While `ShellQuote` properly escapes the file path, the `encodedContent` is base64-encoded which is safe. However, the `ShellQuote` method (line 677) uses single-quote escaping which is correct, but the overall approach of writing files via shell is fragile. If the container doesn't have `base64` installed, this will fail silently.

**Impact:** File write failures on minimal containers.

**Fix:** Use `wslc cp` or a direct file API if available. Alternatively, validate that `base64` is available before using it.

---

### HIGH-09: `OnboardingViewModel.RunElevatedCommandAsync` writes scripts to temp files without cleanup on crash

**File:** `src/WinContainers.App/ViewModels/OnboardingViewModel.cs:424-504`

**Problem:** The method creates three temp files (script, launcher, log) and cleans them up in a `finally` block. However, if the process crashes or is killed during execution, these files are left behind. Over time, this can accumulate many temp files.

**Impact:** Disk space waste, temp directory pollution.

**Fix:** Use a unique temp directory per invocation and clean it up with a `try/finally` that also handles process crashes via a cleanup routine on startup.

---

### HIGH-10: `MainWindow.xaml.cs` has a 396-line code-behind with UI logic mixed with process management

**File:** `src/WinContainers.App/MainWindow.xaml.cs`

**Problem:** The code-behind contains:
- Window sizing and DPI handling
- Output pane management (drag/splitter logic)
- Navigation logic
- Help dialog with WSL status checking
- Process execution (`RunCommandAsync`)
- Output filtering

This violates separation of concerns. The `RunCommandAsync` and `GetWslStatusAsync` methods should be in a service.

**Impact:** Testability issues, maintainability.

**Fix:** Extract process execution and WSL status checking into an `IWslInfoService`. Move output pane management to a view model.

---

### HIGH-11: `ServiceHost` middleware order issue — auth middleware runs after request logging

**File:** `src/WinContainers.Service/Host/ServiceHost.cs:59-97`

**Problem:** The request logging middleware (line 59-74) runs before the auth middleware (line 76-97). This means unauthorized requests are logged, which is actually desirable for security auditing. However, the auth middleware checks `context.Request.Path.StartsWithSegments("/api")` and calls `next()` for non-API paths, but the logging middleware already ran for those paths. This is fine, but the order means that if the auth middleware throws, the request is already logged.

More critically, the auth check uses `context.Request.Headers.Authorization.ToString()` which can throw if the header is malformed. The `BearerTokenValidator.ExtractToken` method handles this, but the `ToString()` call on the header collection could behave unexpectedly.

**Impact:** Potential information disclosure via logs, edge case crashes.

**Fix:** Add error handling around the auth check. Consider using `AuthenticationHeaderValue.TryParse` instead of manual parsing.

---

### HIGH-12: `ContainerDetailViewModel.LoadFileListAsync` parses `ls -lap` output with fragile string splitting

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:566-587`

**Problem:** The method parses `ls -lap` output by splitting on spaces:

```csharp
var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
if (parts.Length < 8) continue;
var perms = parts[0];
var name = string.Join(" ", parts.Skip(8));
```

This is fragile because:
1. `ls` output format varies by locale and implementation
2. File names with spaces are handled by `Skip(8)`, but if the file has an ACL or extended attributes, the column count changes
3. The "total X" line is skipped, but other header lines may not be

**Impact:** Incorrect file listing, missing files.

**Fix:** Use `ls -la --time-style=long-iso --quoting-style=shell` and parse more robustly, or use a JSON-based approach if `wslc` supports it.

---

## 4. Medium Issues

### MEDIUM-01: `ContainersViewModel` uses `App.ServiceClient` static directly instead of DI

**File:** `src/WinContainers.App/ViewModels/ContainersViewModel.cs:105, 229, 237, 238, 288, 289, 290`

**Problem:** Multiple ViewModels access `App.ServiceClient` directly instead of receiving it via constructor injection. This makes the ViewModels untestable and creates tight coupling to the `App` class.

**Impact:** Testability issues, tight coupling.

**Fix:** Inject `WslcServiceClient` into ViewModels that need it.

---

### MEDIUM-02: `DashboardViewModel` is empty — dead code

**File:** `src/WinContainers.App/ViewModels/DashboardViewModel.cs:1-15`

**Problem:** The `DashboardViewModel` has only a constructor that stores dependencies but never uses them. It's registered in DI and resolved but has no properties or methods.

**Impact:** Dead code, confusion.

**Fix:** Remove the ViewModel or implement its intended functionality.

---

### MEDIUM-03: `ImagesViewModel.LoadImageDetailAsync` is async but never awaits

**File:** `src/WinContainers.App/ViewModels/ImagesViewModel.cs:92-102`

**Problem:** The method is declared `async` but doesn't contain any `await` calls:

```csharp
public async Task LoadImageDetailAsync(ImageEntryData image)
{
    SelectedImage = image;
    ShowDetail = true;
    StatusText = $"Loading details for {image.FullTag}...";
    InspectJson = "{}";
    Layers.Clear();
    StatusText = $"{image.FullTag} — {Layers.Count} layer(s)";
}
```

**Impact:** Unnecessary async overhead, compiler warning.

**Fix:** Remove `async` keyword or add actual async work (e.g., loading image inspect data).

---

### MEDIUM-04: `ImagesViewModel.LoadInspectAsync` is a no-op stub

**File:** `src/WinContainers.App/ViewModels/ImagesViewModel.cs:122-125`

**Problem:** The method returns a hardcoded JSON string:

```csharp
public async Task LoadInspectAsync(string imageId)
{
    InspectJson = "{\"info\": \"Inspect not available via WSLC API\"}";
}
```

This is dead functionality that provides no value.

**Impact:** Misleading UI, dead code.

**Fix:** Either implement real inspect functionality or remove the method and its UI binding.

---

### MEDIUM-05: `ServiceEndpointResolver.ResolveServiceProjectPath` has a hardcoded path

**File:** `src/WinContainers.Core/Models/ServiceEndpointResolver.cs:36-53`

**Problem:** The method searches for a `.worktrees/sprint1` path pattern, which is specific to the development environment:

```csharp
var candidate = Path.Combine(current.FullName, ".worktrees", "sprint1", "src", "WinContainers.Service", "WinContainers.Service.csproj");
```

This will fail in production or different development setups.

**Impact:** Broken functionality in non-dev environments.

**Fix:** Remove this method or make the path configurable.

---

### MEDIUM-06: `OutputService` history grows unbounded

**File:** `src/WinContainers.App/Services/OutputService.cs:19, 26`

**Problem:** The `_history` list grows without limit. Each `Write` call adds to it:

```csharp
private readonly List<(LogLevel Level, string Message)> _history = [];
```

Over a long session, this can consume significant memory.

**Impact:** Memory leak over long sessions.

**Fix:** Cap the history size (e.g., 1000 entries) with a ring buffer or trim when exceeding a threshold.

---

### MEDIUM-07: `QuickActionsViewModel.DebounceSearch` doesn't handle rapid cancellation properly

**File:** `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs:909-924`

**Problem:** The debounce uses `Task.Run` with a delay, but if the user types rapidly, multiple tasks are created. The `CancellationTokenSource` is replaced each time, but the old task's `OperationCanceledException` is caught silently. However, the `SearchDockerHubAsync` method dispatches to the UI thread via `_dispatcherQueue?.TryEnqueue`, and if the task is cancelled after the dispatch but before the UI update, the UI may show stale results.

**Impact:** Stale search results, race conditions.

**Fix:** Check the cancellation token before each UI update in `SearchDockerHubAsync`.

---

### MEDIUM-08: `ContainerDetailViewModel` file path handling has edge cases

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:610-612`

**Problem:** `OpenFileViewerAsync` constructs the file path by concatenation:

```csharp
var filePath = CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
```

If `CurrentFilePath` is null (which it shouldn't be but could be in edge cases), this throws. Also, `entry.Name` could contain path separators if the `ls` parsing is incorrect.

**Impact:** Potential crashes, path traversal.

**Fix:** Add null check for `CurrentFilePath` and validate `entry.Name` doesn't contain path separators.

---

### MEDIUM-09: `WslcDriver` uses `Process.Start()` without checking for failure

**File:** `src/WinContainers.Runtime/WslcDriver.cs:133`

**Problem:** `process.Start()` can return `false` if the process fails to start, but the return value is not checked:

```csharp
process.Start();
```

**Impact:** Silent failures when `wslc.exe` cannot be started.

**Fix:** Check the return value of `Start()` and throw an exception if it fails.

---

### MEDIUM-10: `TrayService` event handlers are not thread-safe

**File:** `src/WinContainers.App/Services/TrayService.cs:8-10`

**Problem:** The static events `ShowWindowRequested`, `ExitRequested`, and `ExitRequestedTrayThread` are invoked from the tray thread's message pump. If the UI thread subscribes/unsubscribes concurrently, this can cause a `NullReferenceException` or missed events.

**Impact:** Intermittent crashes on exit.

**Fix:** Use thread-safe event invocation pattern or `ConcurrentDictionary`-based event handlers.

---

### MEDIUM-11: `MainWindow.xaml.cs` `NavigateTo` catches and rethrows exceptions

**File:** `src/WinContainers.App/MainWindow.xaml.cs:128-138`

**Problem:** The method catches exceptions, writes them to a log file, and then rethrows:

```csharp
catch (Exception ex)
{
    System.IO.File.WriteAllText(...);
    throw;
}
```

This means the exception will crash the application. The logging is useful but the rethrow is problematic.

**Impact:** Application crash on navigation errors.

**Fix:** Consider showing an error dialog instead of crashing, or at least log and swallow non-critical navigation errors.

---

### MEDIUM-12: `ContainerDetailViewModel.ConvertPermissionsToNumeric` has a magic string for permission types

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:698-727`

**Problem:** The method checks for specific permission type characters:

```csharp
if (trimmed.StartsWith("d") || trimmed.StartsWith("-") || trimmed.StartsWith("l") || ...)
```

This is a maintenance burden and doesn't handle all possible file types (e.g., sockets `s`, doors `D` on Solaris).

**Impact:** Incorrect permission display for uncommon file types.

**Fix:** Use a more robust approach or document the limitation.

---

### MEDIUM-13: `QuickActionsViewModel.CreateAllFromComposeAsync` pulls images sequentially

**File:** `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs:676-712`

**Problem:** When creating multiple services from a compose file, images are pulled one at a time:

```csharp
foreach (var svc in services)
{
    _output.Write($"Pulling image '{svc.Image}'...");
    var pullOutput = await App.ServiceClient.PullImageAsync(svc.Image);
    ...
}
```

This is slow for multi-service stacks.

**Impact:** Poor UX for large compose stacks.

**Fix:** Pull images in parallel using `Task.WhenAll`, then create containers sequentially.

---

### MEDIUM-14: `WslcCommands.Run` ignores the `restart` parameter

**File:** `src/WinContainers.Core/WslcCommands.cs:61-80`

**Problem:** The method accepts a `restart` parameter but explicitly does not emit it (documented in a comment):

```csharp
// WSLC's run command does not support Docker's --restart option.
// Keep the parameter for the service contract, but do not emit an
// argument that causes every non-default run to fail.
```

This means restart policies are silently ignored. The `QuickActionsViewModel` still shows restart policy options to the user, creating a misleading UX.

**Impact:** User confusion, restart policies don't work.

**Fix:** Either implement restart policy support or remove the UI option and document the limitation.

---

### MEDIUM-15: `ContainerDetailPage.xaml.cs` `FileViewerDeleteButton_Click` calls `DeleteFileAsync` which is a no-op

**File:** `src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs:465-474` and `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:729-734`

**Problem:** The delete file button calls `DeleteFileAsync` which shows an error dialog saying "File delete not available via WSLC API":

```csharp
public async Task DeleteFileAsync(FileEntryData entry)
{
    var filePath = CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
    _output.Write($"Delete not available via WSLC API (path: {filePath})", ServiceLogLevel.Warning);
    await _dialog.ShowMessageAsync("Error", "File delete not available via WSLC API");
}
```

The delete button is still visible and clickable, which is misleading.

**Impact:** Poor UX, user confusion.

**Fix:** Hide the delete button or implement file deletion via `wslc exec rm`.

---

### MEDIUM-16: `QuickActionsViewModel` uses `NetworkInformationException` catch but doesn't handle other exceptions

**File:** `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs:1229-1242`

**Problem:** `GetActiveTcpPorts` only catches `NetworkInformationException`:

```csharp
catch (NetworkInformationException)
{
    return [];
}
```

Other exceptions (e.g., `SecurityException` on restricted environments) are not caught.

**Impact:** Unhandled exceptions in conflict detection.

**Fix:** Catch `Exception` and log the error.

---

### MEDIUM-17: `ImagesViewModel.DeleteImageAsync` rethrows exceptions after logging

**File:** `src/WinContainers.App/ViewModels/ImagesViewModel.cs:104-120`

**Problem:** The method catches exceptions, logs them, and then rethrows:

```csharp
catch (Exception ex)
{
    _output.Write($"Remove image failed: {ex.Message}", Services.LogLevel.Error);
    throw;
}
```

The caller (`ImagesPage.xaml.cs`) doesn't handle this exception, which could crash the UI.

**Impact:** Potential UI crash on image deletion failure.

**Fix:** Handle the exception in the caller or return a result instead of throwing.

---

### MEDIUM-18: `ContainerDetailViewModel.IsErrorOutput` uses string matching for error detection

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:441-451`

**Problem:** Error detection is based on string matching:

```csharp
if (trimmed.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)) return true;
if (trimmed.Contains("cannot start", StringComparison.OrdinalIgnoreCase)) return true;
```

This is fragile and will miss many error cases or produce false positives.

**Impact:** Errors not detected, poor error reporting.

**Fix:** Use structured error responses from the API instead of string matching.

---

## 5. Low Issues

### LOW-01: Inconsistent null-forgiving operator usage

**Files:** Multiple

**Problem:** The codebase uses nullable reference types (`<Nullable>enable</Nullable>` in `Directory.Build.props`), but there are inconsistent uses of the null-forgiving operator (`!`). Some places use it correctly, others don't. For example, `ContainerDetailPage.xaml.cs:114` uses `button.Tag is not string id` but then uses `id` without null check in some paths.

**Fix:** Audit all null-forgiving operator usage and ensure consistency.

---

### LOW-02: `GlobalUsings.cs` is empty or minimal

**File:** `src/WinContainers.App/GlobalUsings.cs`

**Problem:** The file exists but doesn't contain global usings, leading to redundant using statements across files.

**Fix:** Add global usings for commonly used namespaces.

---

### LOW-03: `ContainerCardData` mixes `ObservableObject` with record-like semantics

**File:** `src/WinContainers.Runtime/Models/ContainerCardData.cs:12`

**Problem:** `ContainerCardData` is a `partial class` that extends `ObservableObject` but is used like a data model. The `ContainerGroup` class below it implements `INotifyPropertyChanged` manually instead of using `ObservableObject`.

**Fix:** Make `ContainerGroup` extend `ObservableObject` for consistency.

---

### LOW-04: `WslcContainerParser.GetField` iterates all properties for case-insensitive matching

**File:** `src/WinContainers.Runtime/WslcContainerParser.cs:291-313`

**Problem:** The method first tries `TryGetProperty` (case-sensitive) and then falls back to iterating all properties for case-insensitive matching. This is inefficient for large JSON objects.

**Fix:** Use `JsonSerializer` with `PropertyNameCaseInsensitive` or cache the case-insensitive lookup.

---

### LOW-05: `ServiceHost` uses `int.Parse` for port without error handling

**File:** `src/WinContainers.Service/Host/ServiceHost.cs:16`

**Problem:** `int.Parse(ServiceEndpointResolver.ResolveServicePort())` will throw if the environment variable contains a non-numeric value.

**Fix:** Use `int.TryParse` with a fallback.

---

### LOW-06: `MainWindow.xaml.cs` uses magic numbers for window sizing

**File:** `src/WinContainers.App/MainWindow.xaml.cs:42`

**Problem:** Window size is hardcoded: `(int)(1320 * scale)` and `(int)(920 * scale)`.

**Fix:** Make window size configurable or use a settings file.

---

### LOW-07: `QuickActionsViewModel` has duplicate `LoadTemplatesAsync` and `RefreshCatalogAsync` logic

**File:** `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs:126-181`

**Problem:** The two methods share nearly identical logic for loading templates and updating categories.

**Fix:** Extract common logic into a private helper method.

---

### LOW-08: `ContainerDetailViewModel` `ShellOptions` array is not configurable

**File:** `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs:494`

**Problem:** Shell options are hardcoded: `["/bin/bash", "/bin/sh", "pwsh", "cmd.exe"]`.

**Fix:** Make configurable or detect available shells dynamically.

---

### LOW-09: `OnboardingViewModel` has duplicate `RunPowerShellCommandAsync` and `RunCommandAsync` methods

**File:** `src/WinContainers.App/ViewModels/OnboardingViewModel.cs:316-422`

**Problem:** Both methods are nearly identical, differing only in the executable (`powershell.exe` vs `cmd.exe`).

**Fix:** Extract common logic into a shared helper.

---

### LOW-10: `MainWindow.xaml.cs` `OutputTextBlock` text concatenation is inefficient

**File:** `src/WinContainers.App/MainWindow.xaml.cs:89-93`

**Problem:** Output is accumulated via string concatenation:

```csharp
OutputTextBlock.Text += $"{prefix}{e.Message}";
```

For large outputs, this creates many string allocations.

**Fix:** Use a `StringBuilder` or `TextBox.Document` with `TextPointer`.

---

### LOW-11: `ContainersControl.xaml.cs` `GroupButtonGuard` doesn't use the `group` parameter

**File:** `src/WinContainers.App/Pages/ContainersControl.xaml.cs:241-257`

**Problem:** The `GroupButtonGuard` constructor accepts a `ContainerGroup` parameter but doesn't use it:

```csharp
public GroupButtonGuard(Button btn, ContainerGroup group)
{
    _btn = btn;
    _origContent = btn.Content;
    btn.Content = "◎";
}
```

**Fix:** Remove the unused parameter or use it for additional functionality.

---

### LOW-12: `WslcDriver` timeout constants are not configurable

**File:** `src/WinContainers.Runtime/WslcDriver.cs:10-12`

**Problem:** Timeout values are hardcoded constants. Different environments may need different timeouts.

**Fix:** Make timeouts configurable via environment variables or settings.

---

## 6. Testing Issues

### TEST-01: Unit test coverage is minimal

**File:** `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Problem:** The unit tests focus on parser and command generation but don't test:
- ViewModels (no mocking of `App.ServiceClient`)
- `WslcDriver` (requires `wslc.exe` to be installed)
- `ServiceHost` middleware (auth, logging)
- `QuickActionsViewModel` (compose parsing, conflict detection)
- `ContainerDetailViewModel` (file operations, shell execution)

The tests that do exist are mostly contract tests (verifying method names exist, JSON parsing works).

**Impact:** Low confidence in code changes, high risk of regressions.

**Fix:** Add unit tests for ViewModels using mocked `WslcServiceClient`. Add integration tests for `ServiceHost` middleware. Add tests for compose parsing and conflict detection logic.

---

### TEST-02: Integration tests don't test actual container operations

**File:** `tests/WinContainers.Tests.Integration/UnitTest1.cs`

**Problem:** The integration tests only test:
1. `/api/info` endpoint with auth
2. `/api/info` endpoint without auth (expecting 401)
3. WSLC runtime reachability (skipped in CI)

They don't test container lifecycle operations (start, stop, remove), image operations, volume/network operations, or the exec endpoint.

**Impact:** No integration coverage for the core functionality.

**Fix:** Add integration tests for all API endpoints using a test container or mock WSLC.

---

### TEST-03: Playwright and UI tests are empty stubs

**Files:** `tests/WinContainers.Tests.Playwright/UnitTest1.cs`, `tests/WinContainers.Tests.Ui/AppLaunchTests.cs`

**Problem:** Both test projects contain only empty stub test classes with no actual tests.

**Impact:** No UI-level test coverage.

**Fix:** Implement Playwright tests for the web-based components and WinAppDriver tests for the WinUI UI.

---

### TEST-04: `RuntimeContractTests` modifies global environment variables

**File:** `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs:29-51`

**Problem:** Multiple tests set and restore environment variables (`WINCONTAINERS_SERVICE_PORT`, `WINCONTAINERS_SERVICE_TOKEN`, etc.). If tests run in parallel, this causes race conditions.

**Impact:** Flaky tests, test interference.

**Fix:** Use `[Collection]` to serialize tests that modify environment variables, or use `Environment.SetEnvironmentVariable` with proper isolation.

---

## 7. Architecture Recommendations

### ARCH-01: Consider extracting `WslcServiceClient` to a separate package

The `WslcServiceClient` is a pure HTTP client that could be reused by other clients. Extracting it to a separate NuGet package would enable external tooling.

### ARCH-02: Consider using `IHttpClientFactory` for HTTP clients

Both `WslcServiceClient` and `WslcUpdateService` create `HttpClient` instances directly. Using `IHttpClientFactory` would provide proper lifetime management, logging, and resilience (retry policies, circuit breakers).

### ARCH-03: Consider using `Microsoft.Extensions.Hosting` for the service host

The `ServiceHost.Build` method manually configures Kestrel and middleware. Using `IHost` with `ConfigureWebHostDefaults` would provide better integration with logging, configuration, and lifecycle management.

### ARCH-04: Consider using `CommunityToolkit.Mvvm` source generators

The codebase manually implements `INotifyPropertyChanged` in many places. The `CommunityToolkit.Mvvm` package is already included and provides `[ObservableProperty]` source generators that would eliminate boilerplate.

---

## 8. Priority Fix List

| Priority | Issue | Effort | Impact |
|----------|-------|--------|--------|
| P0 | CRITICAL-01: Command injection in shell exec | Medium | Security |
| P0 | CRITICAL-02: WebView2 XSS via JSON injection | Small | Security |
| P0 | CRITICAL-03: Hardcoded WSL download URL/hash | Medium | Reliability |
| P0 | CRITICAL-04: No cancellation in background tasks | Small | Stability |
| P0 | CRITICAL-05: HttpClient timeout not configured | Small | Reliability |
| P1 | HIGH-01: ViewModelBase null dispatcher risk | Small | Stability |
| P1 | HIGH-02: Race condition in RebuildGroupedList | Medium | Stability |
| P1 | HIGH-03: PropertyChanged subscription leak | Small | Memory |
| P1 | HIGH-05: QuickActionsViewModel too large | Large | Maintainability |
| P1 | HIGH-07: Route parameter validation | Medium | Security |
| P2 | MEDIUM-01: Direct App.ServiceClient usage | Medium | Testability |
| P2 | MEDIUM-02: Empty DashboardViewModel | Small | Cleanup |
| P2 | MEDIUM-03: Async method without await | Small | Quality |
| P2 | MEDIUM-06: Unbounded output history | Small | Performance |
| P2 | TEST-01: Minimal unit test coverage | Large | Quality |
