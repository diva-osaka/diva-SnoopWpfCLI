using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class ListWindowsIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public ListWindowsIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListWindows_ReturnsMainWindow()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"list-windows --pid {_fixture.TestAppPid}");

        Assert.True(exitCode == 0, $"list-windows failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());

        var windows = root.GetProperty("windows");
        Assert.True(windows.GetArrayLength() >= 1, "Should find at least one window");

        // Verify the main window is present (title: "SnoopWpfCLI Test App")
        bool foundMainWindow = false;
        foreach (var win in windows.EnumerateArray())
        {
            var title = win.GetProperty("title").GetString();
            if (title != null && title.Contains("SnoopWpfCLI Test App"))
            {
                foundMainWindow = true;
                Assert.True(win.GetProperty("isVisible").GetBoolean());
                Assert.True(win.GetProperty("width").GetDouble() > 0);
                Assert.True(win.GetProperty("height").GetDouble() > 0);
                break;
            }
        }

        Assert.True(foundMainWindow, "MainWindow should be found in the window list");
    }
}
