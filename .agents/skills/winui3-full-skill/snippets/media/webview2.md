# WebView2

Embeds the Chromium-based Edge engine as a control inside a WinUI 3 app. Supports full web content including HTML5, CSS3, JavaScript, and modern web APIs. Requires the WebView2 Runtime to be installed on the user's machine (included with Windows 11; redistributable available for Windows 10).

---

## Basic Usage

```xaml
<WebView2
    x:Name="MyWebView"
    MinWidth="200"
    MinHeight="200"
    HorizontalAlignment="Stretch"
    VerticalAlignment="Stretch"
    Source="https://learn.microsoft.com/windows/apps/winui/winui3/" />
```

---

## Navigation with Address Bar

```xaml
<Grid RowDefinitions="Auto, *">
    <Grid ColumnDefinitions="*, Auto" Margin="0,0,0,8">
        <TextBox
            x:Name="UrlBox"
            PlaceholderText="Enter URL"
            Text="https://bing.com"
            KeyDown="UrlBox_KeyDown" />
        <Button
            Grid.Column="1"
            Margin="8,0,0,0"
            Content="Go"
            Click="GoButton_Click" />
    </Grid>

    <WebView2
        x:Name="WebView"
        Grid.Row="1"
        NavigationCompleted="WebView_NavigationCompleted"
        NavigationStarting="WebView_NavigationStarting" />
</Grid>
```

```csharp
// Views/BrowserPage.xaml.cs
public sealed partial class BrowserPage : Page
{
    public BrowserPage()
    {
        InitializeComponent();
        Loaded += BrowserPage_Loaded;
    }

    private async void BrowserPage_Loaded(object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // EnsureCoreWebView2Async must be called before any WebView2 API use
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Navigate("https://bing.com");
    }

    private void GoButton_Click(object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (Uri.TryCreate(UrlBox.Text, UriKind.Absolute, out var uri))
            WebView.CoreWebView2.Navigate(uri.ToString());
    }

    private void UrlBox_KeyDown(object sender,
        Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            GoButton_Click(sender, e);
    }

    private void WebView_NavigationStarting(WebView2 sender,
        Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        // Optionally cancel navigation to untrusted origins
    }

    private void WebView_NavigationCompleted(WebView2 sender,
        Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
    {
        UrlBox.Text = sender.Source?.ToString() ?? string.Empty;
    }
}
```

---

## Executing JavaScript

```csharp
// Call into the page after it has loaded
private async Task<string> GetPageTitleAsync()
{
    await WebView.EnsureCoreWebView2Async();
    return await WebView.CoreWebView2.ExecuteScriptAsync("document.title");
}
```

---

## Intercepting Navigation (Host Object / Virtual Host)

```csharp
private async void SetupWebViewAsync()
{
    await WebView.EnsureCoreWebView2Async();
    var coreWv2 = WebView.CoreWebView2;

    // Map a virtual host name to a local folder for serving local files
    coreWv2.SetVirtualHostNameToFolderMapping(
        "local.app",
        Windows.ApplicationModel.Package.Current.InstalledPath + "\\WebContent",
        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

    coreWv2.Navigate("https://local.app/index.html");
}
```

---

## Notes

- Always call `await WebView.EnsureCoreWebView2Async()` before accessing `CoreWebView2`.
- WebView2 requires the **Microsoft Edge WebView2 Runtime** — it is pre-installed on Windows 11 and available as a redistributable for Windows 10.
- `WebView2` has an **airspace limitation** — it always renders on top of other XAML controls. Use `DefaultBackgroundColor` to match the app theme and avoid visual glitches.
- For local content, use `SetVirtualHostNameToFolderMapping` rather than `file://` URIs (blocked by default by the renderer's security policy).
- Use `CoreWebView2.WebMessageReceived` and `PostWebMessageAsJson` for two-way JS ↔ C# communication.
- NuGet package: `Microsoft.Web.WebView2` — add to your `.csproj` alongside the Windows App SDK packages.
