using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Formatters;

public class FormatOptionTests
{
    [Theory]
    [InlineData("list-processes")]
    [InlineData("list-processes --format json")]
    [InlineData("list-processes --format tree")]
    public void ListProcesses_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = ListProcessesCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void ListProcesses_FormatOption_DefaultIsJson()
    {
        var command = ListProcessesCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-processes");
        Assert.Equal(0, result.Errors.Count);

        var formatOption = command.Options.OfType<Option<string>>().First(o => o.Name == "--format");
        var value = result.GetValue(formatOption);
        Assert.Equal("json", value);
    }

    [Fact]
    public void ListProcesses_HasFormatOption()
    {
        var command = ListProcessesCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("get-tree --pid 1234")]
    [InlineData("get-tree --pid 1234 --format json")]
    [InlineData("get-tree --pid 1234 --format tree")]
    public void GetTree_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void GetTree_HasFormatOption()
    {
        var command = GetTreeCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("get-subtree --pid 1234 --type Button --hash 5678")]
    [InlineData("get-subtree --pid 1234 --type Button --hash 5678 --format tree")]
    public void GetSubtree_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void GetSubtree_HasFormatOption()
    {
        var command = GetSubtreeCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("get-element --pid 1234 --type Button --hash 5678")]
    [InlineData("get-element --pid 1234 --type Button --hash 5678 --format tree")]
    public void GetElement_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = GetElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void GetElement_HasFormatOption()
    {
        var command = GetElementCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("ping --pid 1234")]
    [InlineData("ping --pid 1234 --format tree")]
    public void Ping_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = PingCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Ping_HasFormatOption()
    {
        var command = PingCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("invoke --pid 1234 --type Button --hash 5678 --action Invoke_Invoke")]
    [InlineData("invoke --pid 1234 --type Button --hash 5678 --action Invoke_Invoke --format tree")]
    public void Invoke_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Invoke_HasFormatOption()
    {
        var command = InvokeCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Theory]
    [InlineData("screenshot --pid 1234")]
    [InlineData("screenshot --pid 1234 --format tree")]
    public void Screenshot_FormatOption_ParsesWithoutError(string commandLine)
    {
        var command = ScreenshotCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse(commandLine);
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Screenshot_HasFormatOption()
    {
        var command = ScreenshotCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Fact]
    public void AllCommands_FormatOption_InvalidValue_HasErrors()
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-tree --pid 1234 --format xml");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void ListProcesses_JsonFlag_StillWorks()
    {
        var command = ListProcessesCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-processes --json");
        Assert.Equal(0, result.Errors.Count);
    }
}
