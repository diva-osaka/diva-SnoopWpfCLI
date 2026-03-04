using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class GetSubtreeCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = GetSubtreeCommand.Create();
        Assert.Equal("get-subtree", command.Name);
    }

    [Fact]
    public void Command_HasRequiredOptions()
    {
        var command = GetSubtreeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--pid"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--type"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--hash"));
    }

    [Fact]
    public void Command_HasNameOption()
    {
        var command = GetSubtreeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--name"));
    }

    [Fact]
    public void Parse_WithAllRequiredOptions_NoErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --pid 1234 --type System.Windows.Controls.Button --hash 5678");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithNameOption_NoErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --pid 1234 --name MyPanel");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --type System.Windows.Controls.Button --hash 5678");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Command_HasTextOption()
    {
        var command = GetSubtreeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--text"));
    }

    [Fact]
    public void Command_HasBindingPathOption()
    {
        var command = GetSubtreeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--binding-path"));
    }

    [Fact]
    public void Parse_WithTextOption_NoErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --pid 1234 --text \"Click Me\"");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithBindingPathOption_NoErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --pid 1234 --binding-path DataContext.Name");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Command_HasDetailOption()
    {
        var command = GetSubtreeCommand.Create();
        var detailOption = command.Options.FirstOrDefault(o => o.Name == "--detail");
        Assert.NotNull(detailOption);
    }

    [Fact]
    public void Parse_WithDetail_NoErrors()
    {
        var command = GetSubtreeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("get-subtree --pid 1234 --name MyPanel --detail");
        Assert.Equal(0, result.Errors.Count);
    }
}
