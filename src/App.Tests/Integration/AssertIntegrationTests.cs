using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class AssertIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public AssertIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AssertExists_Present_Passes()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name HeaderTitle --exists");

        Assert.True(exitCode == 0, $"assert failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("passed").GetBoolean());
        Assert.Equal("exists", root.GetProperty("assertion").GetString());
    }

    [Fact]
    public async Task AssertExists_Absent_Fails()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name NonExistent --exists");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task AssertText_ExactMatch_Passes()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name CountButton --text \"Click Me\"");

        Assert.True(exitCode == 0, $"assert failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task AssertText_Mismatch_Fails()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name CountButton --text \"Wrong\"");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task AssertProperty_Match_Passes()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name InputTextBox --property InputText --expected \"Initial Text\"");

        Assert.True(exitCode == 0, $"assert failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("passed").GetBoolean());
        Assert.Equal("property", root.GetProperty("assertion").GetString());
    }

    [Fact]
    public async Task AssertProperty_Mismatch_Fails()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"assert --pid {_fixture.TestAppPid} --name InputTextBox --property InputText --expected \"Wrong\"");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("passed").GetBoolean());
    }
}
