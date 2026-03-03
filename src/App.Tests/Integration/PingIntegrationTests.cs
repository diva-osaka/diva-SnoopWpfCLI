using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class PingIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public PingIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Ping_WithValidPid_Succeeds()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync($"ping --pid {_fixture.TestAppPid}", timeoutMs: 60000);

        Assert.True(exitCode == 0, $"Expected exit code 0 but got {exitCode}.\nstdout: {stdout}\nstderr: {stderr}");
        Assert.False(string.IsNullOrWhiteSpace(stdout), "stdout should not be empty");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(_fixture.TestAppPid, root.GetProperty("processId").GetInt32());
    }

    [Fact]
    public async Task Ping_WithInvalidPid_ReturnsError()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync("ping --pid 99999");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
    }
}
