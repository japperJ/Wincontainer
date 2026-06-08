# App Notifications (Toast Notifications)

Windows App Notifications (toast notifications) display system-level notifications from a WinUI 3 packaged or unpackaged desktop app. Use `Microsoft.Windows.AppNotifications` (Windows App SDK) for the modern API.

---

## NuGet / Package Reference

```xml
<!-- .csproj — included in Windows App SDK -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.*" />
```

---

## Register Notification Activation (App.xaml.cs)

```csharp
// App.xaml.cs
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Register notification activator BEFORE showing any notifications
        AppNotificationManager.Default.NotificationInvoked +=
            OnNotificationInvoked;
        AppNotificationManager.Default.Register();
    }

    private void OnNotificationInvoked(AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        // Handle the user clicking the notification or its buttons
        string action = args.Arguments.ContainsKey("action")
            ? args.Arguments["action"]
            : string.Empty;

        DispatcherQueue.TryEnqueue(() =>
        {
            // Navigate or update UI based on action
        });
    }
}
```

---

## Simple Toast

```csharp
// Services/NotificationService.cs
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace MyApp.Services;

public static class NotificationService
{
    public static void ShowSimpleToast(string title, string message)
    {
        var builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }
}
```

---

## Toast with Button Actions

```csharp
public static void ShowActionToast(string title, string message)
{
    var notification = new AppNotificationBuilder()
        .AddText(title)
        .AddText(message)
        .AddButton(new AppNotificationButton("Approve")
            .AddArgument("action", "approve"))
        .AddButton(new AppNotificationButton("Dismiss")
            .AddArgument("action", "dismiss"))
        .BuildNotification();

    AppNotificationManager.Default.Show(notification);
}
```

---

## Toast with Image

```csharp
public static void ShowImageToast(string title, string message, string imageUri)
{
    var notification = new AppNotificationBuilder()
        .AddText(title)
        .AddText(message)
        .SetHeroImage(new Uri(imageUri))
        .BuildNotification();

    AppNotificationManager.Default.Show(notification);
}
```

---

## Toast with Input (Reply)

```csharp
public static void ShowReplyToast(string sender, string message)
{
    var notification = new AppNotificationBuilder()
        .AddText(sender)
        .AddText(message)
        .AddTextBox("tbReply", "Type a reply...", "Reply")
        .AddButton(new AppNotificationButton("Send")
            .AddArgument("action", "reply")
            .SetInputId("tbReply"))
        .BuildNotification();

    AppNotificationManager.Default.Show(notification);
}
```

---

## Handling Notification Activation in OnNotificationInvoked

```csharp
private void OnNotificationInvoked(AppNotificationManager sender,
    AppNotificationActivatedEventArgs args)
{
    var action = args.Arguments.TryGetValue("action", out var val) ? val : "";
    var replyText = args.UserInput.TryGetValue("tbReply", out var reply) ? reply : "";

    switch (action)
    {
        case "approve":
            // Handle approve
            break;
        case "reply":
            // Handle reply with replyText
            break;
    }
}
```

---

## Cleanup

```csharp
// Call in App.xaml.cs when the app closes
protected override void OnExit()
{
    AppNotificationManager.Default.Unregister();
    base.OnExit();
}
```

---

## Notes

- `AppNotificationManager.Default.Register()` must be called **before** showing any notifications and **before** the app is activated via a notification.
- For **unpackaged apps**, also set the app user model ID (AUMID) using `AppNotificationManager.SetDefault(aumid)` before registering.
- Notifications are only delivered when the app is running; for scheduled or background notifications use `Windows.UI.Notifications.ToastNotificationManager` (WinRT API).
- `AppNotificationBuilder` uses an XML schema under the hood; for advanced templates not covered by the builder, construct the XML manually via `AppNotification(string xml)`.
- Handle activation at app startup as well — if the user clicks a notification while the app is closed, the app launches with the activation arguments in `OnLaunched`.
