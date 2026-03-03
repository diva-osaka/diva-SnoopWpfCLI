using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class GetElementIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public GetElementIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetElementByName_ReturnsDetails()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"get-element --pid {_fixture.TestAppPid} --name CountButton");

        Assert.True(exitCode == 0, $"get-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Button", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("element", out _), "Should contain element details");
    }

    [Fact]
    public async Task GetElementByName_NotFound()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"get-element --pid {_fixture.TestAppPid} --name NoSuchElement");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
    }
}
