# WinUI 3 Full Skill

> An AI agent skill that gives any coding assistant **complete WinUI 3 knowledge**: ~70 controls, MVVM/DI patterns, Fluent Design, accessibility, theming, and windowing — all backed by real code from the WinUI Gallery.

---

## Install

```bash
npx skills add SudoCode76/winui3-skills
```

Works with **OpenCode**, **Claude Code**, **GitHub Copilot**, **Cursor**, **Windsurf**, **Cline**, **Codex**, and [40+ other agents](https://skills.sh).

---

## What this skill does

Once installed, your AI agent can generate production-quality **WinUI 3 / Windows App SDK 1.8+** code for any scenario — from a single control to a full app shell — following Microsoft's Fluent Design guidelines and industry best practices.

The skill covers:

- **~70 controls** with working XAML + C# examples drawn from the official [WinUI Gallery](https://github.com/microsoft/WinUI-Gallery)
- **MVVM pattern** with `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `RelayCommand`)
- **Dependency Injection** wiring via `Microsoft.Extensions.DependencyInjection`
- **Compiled data binding** with `x:Bind` and `x:DataType` on every `DataTemplate`
- **Async/threading rules** — no blocking the UI thread, `DispatcherQueue.TryEnqueue`, `CancellationToken`
- **Accessibility** — `AutomationProperties.Name`, keyboard navigation, Narrator compatibility
- **Theming** — light/dark/high-contrast with `ThemeResource`, Mica, Acrylic
- **Windowing** — `AppWindow`, custom `TitleBar`, multiple windows, presenters
- **Fundamentals** — `ResourceDictionary`, `Style`, `DataTemplate`, `UserControl`, `DependencyProperty`

---

## Coverage

### Basic Input (14 controls)
`Button` · `RepeatButton` · `ToggleButton` · `HyperlinkButton` · `DropDownButton` · `SplitButton` · `ToggleSplitButton` · `CheckBox` · `RadioButton` · `ComboBox` · `Slider` · `ToggleSwitch` · `ColorPicker` · `RatingControl`

### Text (7 controls)
`TextBlock` · `TextBox` · `AutoSuggestBox` · `NumberBox` · `PasswordBox` · `RichEditBox` · `RichTextBlock`

### Collections (7 controls)
`ListView` · `GridView` · `ListBox` · `TreeView` · `ItemsRepeater` · `ItemsView` · `FlipView`

### Navigation (5 controls)
`NavigationView` · `TabView` · `BreadcrumbBar` · `Pivot` · `SelectorBar`

### Dialogs & Flyouts (4 controls)
`ContentDialog` · `Flyout` · `TeachingTip` · `Popup`

### Layout (9 panels)
`Grid` · `StackPanel` · `Border` · `Canvas` · `RelativePanel` · `Expander` · `SplitView` · `VariableSizedWrapGrid` · `Viewbox`

### Status & Info (5 controls)
`InfoBar` · `InfoBadge` · `ProgressBar` · `ProgressRing` · `ToolTip`

### Menus & Toolbars (6 controls)
`CommandBar` · `CommandBarFlyout` · `MenuBar` · `MenuFlyout` · `AppBarButton` · `SwipeControl`

### Date & Time (4 controls)
`CalendarDatePicker` · `CalendarView` · `DatePicker` · `TimePicker`

### Scrolling (5 controls)
`ScrollViewer` · `ScrollView` · `PipsPager` · `SemanticZoom` · `AnnotatedScrollBar`

### Media (5 controls)
`Image` · `PersonPicture` · `WebView2` · `MediaPlayerElement` · `AnimatedVisualPlayer`

### Styles & Brushes (6 topics)
`AcrylicBrush` · System Backdrops (Mica/Acrylic) · `AnimatedIcon` · `ThemeShadow` · Compact Sizing · `IconElement`

### Motion & Animations (5 topics)
Connected Animation · Implicit Transitions · Page Transitions · Theme Transitions · `ParallaxView`

### System Integration (4 topics)
App Notifications (toast) · Clipboard · Storage Pickers · Content Island

### Windowing (3 topics)
`AppWindow` · `TitleBar` · Multiple Windows

### Fundamentals (5 topics)
`ResourceDictionary` · Styles · Data Binding · Data Templates · Custom UserControls

### App Patterns (3 patterns)
App Shell · MVVM + DI Setup · Theming

---

## Non-negotiable rules enforced

The skill instructs the agent to always follow these rules — no exceptions:

| Rule | Why |
|------|-----|
| Use `x:Bind`, never `{Binding}` | Compiled binding: faster, type-safe, no silent runtime failures |
| Set `x:DataType` on every `DataTemplate` | Required for compiled bindings inside templates |
| No `Microsoft.UI.Xaml.*` in ViewModels | Keeps ViewModels unit-testable |
| No business logic in code-behind | MVVM separation of concerns |
| `NavigationView` for primary navigation | Platform-standard; never `Pivot` or `TabView` for top-level nav |
| No `.Result` / `.Wait()` on the UI thread | Prevents deadlocks and frozen UI |
| `AutomationProperties.Name` on all interactive controls | Accessibility — Narrator / screen reader support |
| `ThemeResource` for all colour/brush values | Automatic light/dark/high-contrast adaptation |

---

## Repository structure

```
winui3-full-skill/
├── skill.md                 ← Skill entry point (loaded by the agent)
├── catalog/
│   ├── controls.md          ← Full index of all ~70 controls with snippet links
│   ├── best-practices.md    ← Performance, threading, accessibility, arch rules
│   └── patterns.md          ← MVVM, DI, navigation, async, error-handling patterns
└── snippets/
    ├── basic-input/         ← 14 files
    ├── text/                ← 7 files
    ├── collections/         ← 7 files
    ├── navigation/          ← 5 files
    ├── dialogs/             ← 4 files
    ├── layout/              ← 9 files
    ├── status/              ← 5 files
    ├── menus/               ← 6 files
    ├── datetime/            ← 4 files
    ├── scrolling/           ← 5 files
    ├── media/               ← 5 files
    ├── styles/              ← 6 files
    ├── motion/              ← 5 files
    ├── system/              ← 4 files
    ├── windowing/           ← 3 files
    ├── fundamentals/        ← 5 files
    └── patterns/            ← 3 files
```

---

## Quick-start examples

### Scaffold a page

```
Create a products list page for a WinUI 3 app. Use ListView with a DataTemplate,
MVVM with CommunityToolkit.Mvvm, async data loading, and an InfoBar for errors.
```

### Use a specific control

```
Show me how to use ContentDialog in WinUI 3 with async/await and a result enum.
```

### Generate a full app shell

```
Generate a WinUI 3 app shell with NavigationView, a Frame, back navigation,
custom TitleBar, Mica backdrop, and MVVM+DI wiring.
```

---

## Sources

All code snippets are based on real source from the official Microsoft WinUI Gallery:

- Control pages: `WinUIGallery/Samples/ControlPages/`
- Isolated snippets: `WinUIGallery/Samples/SampleCode/`

Source: [github.com/microsoft/WinUI-Gallery](https://github.com/microsoft/WinUI-Gallery)

---

## Related

- [Windows App SDK documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [WinUI 3 controls overview](https://learn.microsoft.com/windows/apps/design/controls/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [skills.sh directory](https://skills.sh)

---

## License

MIT
