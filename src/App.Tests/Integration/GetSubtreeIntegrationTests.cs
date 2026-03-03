using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class GetSubtreeIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public GetSubtreeIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSubtreeByName_ReturnsTree()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"get-subtree --pid {_fixture.TestAppPid} --name TestExpander");

        Assert.True(exitCode == 0, $"get-subtree failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.TryGetProperty("visualTreeJson", out var visualTreeJson));
        Assert.False(string.IsNullOrWhiteSpace(visualTreeJson.GetString()),
            "visualTreeJson should contain subtree data");
    }
}
