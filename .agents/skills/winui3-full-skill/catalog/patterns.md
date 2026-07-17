# WinUI 3 Patterns

Architectural and structural patterns for WinUI 3 applications. Each pattern entry
references the concrete snippet file that provides working XAML + C# examples.

---

## MVVM (Model-View-ViewModel)

The recommended architecture for WinUI 3 apps. Separates UI (View / XAML) from logic
(ViewModel) and data (Model), enabling unit testing and maintainability.

**Rules:**
- ViewModels must **not** reference `Microsoft.UI.Xaml.*` types.
- Use `CommunityToolkit.Mvvm` — `ObservableObject`, `[ObservableProperty]`,
  `RelayCommand`, `AsyncRelayCommand`.
- Bind `x:DataType` on every page and `DataTemplate`.
- Resolve ViewModels via Dependency Injection; never `new ViewModel()` in a View.

**Snippet:** [snippets/patterns/mvvm-di-setup.md](../snippets/patterns/mvvm-di-setup.md)

---

## Dependency Injection

WinUI 3 apps use `Microsoft.Extensions.DependencyInjection` for service registration and
resolution. The composition root is `App.xaml.cs`.

**Rules:**
- Register all services and ViewModels in `App.xaml.cs → ConfigureServices`.
- Prefer constructor injection in both ViewModels and services.
- Use `IServiceProvider` only at composition-root boundaries (App, navigation service).
- Prefer transient or scoped ViewModels over singleton unless representing global state.

**Snippet:** [snippets/patterns/mvvm-di-setup.md](../snippets/patterns/mvvm-di-setup.md)

---

## App Shell (NavigationView + Frame)

The standard top-level app structure: a `NavigationView` hosts a `Frame` that navigates
between pages. The shell lives in `MainWindow.xaml`.

**Rules:**
- Use `NavigationView` for all primary navigation.
- Navigate with `Frame.Navigate(typeof(TargetPage), parameter)`.
- Handle back navigation via `TitleBar.BackRequested` or `SystemNavigationManager`.
- Wire `NavigationView.SelectionChanged` → `Frame.Navigate`.

**Snippet:** [snippets/patterns/app-shell.md](../snippets/patterns/app-shell.md)

---

## Theming (Light / Dark / Custom)

WinUI 3 supports per-app and per-element theme switching via `RequestedTheme`.

**Rules:**
- Use `ThemeResource` for all colour/brush values so they adapt automatically.
- Define custom colours in `ResourceDictionary.ThemeDictionaries` (`Default`, `Light`,
  `HighContrast`).
- Never hard-code hex colour values in XAML or C#.
- Persist user theme preference to `ApplicationData.Current.LocalSettings`.

**Snippet:** [snippets/patterns/theming.md](../snippets/patterns/theming.md)

---

## Data Binding

See [catalog/best-practices.md](best-practices.md) §1 and the dedicated snippet for
comprehensive examples of `x:Bind`, `{Binding}`, converters, and `ObservableCollection`.

**Snippet:** [snippets/fundamentals/binding.md](../snippets/fundamentals/binding.md)

---

## ResourceDictionary & Styles

Centralise brushes, sizes, and styles in `ResourceDictionary` files merged in `App.xaml`.
Use `BasedOn` to extend platform default styles rather than copying them.

**Snippets:**
- [snippets/fundamentals/resources.md](../snippets/fundamentals/resources.md)
- [snippets/fundamentals/styles.md](../snippets/fundamentals/styles.md)

---

## Custom UserControls

Encapsulate repeated UI patterns in `UserControl` + `DependencyProperty` to enable data
binding, style setters, and reuse across pages.

**Snippet:** [snippets/fundamentals/custom-user-controls.md](../snippets/fundamentals/custom-user-controls.md)

---

## Data Templates & Template Selection

Use `DataTemplate` + `x:DataType` for typed compiled bindings in all collection and
content controls. Use `DataTemplateSelector` for heterogeneous item types.

**Snippet:** [snippets/fundamentals/templates.md](../snippets/fundamentals/templates.md)

---

## Async Loading Pattern

Standard pattern for loading data asynchronously on a page without blocking the UI thread.

```csharp
// ViewModel
[ObservableProperty] private bool _isLoading;
[ObservableProperty] private string? _errorMessage;

public ObservableCollection<Item> Items { get; } = new();

[RelayCommand]
private async Task LoadAsync(CancellationToken ct)
{
    IsLoading = true;
    ErrorMessage = null;
    try
    {
        var data = await _dataService.GetItemsAsync(ct);
        Items.Clear();
        foreach (var item in data)
            Items.Add(item);
    }
    catch (OperationCanceledException) { /* navigation cancelled */ }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load items");
        ErrorMessage = "Could not load items. Please try again.";
    }
    finally
    {
        IsLoading = false;
    }
}
```

```xaml
<!-- Page -->
<Grid>
    <ListView
        ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
        Visibility="{x:Bind GetListVisibility(ViewModel.IsLoading, ViewModel.ErrorMessage), Mode=OneWay}" />

    <ProgressRing
        IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
        HorizontalAlignment="Center"
        VerticalAlignment="Center" />

    <InfoBar
        IsOpen="{x:Bind ViewModel.HasError, Mode=OneWay}"
        Severity="Error"
        Title="Error"
        Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}"
        IsClosable="True" />
</Grid>
```

---

## Navigation Service

Abstract `Frame.Navigate` behind an `INavigationService` interface so ViewModels can
trigger navigation without referencing UI types.

```csharp
// Services/INavigationService.cs
namespace MyApp.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void Navigate<TPage>(object? parameter = null);
    void GoBack();
}
```

```csharp
// Services/NavigationService.cs
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public void SetFrame(Frame frame) => _frame = frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void Navigate<TPage>(object? parameter = null)
        => _frame?.Navigate(typeof(TPage), parameter);

    public void GoBack()
    {
        if (_frame?.CanGoBack is true)
            _frame.GoBack();
    }
}
```

Register in `App.xaml.cs`:

```csharp
services.AddSingleton<INavigationService, NavigationService>();
```

---

## Error Presentation via InfoBar

Expose errors from ViewModels as observable properties and display them with `InfoBar`.
Never show unhandled exception dialogs.

```csharp
// ViewModel
[ObservableProperty] private string? _errorMessage;
public bool HasError => ErrorMessage is not null;

partial void OnErrorMessageChanged(string? value)
    => OnPropertyChanged(nameof(HasError));
```

```xaml
<InfoBar
    IsOpen="{x:Bind ViewModel.HasError, Mode=OneWay}"
    Severity="Error"
    Title="Something went wrong"
    Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}"
    IsClosable="True"
    Closed="ErrorBar_Closed" />
```

```csharp
private void ErrorBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    => ViewModel.ErrorMessage = null;
```
