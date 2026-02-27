using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

public class GetTreeIntegrationTests : IntegrationTestBase, IAsyncLifetime
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
    public async Task GetTree_ReturnsVisualTree()
    {
        // First ping to inject
        var (pingStdout, pingStderr, pingExitCode) = await RunCliAsync($"ping --pid {TestAppPid}", timeoutMs: 60000);
        Assert.True(pingExitCode == 0, $"Ping failed with exit code {pingExitCode}.\nstdout: {pingStdout}\nstderr: {pingStderr}");

        // Then get the tree
        var (stdout, stderr, exitCode) = await RunCliAsync($"get-tree --pid {TestAppPid}", timeoutMs: 60000);

        Assert.True(exitCode == 0, $"get-tree failed with exit code {exitCode}.\nstdout: {stdout}\nstderr: {stderr}");
        Assert.False(string.IsNullOrWhiteSpace(stdout), "stdout should not be empty");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(TestAppPid, root.GetProperty("processId").GetInt32());

        Assert.True(root.TryGetProperty("visualTreeJson", out var visualTreeJson));
        Assert.False(string.IsNullOrWhiteSpace(visualTreeJson.GetString()), "visualTreeJson should contain data");
    }
}
