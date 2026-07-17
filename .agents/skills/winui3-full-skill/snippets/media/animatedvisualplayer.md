# AnimatedVisualPlayer

Plays vector-based Lottie animations (Adobe After Effects exported via `lottie-windows`) or any `IAnimatedVisualSource` at native Composition layer speed — no video codec required.

---

## Basic Usage

```xaml
<!-- After effects animation codegen-ed to a C# class via Lottie-Windows -->
<AnimatedVisualPlayer
    x:Name="Player"
    AutoPlay="True">
    <AnimatedVisualPlayer.Source>
        <animatedvisuals:LottieLogo1 />
    </AnimatedVisualPlayer.Source>
</AnimatedVisualPlayer>
```

> **Namespace declaration required:**
> ```xaml
> xmlns:animatedvisuals="using:AnimatedVisuals"
> ```

---

## Play / Pause / Stop Controls

```xaml
<StackPanel Spacing="8">
    <AnimatedVisualPlayer
        x:Name="Player"
        Width="200"
        Height="200"
        AutoPlay="False">
        <AnimatedVisualPlayer.Source>
            <animatedvisuals:LottieLogo1 />
        </AnimatedVisualPlayer.Source>
    </AnimatedVisualPlayer>

    <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Content="Play" Click="PlayButton_Click"
                AutomationProperties.Name="Play animation" />
        <ToggleButton x:Name="PauseButton"
                      Content="Pause"
                      Checked="PauseButton_Checked"
                      Unchecked="PauseButton_Unchecked"
                      AutomationProperties.Name="Pause animation" />
        <Button Content="Stop" Click="StopButton_Click"
                AutomationProperties.Name="Stop animation" />
        <Button Content="Reverse" Click="ReverseButton_Click"
                AutomationProperties.Name="Reverse animation" />
    </StackPanel>
</StackPanel>
```

```csharp
// Views/AnimationPage.xaml.cs
private async void PlayButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    await Player.PlayAsync(fromProgress: 0, toProgress: 1, looped: false);
}

private void PauseButton_Checked(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    Player.Pause();
}

private void PauseButton_Unchecked(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    Player.Resume();
}

private void StopButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    Player.Stop();
}

private async void ReverseButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // Play backwards: progress from 1.0 → 0.0
    await Player.PlayAsync(fromProgress: 1, toProgress: 0, looped: false);
}
```

---

## Looping Indefinitely

```xaml
<AnimatedVisualPlayer
    x:Name="LoadingSpinner"
    Width="48"
    Height="48"
    AutoPlay="True">
    <AnimatedVisualPlayer.Source>
        <animatedvisuals:MySpinnerAnimation />
    </AnimatedVisualPlayer.Source>
</AnimatedVisualPlayer>
```

```csharp
// Start looping from code
await LoadingSpinner.PlayAsync(0, 1, looped: true);
```

---

## Play a Specific Segment

```csharp
// Play only frames 0.25–0.75 (middle half of the animation)
await Player.PlayAsync(fromProgress: 0.25, toProgress: 0.75, looped: false);
```

---

## Setting Playback Speed

```csharp
Player.PlaybackRate = 2.0; // 2x speed
```

---

## Generating Lottie C# Source

1. Install the `LottieGen` tool: `dotnet tool install -g Microsoft.Toolkit.Uwp.UI.Lottie`
2. Export your animation as a `.json` file from After Effects using the [Bodymovin plugin](https://aescripts.com/bodymovin/).
3. Generate the C# class:
   ```powershell
   lottie -InputFile MyAnimation.json -Language CSharp -OutputFolder Animations/
   ```
4. Add the generated file to your project and reference the namespace:
   ```xaml
   xmlns:animatedvisuals="using:Animations"
   ```

---

## Notes

- `AutoPlay="True"` starts the animation as soon as the source is ready; `AutoPlay="False"` requires a call to `PlayAsync()`.
- `PlayAsync()` returns a `Task` that completes when the animation segment finishes (or when stopped).
- Use `looped: true` in `PlayAsync()` for loading spinners and ambient animations.
- The animation runs in the **Composition layer** (not on the UI thread) — it won't freeze during CPU-heavy work.
- `AnimatedVisualPlayer` falls back to a static `FallbackContent` image when the `IAnimatedVisualSource` isn't supported on older OS builds.
- For icon-state transitions (e.g. play/pause toggle), use `AnimatedIcon` instead — it drives animation progress via control state.
