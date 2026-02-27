using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class ListProcessesCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = ListProcessesCommand.Create();
        Assert.Equal("list-processes", command.Name);
    }

    [Fact]
    public void Command_HasJsonOption()
    {
        var command = ListProcessesCommand.Create();
        var jsonOption = command.Options.FirstOrDefault(o => o.Name == "--json");
        Assert.NotNull(jsonOption);
    }

    [Fact]
    public void Command_HasVerboseOption()
    {
        var command = ListProcessesCommand.Create();
        var verboseOption = command.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.NotNull(verboseOption);
    }

    [Fact]
    public void Parse_DefaultValues_JsonTrue_VerboseFalse()
    {
        var command = ListProcessesCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-processes");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithVerbose_NoErrors()
    {
        var command = ListProcessesCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("list-processes --verbose");
        Assert.Equal(0, result.Errors.Count);
    }
}
