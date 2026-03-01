using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class ListWindowsCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = ListWindowsCommand.Create();
        Assert.Equal("list-windows", command.Name);
    }

    [Fact]
    public void Command_HasPidOption()
    {
        var command = ListWindowsCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasFormatOption()
    {
        var command = ListWindowsCommand.Create();
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(formatOption);
    }

    [Fact]
    public void Command_HasVerboseOption()
    {
        var command = ListWindowsCommand.Create();
        var verboseOption = command.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.NotNull(verboseOption);
    }

    [Fact]
    public void Parse_WithPid_NoErrors()
    {
        var command = ListWindowsCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-windows --pid 1234");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithoutPid_HasErrors()
    {
        var command = ListWindowsCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-windows");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithFormatJson_NoErrors()
    {
        var command = ListWindowsCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-windows --pid 1234 --format json");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithFormatTree_NoErrors()
    {
        var command = ListWindowsCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-windows --pid 1234 --format tree");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInvalidFormat_HasErrors()
    {
        var command = ListWindowsCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-windows --pid 1234 --format xml");
        Assert.True(result.Errors.Count > 0);
    }
}
