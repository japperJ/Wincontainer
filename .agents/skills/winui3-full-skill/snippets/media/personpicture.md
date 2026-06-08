# PersonPicture

A circular avatar control that shows a contact's profile photo, display name initials, or a generic person icon as fallback. Ideal for contact lists, chat UIs, and profile pages.

---

## Basic Usage

```xaml
<!-- Profile photo -->
<PersonPicture ProfilePicture="ms-appx:///Assets/profile.jpg" />

<!-- Display name initials (auto-computed) -->
<PersonPicture DisplayName="Jane Doe" />

<!-- Explicit initials -->
<PersonPicture Initials="JD" />
```

---

## Bound to ViewModel

```xaml
<PersonPicture
    Height="64"
    ProfilePicture="{x:Bind ViewModel.ProfilePicture}"
    DisplayName="{x:Bind ViewModel.DisplayName}"
    Initials="{x:Bind ViewModel.Initials}" />
```

```csharp
// ViewModels/ProfileViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MyApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    private BitmapImage? _profilePicture;

    [ObservableProperty]
    private string _displayName = "Jane Doe";

    // Initials are derived automatically from DisplayName when not set explicitly.
    // Set explicitly only if you need custom initials (e.g. different script).
    [ObservableProperty]
    private string _initials = string.Empty;

    public async Task LoadProfileAsync()
    {
        // Load from remote or local source
        var bmp = new BitmapImage(new Uri("https://example.com/avatar.jpg"));
        ProfilePicture = bmp;
    }
}
```

---

## In a ListView (Contact List)

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Contacts}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Contact">
            <StackPanel Orientation="Horizontal" Spacing="12" Padding="8">
                <PersonPicture
                    Width="40"
                    Height="40"
                    DisplayName="{x:Bind Name}"
                    ProfilePicture="{x:Bind AvatarSource}" />
                <StackPanel VerticalAlignment="Center">
                    <TextBlock
                        Style="{StaticResource BaseTextBlockStyle}"
                        Text="{x:Bind Name}" />
                    <TextBlock
                        Style="{StaticResource BodyTextBlockStyle}"
                        Text="{x:Bind Email}"
                        Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                </StackPanel>
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

---

## With Badge (Notification Count)

```xaml
<Grid>
    <PersonPicture DisplayName="Bob Baker" Height="48" />
    <InfoBadge
        Value="3"
        VerticalAlignment="Top"
        HorizontalAlignment="Right" />
</Grid>
```

---

## Variants

| Property | Description |
|---|---|
| `ProfilePicture` | `ImageSource` — photo to display |
| `DisplayName` | Full name string; initials auto-extracted (e.g. "Jane Doe" → "JD") |
| `Initials` | Override initials (max 2 characters shown) |
| `IsGroup` | `true` to show a group icon instead of a person icon fallback |
| `BadgeNumber` | Integer badge count (shows a red badge dot) |
| `BadgeGlyph` | Segoe MDL2 glyph string for badge |
| `BadgeImageSource` | `ImageSource` for badge |

---

## Notes

- The fallback order is: `ProfilePicture` → derived initials from `DisplayName` → `Initials` → generic person icon.
- Set only the minimum needed: if you have a photo, set `ProfilePicture`; the control will fall back automatically.
- `BadgeNumber`, `BadgeGlyph`, and `BadgeImageSource` are mutually exclusive; the last one set wins.
- Always provide `AutomationProperties.Name` when the control is interactive (e.g. a button wrapping it).
- Control is circular by default; use `CornerRadius` on a wrapping `Border` if you need rounded-square avatars.
