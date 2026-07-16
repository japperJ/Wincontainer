namespace WinContainers.Tests.Ui;

public sealed class AppLaunchTests : IClassFixture<WinAppDriverFixture>
{
    private readonly WinAppDriverFixture _fixture;

    public AppLaunchTests(WinAppDriverFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void App_Launches_And_Navigation_Is_Visible()
    {
        var navId = _fixture.Session.FindElementByAccessibilityId("RootNavigation");
        Assert.NotNull(navId);
    }

    [Fact]
    public void Navigate_To_Terminal_Tab()
    {
        GoToTerminalTab();

        var treeViewId = _fixture.Session.FindElementByName("CommandTree");
        Assert.NotNull(treeViewId);
    }

    [Fact]
    public void List_Containers_And_Get_A_Container()
    {
        GoToTerminalTab();

        var listContainers = _fixture.Session.WaitForElementByName("List Containers", 5000);
        Assert.NotNull(listContainers);
        _fixture.Session.Click(listContainers);

        var runBtn = _fixture.Session.WaitForElementByName("RunButton", 60000);
        Assert.NotNull(runBtn);
        _fixture.Session.Click(runBtn);

        var outputText = _fixture.Session.WaitForElementByName("OutputText", 60000);
        Assert.NotNull(outputText);

        var output = _fixture.Session.GetText(outputText);
        Assert.False(string.IsNullOrEmpty(output));

        var containerNames = ParseTableOutput(output);

        if (containerNames.Count == 0)
            Assert.Fail($"No containers parsed from output. Raw output (first 500):\n{output.Substring(0, Math.Min(output.Length, 500))}");

        if (containerNames.Count > 0)
        {
            var firstContainer = containerNames[0];
            Console.Error.WriteLine($"Using container: {firstContainer}");

            var inspectCmd = _fixture.Session.WaitForElementByName("Inspect Container", 5000);
            Assert.NotNull(inspectCmd);
            _fixture.Session.Click(inspectCmd);

            var containerDropdown = _fixture.Session.WaitForElementByName("Container", 10000);
            Assert.NotNull(containerDropdown);

            // Open the dropdown and use keyboard to select first item
            _fixture.Session.Click(containerDropdown);
            Thread.Sleep(500);
            _fixture.Session.SendKeysToSession("\uE015\uE007"); // Down + Enter
            Thread.Sleep(500);

            var inspectRunBtn = _fixture.Session.WaitForElementByName("RunButton", 5000);
            Assert.NotNull(inspectRunBtn);
            _fixture.Session.Click(inspectRunBtn);

            var inspectOutput = _fixture.Session.WaitForElementByName("OutputText", 30000);
            if (inspectOutput is null)
            {
                // Debug: try to find any error output
                Console.Error.WriteLine("OutputText not found after 30s. Checking for other elements...");
            }
            Assert.NotNull(inspectOutput);
            var outText = _fixture.Session.GetText(inspectOutput);
            Console.Error.WriteLine($"Inspect output: {outText}");
            Assert.False(string.IsNullOrEmpty(outText));
        }
    }

    private static List<string> ParseTableOutput(string output)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(output)) return result;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Skip the header line
            if (trimmed.StartsWith("CONTAINER ID", StringComparison.OrdinalIgnoreCase)) continue;

            // Split on 2+ spaces, last field is the container name
            var parts = trimmed.Split(new[] { "  ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var name = parts[^1].Trim(); // Last element
                if (!string.IsNullOrWhiteSpace(name) && name != "NAMES")
                    result.Add(name);
            }
        }
        return result;
    }

    private void GoToTerminalTab()
    {
        var terminalName = _fixture.Session.WaitForElementByAccessibilityId("TerminalNavItem", 3000);
        Assert.NotNull(terminalName);
        _fixture.Session.Click(terminalName);
        var header = _fixture.Session.WaitForElementByName("Commands", 8000);
        Assert.NotNull(header);
    }
}

public sealed class WinAppDriverFixture : IDisposable
{
    public WinAppDriverSession Session { get; }

    public WinAppDriverFixture()
    {
        var path = AppExePath;
        Console.Error.WriteLine($"AppExePath = {path}");
        Console.Error.WriteLine($"Exists = {File.Exists(path)}");
        Session = new WinAppDriverSession(path);
    }

    public void Dispose()
    {
        Session?.Dispose();
    }

    private static string AppExePath =>
        Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "publish", "WinContainers", "WinContainers.App.exe"));
}
