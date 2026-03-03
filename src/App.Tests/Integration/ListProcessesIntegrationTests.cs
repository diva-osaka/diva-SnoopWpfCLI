using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class ListProcessesIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public ListProcessesIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListProcesses_FindsTestApp()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync("list-processes");

        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stdout), "stdout should not be empty");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());

        var processes = root.GetProperty("processes");
        Assert.True(processes.GetArrayLength() > 0, "Should find at least one WPF process");

        // Find our TestApp in the list
        bool foundTestApp = false;
        foreach (var proc in processes.EnumerateArray())
        {
            var processName = proc.GetProperty("processName").GetString();
            if (processName == "TestApp")
            {
                foundTestApp = true;
                Assert.Equal(_fixture.TestAppPid, proc.GetProperty("processId").GetInt32());
                Assert.True(proc.GetProperty("isWpfApplication").GetBoolean());
                break;
            }
        }

        Assert.True(foundTestApp, "TestApp should be found in the WPF process list");
    }

    [Fact]
    public async Task ListProcesses_ReturnsValidJson()
    {
        var (stdout, _, exitCode) = await _fixture.RunCliAsync("list-processes");

        Assert.Equal(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("success", out _));
        Assert.True(root.TryGetProperty("count", out _));
        Assert.True(root.TryGetProperty("processes", out _));
    }
}
