using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class WaitIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public WaitIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WaitFound_ExistingElement_Succeeds()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"wait --pid {_fixture.TestAppPid} --name HeaderTitle --until found --timeout 5000");

        Assert.True(exitCode == 0, $"wait failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("found", root.GetProperty("condition").GetString());
    }

    [Fact]
    public async Task WaitGone_ExistingElement_TimesOut()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"wait --pid {_fixture.TestAppPid} --name HeaderTitle --until gone --timeout 2000");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Timeout", root.GetProperty("error").GetString());
    }
}
