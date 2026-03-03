using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class GetTreeIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public GetTreeIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTree_ReturnsVisualTree()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync($"get-tree --pid {_fixture.TestAppPid}", timeoutMs: 60000);

        Assert.True(exitCode == 0, $"get-tree failed with exit code {exitCode}.\nstdout: {stdout}\nstderr: {stderr}");
        Assert.False(string.IsNullOrWhiteSpace(stdout), "stdout should not be empty");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(_fixture.TestAppPid, root.GetProperty("processId").GetInt32());

        Assert.True(root.TryGetProperty("visualTreeJson", out var visualTreeJson));
        Assert.False(string.IsNullOrWhiteSpace(visualTreeJson.GetString()), "visualTreeJson should contain data");
    }
}
