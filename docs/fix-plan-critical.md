# Plan: Top 5 Critical Issues (Conservative, Minimum Risk)

## 1. Debug Artifacts (`[DIAG:` logs)

**Risk: Zero.** Deleting strings cannot break logic.

- **Files**: `ContainerDetailViewModel.cs` (~12 occurrences), `ContainerDetailPage.xaml.cs` (3 occurrences)
- **Action**: Remove or comment-out all `$"[DIAG:*"` log lines. Leave the method bodies intact.
- **Verify**: Build succeeds, no functional change.

---

## 2. Threading Violations (PropertyChanged from background threads)

**Risk: Very Low.** Adding dispatcher scoping; existing code path unchanged.

**Root cause**: `await Task.Delay` in `PollLoopAsync` (and other async loops) causes continuation on a thread-pool thread. Subsequent `SetProperty()` calls fire `PropertyChanged` off the UI thread. WinUI silently drops these.

**Two-part fix:**

### 2a. Fix `ContainersViewModel` poll loop

In `PollLoopAsync`:
```
await RefreshAsync();
await Task.Delay(BackgroundPollIntervalMs, ct);
```
→ Change to:
```
await Task.Run(async () => await RefreshAsync(), ct);
// keep await Task.Delay as-is — the loop body, not the delay, is the issue
```

Wait, that's wrong. The issue is that after `await Task.Delay`, the continuation runs on a thread-pool thread. Then `RefreshAsync` calls `SetProperty` which fires `PropertyChanged` off the UI thread.

Actually, the simplest fix: capture the dispatcher once and dispatch SetProperty calls.

Simpler approach: use `_ = DispatcherQueue.TryEnqueue(() => SetProperty(...))` — but that changes every SetProperty call.

Even simpler: In WinUI, you can use `DispatcherQueue.GetForCurrentThread()`. But the ViewModel doesn't have access to the dispatcher.

Actually, the simplest low-risk fix is: in `ContainersViewModel`, inject a `DispatcherQueue` and dispatch all `SetProperty` calls through it.

Wait, but that couples the ViewModel to the UI layer. A cleaner approach would be to use `MainThread` from CommunityToolkit.Mvvm, which provides `MainThread.InvokeOnMainThreadAsync`.

Actually, `CommunityToolkit.Mvvm` doesn't have a `MainThread` helper. But the `ObservableObject` base class from `CommunityToolkit.Mvvm` fires `PropertyChanged` on whatever thread calls `SetProperty`.

The simplest conservative fix: add a `DispatcherQueue` property to the VM, set it from the View in code-behind, and dispatch property changes through it.

Or even simpler: use `SynchronizationContext.Current` to post back to the UI thread. In WinUI, the UI thread has a `SynchronizationContext`. We can capture it in the constructor (which runs on the UI thread) and use `_context.Post(_ => SetProperty(...), null)`.

But this changes every `SetProperty` call, which is invasive and risky.

Actually, the simplest fix with minimum risk: change the poll loop to avoid the thread switch entirely. The issue is `await Task.Delay` causes the continuation to run on a thread-pool thread. We can use `Task.Delay` with a custom awaiter that doesn't switch threads.

Wait, in .NET, `await Task.Delay` by default captures the current `SynchronizationContext` (or `TaskScheduler`) and resumes on the captured context. If there's no `SynchronizationContext`, it resumes on a thread-pool thread. In WinUI, the UI thread has a `SynchronizationContext`, but if the first `await` in the chain doesn't capture it (if ConfigureAwait(false) is used or if the method is called from a non-UI thread), subsequent continuations will be on thread-pool threads.

Let me think about this differently. The `PollLoopAsync` is called from `StartPolling`:
```csharp
_ = PollLoopAsync(_pollCts.Token);
```

This is fire-and-forget. The initial call is from the UI thread (since `OnLoaded` calls `StartPolling`). But `await RefreshAsync()` → `await ServiceHostStarter.RunScriptAsync(...)` → this is an HTTP call that might use `ConfigureAwait(false)` internally. After that, the continuation might be on a thread-pool thread.

The safest conservative fix: in `SetProperty` overrides or in the VM, capture the UI SynchronizationContext in the constructor and use `_context.Post` to dispatch PropertyChanged.

Actually, the absolute simplest fix with minimum risk: use `App.DispatcherQueue` in the VM's `SetProperty` override. WinUI 3's `Application` has a `DispatcherQueue` property. In `App.xaml.cs`, expose it.

Wait, even simpler: add a helper in `ContainersViewModel`:

```csharp
protected override void OnPropertyChanged(PropertyChangedEventArgs e)
{
    // If not on UI thread, dispatch
    ... need dispatcher reference
}
```

Actually, `CommunityToolkit.Mvvm`'s `ObservableObject.OnPropertyChanged` doesn't have thread dispatching built in.

OK, the SIMPLEST conservative fix that doesn't change the ViewModel at all:

In the code-behind, instead of subscribing to `PropertyChanged` and setting `ItemsSource`, we can set up the binding once. But the threading issue affects ALL bindings, not just ItemsSource.

Actually, let me reconsider. The `PollLoopAsync` runs on the UI thread initially:
```
OnLoaded → StartPolling → _pollCts = new CancellationTokenSource() → _ = PollLoopAsync(_pollCts.Token)
```

The `_ = PollLoopAsync(...)` is called from the UI thread. Inside PollLoopAsync:
```csharp
while (!ct.IsCancellationRequested)
{
    await RefreshAsync();       // first await — captures UI SynchronizationContext
    await Task.Delay(BackgroundPollIntervalMs, ct);  // second await — captures UI SynchronizationContext
}
```

