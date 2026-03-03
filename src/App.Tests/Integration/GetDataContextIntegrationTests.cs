using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class GetDataContextIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public GetDataContextIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(string type, int hash)> ResolveElementAsync(string name)
    {
        var (stdout, _, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --name {name}");
        Assert.True(exitCode == 0, $"find-element --name {name} failed");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("matchCount").GetInt32());

        var element = root.GetProperty("elements")[0];
        return (element.GetProperty("type").GetString()!, element.GetProperty("hashcode").GetInt32());
    }

    [Fact]
    public async Task GetDataContext_ReturnsViewModelProperties()
    {
        var (type, hash) = await ResolveElementAsync("InputTextBox");

        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"get-datacontext --pid {_fixture.TestAppPid} --type \"{type}\" --hash {hash}");

        Assert.True(exitCode == 0, $"get-datacontext failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("hasDataContext").GetBoolean());

        var dataContext = root.GetProperty("dataContext");
        Assert.True(dataContext.TryGetProperty("properties", out var properties));

        // Verify InputText property exists in the DataContext
        Assert.True(properties.TryGetProperty("InputText", out _),
            "DataContext should contain InputText property");
    }

    [Fact]
    public async Task GetDataContext_SpecificProperty()
    {
        var (type, hash) = await ResolveElementAsync("InputTextBox");

        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"get-datacontext --pid {_fixture.TestAppPid} --type \"{type}\" --hash {hash} --property InputText");

        Assert.True(exitCode == 0, $"get-datacontext failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());

        var dataContext = root.GetProperty("dataContext");
        var properties = dataContext.GetProperty("properties");
        var inputText = properties.GetProperty("InputText");
        Assert.Equal("Initial Text", inputText.GetProperty("value").GetString());
    }
}
