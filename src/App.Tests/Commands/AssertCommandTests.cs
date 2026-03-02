using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class AssertCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = AssertCommand.Create();
        Assert.Equal("assert", command.Name);
    }

    [Fact]
    public void Command_HasRequiredPidOption()
    {
        var command = AssertCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasNameOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--name");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasTextOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--text");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasAutomationIdOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--automationid");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasTypeOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--type");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasHashOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--hash");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasExistsOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--exists");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasPropertyOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--property");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasExpectedOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--expected");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Command_HasFormatOption()
    {
        var command = AssertCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--format");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Parse_WithExistsAssertion_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name StatusText --exists");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithTextAssertion_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name StatusText --text \"Success\"");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithPropertyAssertion_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --type MyApp.MainWindow --hash 12345 --property HasUnsavedChanges --expected true");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithAutomationIdAndExists_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --automationid BtnSubmit --exists");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --name StatusText --exists");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithJsonFormat_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name StatusText --exists --format json");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithTreeFormat_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name StatusText --exists --format tree");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInvalidFormat_HasErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name StatusText --exists --format xml");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithNameTextAndProperty_NoErrors()
    {
        var command = AssertCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("assert --pid 1234 --name MyElement --property IsActive --expected true");
        Assert.Equal(0, result.Errors.Count);
    }
}
