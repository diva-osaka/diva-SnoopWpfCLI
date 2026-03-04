using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class GetTreeCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = GetTreeCommand.Create();
        Assert.Equal("get-tree", command.Name);
    }

    [Fact]
    public void Command_HasPidOption()
    {
        var command = GetTreeCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasVerboseOption()
    {
        var command = GetTreeCommand.Create();
        var verboseOption = command.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.NotNull(verboseOption);
    }

    [Fact]
    public void Parse_WithPid_NoErrors()
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-tree --pid 1234");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithoutPid_HasErrors()
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-tree");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Command_HasDetailOption()
    {
        var command = GetTreeCommand.Create();
        var detailOption = command.Options.FirstOrDefault(o => o.Name == "--detail");
        Assert.NotNull(detailOption);
    }

    [Fact]
    public void Parse_WithDetail_NoErrors()
    {
        var command = GetTreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-tree --pid 1234 --detail");
        Assert.Equal(0, result.Errors.Count);
    }
}
