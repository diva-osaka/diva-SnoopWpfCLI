using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class GetElementCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = GetElementCommand.Create();
        Assert.Equal("get-element", command.Name);
    }

    [Fact]
    public void Command_HasRequiredOptions()
    {
        var command = GetElementCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--pid"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--type"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--hash"));
    }

    [Fact]
    public void Command_HasNameOption()
    {
        var command = GetElementCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--name"));
    }

    [Fact]
    public void Parse_WithAllRequiredOptions_NoErrors()
    {
        var command = GetElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-element --pid 1234 --type System.Windows.Controls.Button --hash 5678");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithNameOption_NoErrors()
    {
        var command = GetElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-element --pid 1234 --name CountButton");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Command_HasTextOption()
    {
        var command = GetElementCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--text"));
    }

    [Fact]
    public void Parse_WithTextOption_NoErrors()
    {
        var command = GetElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-element --pid 1234 --text \"Click Me\"");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = GetElementCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-element --type System.Windows.Controls.Button --hash 5678");
        Assert.True(result.Errors.Count > 0);
    }
}
