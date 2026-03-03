using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[Collection("TestApp")]
public class FindElementIntegrationTests
{
    private readonly TestAppFixture _fixture;

    public FindElementIntegrationTests(TestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FindByName_ReturnsSingleElement()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --name HeaderTitle");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("matchCount").GetInt32());

        var elements = root.GetProperty("elements");
        Assert.Equal(1, elements.GetArrayLength());

        var element = elements[0];
        Assert.Contains("TextBlock", element.GetProperty("type").GetString());
    }

    [Fact]
    public async Task FindByName_CaseInsensitive()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --name headertitle");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("matchCount").GetInt32());
    }

    [Fact]
    public async Task FindByText_PartialMatch()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --text \"Click Me\"");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("matchCount").GetInt32() >= 1, "Should find at least one element with 'Click Me'");

        // Verify CountButton is among results
        bool foundCountButton = false;
        foreach (var el in root.GetProperty("elements").EnumerateArray())
        {
            var name = el.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (name == "CountButton")
            {
                foundCountButton = true;
                break;
            }
        }
        Assert.True(foundCountButton, "CountButton should be found with text 'Click Me'");
    }

    [Fact]
    public async Task FindByType_ReturnsMultipleElements()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --type System.Windows.Controls.Button");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("matchCount").GetInt32() > 1, "Should find multiple Button elements");
    }

    [Fact]
    public async Task FindByBindingPath_FindsBoundElements()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --binding-path InputText");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("matchCount").GetInt32() >= 2,
            "Should find at least InputTextBox and MirrorTextBox");

        // Verify both expected elements are present
        bool foundInputTextBox = false;
        bool foundMirrorTextBox = false;
        foreach (var el in root.GetProperty("elements").EnumerateArray())
        {
            var name = el.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (name == "InputTextBox") foundInputTextBox = true;
            if (name == "MirrorTextBox") foundMirrorTextBox = true;
        }
        Assert.True(foundInputTextBox, "InputTextBox should be found with binding-path InputText");
        Assert.True(foundMirrorTextBox, "MirrorTextBox should be found with binding-path InputText");
    }

    [Fact]
    public async Task FindWithCombinedFilters()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --name InputTextBox --binding-path InputText");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("matchCount").GetInt32());
    }

    [Fact]
    public async Task FindNonExistent_ReturnsZeroMatches()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --name NonExistent");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("matchCount").GetInt32());
    }

    [Fact]
    public async Task FindNoCriteria_ReturnsError()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid}");

        Assert.NotEqual(0, exitCode);

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.TryGetProperty("error", out _), "error field should be present in stdout JSON");
        Assert.True(string.IsNullOrWhiteSpace(stderr), "stderr should be empty unless --verbose is enabled");
    }

    [Fact]
    public async Task FindWithInteractiveOnly_ReturnsOnlyInteractiveElements()
    {
        var (stdout, stderr, exitCode) = await _fixture.RunCliAsync(
            $"find-element --pid {_fixture.TestAppPid} --text \"Click Me\" --interactive-only");

        Assert.True(exitCode == 0, $"find-element failed: exitCode={exitCode}\nstdout: {stdout}\nstderr: {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        // All results should be interactive types
        var interactiveTypes = new[]
        {
            "Button", "TextBox", "CheckBox", "ComboBox", "ListBox",
            "RadioButton", "Slider", "ToggleButton", "PasswordBox",
            "RichTextBox", "DatePicker", "Expander", "MenuItem",
            "TabItem", "TreeViewItem", "ListBoxItem", "ComboBoxItem"
        };
        foreach (var el in root.GetProperty("elements").EnumerateArray())
        {
            var typeName = el.GetProperty("type").GetString();
            Assert.True(
                typeName != null && interactiveTypes.Any(t => typeName.EndsWith(t)),
                $"Element type '{typeName}' should be an interactive type");
        }
    }
}