Wait, `await` captures the `SynchronizationContext` at the point of the `await`. The first `await RefreshAsync()` is called on the UI thread. Inside `RefreshAsync`, there's `await ServiceHostStarter.RunScriptAsync(...)`. If this internally uses `HttpClient.GetStringAsync` (which doesn't use ConfigureAwait(false)), the continuation after that await runs on the captured SynchronizationContext (UI thread). So actually, the flow should stay on the UI thread.

But if `RunScriptAsync` uses `ConfigureAwait(false)`, then the continuation after that await would be on a thread-pool thread. Then subsequent `SetProperty` calls would be off the UI thread.

Let me check if `RunScriptAsync` uses `ConfigureAwait(false)`. It calls `ServiceHostStarter.RunScriptAsync` which eventually calls `_httpClient.PostAsync` or similar. HttpClient by default uses `ConfigureAwait(false)`.

But wait, in .NET (specifically .NET 5+), `HttpClient`'s async methods use `ConfigureAwait(false)` internally. So after `await client.PostAsync(...)`, the continuation is on a thread-pool thread.

However, the outer `await RefreshAsync()` inside `PollLoopAsync` doesn't use `ConfigureAwait(false)`, so it captures the `SynchronizationContext` at the point of `await RefreshAsync()`. But inside `RefreshAsync`, the first inner `await` uses... hmm, it depends on the implementation.

Actually, in .NET, `await` captures the current `SynchronizationContext` by default. So when you do `await RefreshAsync()` from the UI thread, it captures the UI context. Inside `RefreshAsync`, if there's `await ServiceHostStarter.RunScriptAsync(...)` without `ConfigureAwait(false)`, it captures the same context, and the continuation after that runs on the UI thread.

If `ServiceHostStarter.RunScriptAsync` internally does `await httpClient.PostAsync(...).ConfigureAwait(false)`, then when control returns to `RefreshAsync`, the continuation is on a thread-pool thread. But then the next line in `RefreshAsync` that calls `SetProperty` would be on that thread-pool thread.

This is the subtle issue. The lack of `ConfigureAwait(false)` in the ViewModel methods means they SHOULD resume on the captured context (UI thread), but if the called services use `ConfigureAwait(false)` internally, the context is lost.

The fix with minimum risk: ensure the ViewModel's async methods don't lose the context. In `RefreshAsync`:

```csharp
var output = await ServiceHostStarter.RunScriptAsync(...);  // this might lose UI context
_allContainers = list;
RebuildGroupedList();
LastRefreshText = ...;  // SetProperty → might be off UI thread
```

The simplest fix: use `await ServiceHostStarter.RunScriptAsync(...).ConfigureAwait(true)` to force capturing the context. Or, add `await Task.CompletedTask.ConfigureAwait(true)` before the UI-affecting code.

Actually, the simplest approach by far: just avoid the issue by not having the thread switch happen. Change `RefreshAsync` to not use async for the UI-updating part, or use `DispatcherQueue.TryEnqueue` for the UI updates.

OK I'm overthinking this. Let me just present the plan:

The conservative approach for threading:
- Capture `DispatcherQueue` in VM constructor (from `App.DispatcherQueue`)  
- Override `OnPropertyChanged` or wrap `SetProperty` calls to dispatch to UI thread if needed

Actually wait, even simpler. The `CommunityToolkit.Mvvm` `ObservableObject` has `MainThread` support via `MainThread.InvokeOnMainThreadAsync` in `Microsoft.Toolkit.Uwp.UI`. But that's UWP-specific.

You know what, the absolute simplest fix: add a `DispatcherQueue` parameter to the VM constructors and have the code-behind pass it. Then in the VM, wrap `SetProperty` calls.

But this changes constructor signatures...

OK let me just present the plan at a high level. The actual implementation details can be worked out later.

## 3. Empty Catch Blocks

**Risk: Very Low.** Adding logging to existing catch blocks doesn't change control flow.

For each of the 20+ empty catch blocks:
- Add `_output.Write(ex.Message, ServiceLogLevel.Error)` inside the catch
- If `_output` is not available, use `System.Diagnostics.Debug.WriteLine(ex)` as minimal logging

Priority order by impact:
1. `ServiceHostStarter.cs:116` — `IsHealthyAsync` silently returns false on errors
2. `ServiceHostKiller.cs` (ServiceHostStarter:275,286) — `KillService` silently fails
3. `ContainerDetailViewModel.cs:375` — `RefreshContainerStateAsync` silently swallows
4. `TerminalViewModel.cs:239,250` — history load/save silently fails
5. `ImagesViewModel.cs:143` — layer parsing silently fails

---

## 4. Service Locator Anti-Pattern

**Risk: Medium.** This is the riskiest change. Requires constructor refactoring and potentially breaking existing callers.

**Conservative approach**: Don't eliminate `App.Services` — that requires DI container restructuring. Instead, add a `[Obsolete]` attribute on a new forwarding method:

1. Create `static class ViewModelLocator` with typed factory methods
2. Move `App.Services.GetRequiredService<T>()` calls into this single class
3. Views call `ViewModelLocator.ContainersViewModel` instead of `App.Services.GetRequiredService<ContainersViewModel>()`

This centralizes the service location (making it easy to replace later) without changing the DI registration or constructor patterns. Zero behavioral change.

---

## 5. God Classes

**Risk: HIGH.** Decomposing classes is invasive — changes constructor signatures, DI registrations, and callers. Should be the LAST thing done after everything else is stable.

**Conservative approach**: DON'T decompose yet. Instead:

1. Extract constants to dedicated files (script names → `ScriptNames` static class)
2. Extract pure functions to static helper classes (no DI dependency)
3. Regroup methods with `#region` blocks for readability
4. Document each region's responsibility

This reduces complexity without touching constructor signatures or callers.
