# WinContainers Sprint 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the first shippable Sprint 1 scaffold for WinContainers: a working WinUI 3 shell, a loopback ASP.NET Core host, script extraction, and the first runtime scripts for container list/start/stop/pull.

**Architecture:** The app will keep the agreed baseline: one executable, WinUI 3 shell, in-process ASP.NET Core host on loopback, PowerShell-first runtime execution, JSON state in `%APPDATA%`, and WSLC-backed scripts as the first runtime seam.

**Tech Stack:** .NET 8, WinUI 3 / Windows App SDK, ASP.NET Core minimal API, PowerShell SDK, xUnit + FluentAssertions + NSubstitute, GitHub Actions, Velopack.

---

### Task 1: Scaffold the multi-project solution

**Files:**
- Create: `WinContainers.sln`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `src/WinContainers.Core/WinContainers.Core.csproj`
- Create: `src/WinContainers.Service/WinContainers.Service.csproj`
- Create: `src/WinContainers.Scripts/WinContainers.Scripts.csproj`
- Create: `src/WinContainers.App/WinContainers.App.csproj`
- Create: `tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj`
- Create: `tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj`
- Create: `tests/WinContainers.Tests.Playwright/WinContainers.Tests.Playwright.csproj`

- [ ] **Step 1: Create the solution and project skeleton**

```bash
dotnet new sln -n WinContainers
mkdir -p src tests
dotnet new classlib -n WinContainers.Core -o src/WinContainers.Core
(dotnet new classlib -n WinContainers.Service -o src/WinContainers.Service)
dotnet new classlib -n WinContainers.Scripts -o src/WinContainers.Scripts
dotnet new winui3 -n WinContainers.App -o src/WinContainers.App --use-winui3 --framework net8.0-windows10.0.19041.0
(dotnet new xunit -n WinContainers.Tests.Unit -o tests/WinContainers.Tests.Unit)
(dotnet new xunit -n WinContainers.Tests.Integration -o tests/WinContainers.Tests.Integration)
(dotnet new xunit -n WinContainers.Tests.Playwright -o tests/WinContainers.Tests.Playwright)
```

- [ ] **Step 2: Add shared package and build settings**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>false</UseWPF>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

```xml
<!-- Directory.Packages.props -->
<Project>
  <ItemGroup>
    <PackageVersion Include="Microsoft.WindowsAppSDK" Version="1.5.240627000" />
    <PackageVersion Include="Microsoft.PowerShell.SDK" Version="7.4.6" />
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.1" />
    <PackageVersion Include="xunit" Version="2.9.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add all projects to the solution and verify restore works**

```bash
dotnet sln WinContainers.sln add src/WinContainers.Core/WinContainers.Core.csproj src/WinContainers.Service/WinContainers.Service.csproj src/WinContainers.Scripts/WinContainers.Scripts.csproj src/WinContainers.App/WinContainers.App.csproj tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj tests/WinContainers.Tests.Playwright/WinContainers.Tests.Playwright.csproj
dotnet restore WinContainers.sln
```

### Task 2: Implement the loopback host and token file contract

**Files:**
- Create: `src/WinContainers.Service/Host/ServiceHost.cs`
- Create: `src/WinContainers.Service/Program.cs`
- Create: `src/WinContainers.Core/Models/ServiceInfo.cs`
- Create: `src/WinContainers.Core/Contracts/IRuntimeDriver.cs`

- [ ] **Step 1: Add the service model and contracts**

```csharp
// src/WinContainers.Core/Models/ServiceInfo.cs
namespace WinContainers.Core.Models;

public sealed record ServiceInfo(string Port, string Token);
```

```csharp
// src/WinContainers.Core/Contracts/IRuntimeDriver.cs
namespace WinContainers.Core.Contracts;

