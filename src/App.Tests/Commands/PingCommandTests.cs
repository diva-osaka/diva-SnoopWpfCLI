using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class PingCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = PingCommand.Create();
        Assert.Equal("ping", command.Name);
    }

    [Fact]
    public void Command_HasPidOption()
    {
        var command = PingCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasVerboseOption()
    {
        var command = PingCommand.Create();
        var verboseOption = command.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.NotNull(verboseOption);
    }

    [Fact]
    public void Parse_WithPid_NoErrors()
    {
        var command = PingCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("ping --pid 1234");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithoutPid_HasErrors()
    {
        var command = PingCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("ping");
        Assert.True(result.Errors.Count > 0);
    }
}
