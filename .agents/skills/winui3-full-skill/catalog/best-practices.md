# WinUI 3 Best Practices

Authoritative rules for every WinUI 3 / Windows App SDK project generated or maintained
using this skill.

---

## 1. Data Binding

- **Always use `x:Bind`** (compiled binding) instead of `{Binding}`.
  `{Binding}` uses runtime reflection, is slower, and produces silent runtime failures for
  typos. Use `{Binding}` only for `ElementName` references and truly dynamic scenarios.
- **Always set `x:DataType`** on every `DataTemplate` to enable compiled bindings and
  IntelliSense inside the template.
- Default mode for `x:Bind` is `OneTime`; explicitly add `Mode=OneWay` or `Mode=TwoWay`
  for live or two-way bindings.
- Use `ObservableCollection<T>` (not `List<T>`) for collection properties that change at
  runtime so the UI updates automatically.

---

## 2. MVVM Pattern

- ViewModels **must not** reference any WinUI / Windows UI types (`Microsoft.UI.Xaml.*`).
  This keeps them unit-testable and independent of the UI framework.
- Implement `INotifyPropertyChanged` via `CommunityToolkit.Mvvm`'s `ObservableObject` and
  the `[ObservableProperty]` source generator — never write boilerplate `SetProperty` calls
  manually.
- Use `RelayCommand` / `AsyncRelayCommand` (CommunityToolkit.Mvvm) for all commands.
- Keep code-behind (`.xaml.cs`) minimal — only UI event wiring (`x:Bind` event handlers)
  and navigation calls. All logic belongs in the ViewModel.
- ViewModels are resolved via Dependency Injection; never instantiate them with `new` in
  the View.

---

## 3. Threading & Async

- Use `async`/`await` for **all** I/O-bound and long-running operations.
- **Never block the UI thread**: `Thread.Sleep`, `.Result`, and `.Wait()` are forbidden on
  the UI thread.
- Offload CPU-bound work with `Task.Run(...)`.
- Use `CancellationToken` for cancellable operations; propagate it through the full call
  chain.
- Use `DispatcherQueue.TryEnqueue` (not the deprecated `Dispatcher.Invoke`) when marshalling
  work back to the UI thread from a background thread.

---

## 4. Performance

- **Enable UI virtualisation** for lists: always use `ListView`, `GridView`, `ItemsView`,
  or `ItemsRepeater` with a `VirtualizingLayout`. Never place collection controls inside an
  unbounded `ScrollViewer` with `VerticalScrollMode="Disabled"`.
- **Keep `DataTemplate` XAML shallow**: deeply nested panels in item templates slow
  realisation. Move complex content to a dedicated `UserControl`.
- Avoid `x:Bind` with expensive inline function calls in tight loops — cache the value in
  the ViewModel instead.
- Use `x:Load` (deferred loading) for elements that may never be visible (e.g., error
  panels, empty states) to avoid constructing their visual trees at startup.
- Use `x:Phase` on `DataTemplate` bindings to stagger non-critical property updates during
  fast scrolling.
- Prefer `SolidColorBrush` resources over inline `Color` values; brushes are shared and
  not re-allocated per element.

---

## 5. Memory Management

- Unsubscribe from events in `Unloaded` (or implement `IDisposable`) to avoid memory leaks,
  especially for long-lived objects (singletons, static event buses).
- Use `WeakReference<T>` or `WeakEventManager` for ViewModel → View callbacks when the View
  lifetime is shorter than the ViewModel.
- Dispose `WebView2`, `MediaPlayer`, and `IRandomAccessStream` instances explicitly.

---

## 6. Accessibility

- Provide `AutomationProperties.Name` on every interactive control that has no visible
  text label (icon buttons, image buttons).
- Provide `AutomationProperties.LabeledBy` or `AutomationProperties.FullDescription` for
  inputs that have a separate `TextBlock` label.
- Test keyboard navigation (Tab order, arrow keys in lists, Enter/Space to activate).
- Test with Narrator (built-in Windows screen reader).
- Never rely on colour alone to convey meaning — pair colour with text, icon, or pattern.

---

## 7. Error Handling

- Catch specific exception types; avoid bare `catch (Exception)` unless logging and
  rethrowing.
- Surface errors to the UI via ViewModel properties (e.g., `ErrorMessage`) bound to an
  `InfoBar` or `ContentDialog` — not via unhandled exceptions.
- Log errors with `ILogger<T>` (Microsoft.Extensions.Logging); inject it via DI.
- Use `try/finally` (or `using`) to ensure resource cleanup regardless of exceptions.
- For `async void` event handlers (only acceptable in code-behind), always wrap the body
  in `try/catch` because exceptions on `async void` methods cannot be observed by callers.

---

## 8. Navigation

- Use `NavigationView` for all primary (top-level) app navigation. Do not use `Pivot` or
  `TabView` as a primary navigation pattern.
- Navigate using `Frame.Navigate(typeof(TargetPage), parameter)`.
- Register pages and ViewModels in DI; resolve them in a navigation service rather than
  directly in `MainWindow`.
- Handle `Frame.BackStack` and the hardware/software back button via `SystemNavigationManager`
  or the `TitleBar.BackRequested` event.

---

## 9. Resources & Theming

- Always merge `XamlControlsResources` **first** in `MergedDictionaries`; app overrides
  must appear after it.
- Use `ThemeResource` (not `StaticResource`) for all colour and brush values so they
  automatically update when the user switches between light, dark, and high-contrast modes.
- Define app colours in `ThemeDictionaries` (`Default`/`Light`/`HighContrast`) rather than
  hard-coding hex values inline.
- Prefer `ResourceDictionary` + `StaticResource`/`ThemeResource` over inline styles.

---

## 10. Windowing

- Set `AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode` so the system
  title bar stays in sync with the app theme.
- When using a custom `TitleBar` control, always call both `ExtendsContentIntoTitleBar = true`
  **and** `SetTitleBar(element)`.
- With `OverlappedPresenter`, `HasBorder` must be `true` when `HasTitleBar` is `true` —
  the opposite combination causes a fatal runtime error.
- Store references to secondary windows to prevent garbage collection while they are open.
- Use `DispatcherQueue.TryEnqueue` to communicate between windows on different UI threads.

---

## 11. Dependency Injection

- Register services and ViewModels in `App.xaml.cs` using
  `Microsoft.Extensions.DependencyInjection`.
- Prefer constructor injection; avoid service locator / static `App.Services` accessors
  except at composition root boundaries.
- Use scoped or transient lifetimes for ViewModels (not singleton) unless the ViewModel
  represents shared global state.

---

## 12. Code Style

- Language version: C# 10+ (file-scoped namespaces, `record`, `global using`).
- Async methods suffix: `Async` (e.g., `LoadDataAsync`).
- Private fields: `_camelCase`. Classes / methods / properties: `PascalCase`.
- `var` when the type is obvious from the right-hand side; avoid for primitive types.
- Enable `<Nullable>enable</Nullable>` and annotate all reference parameters/returns.
- Remove unused `using` directives; group: System → third-party → project, with blank line
  between groups.