public interface IRuntimeDriver
{
    Task<string> GetVersionAsync(CancellationToken cancellationToken);
    Task<string> RunAsync(string scriptName, IDictionary<string, string>? parameters, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Implement a minimal Kestrel host bound to loopback**

```csharp
// src/WinContainers.Service/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.ListenLocalhost(0);
});

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
app.Run();
```

- [ ] **Step 3: Write the test for the token/port file bootstrap contract**

```csharp
[Fact]
public void ServiceInfo_ShouldRoundTripPortAndToken()
{
    var info = new ServiceInfo("12345", "secret-token");

    info.Port.Should().Be("12345");
    info.Token.Should().Be("secret-token");
}
```

### Task 3: Implement script extraction and the first script manifest

**Files:**
- Create: `src/WinContainers.Scripts/ScriptManifest.json`
- Create: `src/WinContainers.Scripts/Get-Container.ps1`
- Create: `src/WinContainers.Scripts/Start-Container.ps1`
- Create: `src/WinContainers.Scripts/Stop-Container.ps1`
- Create: `src/WinContainers.Scripts/Pull-Image.ps1`
- Create: `src/WinContainers.Scripts/ScriptProvider.cs`

- [ ] **Step 1: Add the first manifest entries**

```json
{
  "scripts": [
    { "name": "Get-Container", "type": "one-shot", "command": "wslc container ps --format json", "timeoutMs": 30000 },
    { "name": "Start-Container", "type": "one-shot", "command": "wslc container start {id}", "timeoutMs": 30000 },
    { "name": "Stop-Container", "type": "one-shot", "command": "wslc container stop {id}", "timeoutMs": 30000 },
    { "name": "Pull-Image", "type": "stream", "command": "wslc image pull {image}", "timeoutMs": 1800000 }
  ]
}
```

- [ ] **Step 2: Implement the script extraction provider**

```csharp
public sealed class ScriptProvider
{
    public string RootDirectory { get; }

    public ScriptProvider(string rootDirectory) => RootDirectory = rootDirectory;

    public void EnsureExtracted() { }
}
```

- [ ] **Step 3: Add a unit test for the manifest shape**

```csharp
[Fact]
public void ScriptManifest_ShouldContainCoreScripts()
{
    var manifest = JsonSerializer.Deserialize<ScriptManifest>(File.ReadAllText("src/WinContainers.Scripts/ScriptManifest.json"));

    manifest!.Scripts.Should().Contain(s => s.Name == "Get-Container");
    manifest.Scripts.Should().Contain(s => s.Name == "Pull-Image");
}
```

### Task 4: Build the WinUI 3 shell and placeholder pages

**Files:**
- Modify: `src/WinContainers.App/App.xaml`
- Modify: `src/WinContainers.App/MainWindow.xaml`
- Create: `src/WinContainers.App/Pages/DashboardPage.xaml`
- Create: `src/WinContainers.App/Pages/ImagesPage.xaml`
- Create: `src/WinContainers.App/Pages/SettingsPage.xaml`

- [ ] **Step 1: Add the main navigation shell**

```xml
<!-- src/WinContainers.App/MainWindow.xaml -->
<Window x:Class="WinContainers.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WinContainers" Height="900" Width="1400">
  <NavigationView PaneDisplayMode="LeftCompact" IsBackButtonVisible="Collapsed">
    <NavigationView.MenuItems>
      <NavigationViewItem Content="Dashboard" Tag="Dashboard" />
      <NavigationViewItem Content="Images" Tag="Images" />
      <NavigationViewItem Content="Settings" Tag="Settings" />
    </NavigationView.MenuItems>
  </NavigationView>
</Window>
```

- [ ] **Step 2: Add a placeholder dashboard panel**

```xml
<!-- src/WinContainers.App/Pages/DashboardPage.xaml -->
<Page>
  <StackPanel Padding="24">
    <TextBlock Text="WinContainers" Style="{StaticResource TitleTextBlockStyle}" />
    <TextBlock Text="Sprint 1 placeholder dashboard" />
  </StackPanel>
</Page>
```

- [ ] **Step 3: Verify the project builds**

```bash
dotnet build WinContainers.sln -c Debug
```

### Task 5: Add the first automated test gates

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/UnitTest1.cs`
- Modify: `tests/WinContainers.Tests.Integration/IntegrationTest1.cs`
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Add unit tests for the core contracts and manifest shape**

```csharp
[Fact]
public void RuntimeDriverContract_ShouldBeAvailable() => true;
```

- [ ] **Step 2: Add an integration placeholder that is skipped when WSLC is unavailable**

```csharp
[Fact(Skip = "WSLC runtime is not available in CI by default")]
public void WslcRuntime_ShouldBeReachable() { }
```

- [ ] **Step 3: Add the first CI workflow**

```yaml
name: ci
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore WinContainers.sln
      - run: dotnet build WinContainers.sln -c Release
      - run: dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Release
```

---

## Sprint 1 Definition of Done

- Solution scaffolds and builds on `windows-latest`
- Loopback host starts and responds on `/api/health`
- Script manifest contains the first runtime commands
- WinUI 3 shell loads with placeholder dashboard/images/settings pages
- Unit test project runs in CI
- Sprint 1 deliverables are ready for the next implementation pass
