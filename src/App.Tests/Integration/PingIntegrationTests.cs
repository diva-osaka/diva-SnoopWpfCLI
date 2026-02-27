using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

public class PingIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await StartTestAppAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await CleanupAsync();
    }

    [Fact]
    public async Task Ping_WithValidPid_Succeeds()
    {
        var (stdout, stderr, exitCode) = await RunCliAsync($"ping --pid {TestAppPid}", timeoutMs: 60000);

        Assert.True(exitCode == 0, $"Expected exit code 0 but got {exitCode}.\nstdout: {stdout}\nstderr: {stderr}");
        Assert.False(string.IsNullOrWhiteSpace(stdout), "stdout should not be empty");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(TestAppPid, root.GetProperty("processId").GetInt32());
    }

    [Fact]
    public async Task Ping_WithInvalidPid_ReturnsError()
    {
        var (stdout, stderr, exitCode) = await RunCliAsync("ping --pid 99999");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
    }
}
