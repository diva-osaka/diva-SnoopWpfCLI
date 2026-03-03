using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class WaitCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = WaitCommand.Create();
        Assert.Equal("wait", command.Name);
    }

    [Fact]
    public void Command_HasRequiredPidOption()
    {
        var command = WaitCommand.Create();
        var pidOption = command.Options.FirstOrDefault(o => o.Name == "--pid");
        Assert.NotNull(pidOption);
    }

    [Fact]
    public void Command_HasNameOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--name"));
    }

    [Fact]
    public void Command_HasTextOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--text"));
    }

    [Fact]
    public void Command_HasAutomationIdOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--automationid"));
    }

    [Fact]
    public void Command_HasUntilOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--until"));
    }

    [Fact]
    public void Command_HasTimeoutOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--timeout"));
    }

    [Fact]
    public void Command_HasIntervalOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--interval"));
    }

    [Fact]
    public void Parse_WithPidAndName_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithUntilGone_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --until gone");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithUntilEnabled_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --until enabled");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithUntilDisabled_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --until disabled");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInvalidUntil_HasErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --until invalid");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithTimeout_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --timeout 5000");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithInterval_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --interval 200");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithAllOptions_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --text Complete --until found --timeout 5000 --interval 200 --format json");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingPid_HasErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --name CountButton");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithTextOnly_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --text \"Loading complete\"");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithAutomationIdOnly_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --automationid BtnSubmit");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Command_HasTypeOption()
    {
        var command = WaitCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--type"));
    }

    [Fact]
    public void Parse_WithTypeOption_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --type System.Windows.Controls.Button --until found");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithNameAndType_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name CountButton --type System.Windows.Controls.Button --until found --timeout 10000");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Command_HasInteractiveOnlyOption()
    {
        var command = WaitCommand.Create();
        var opt = command.Options.FirstOrDefault(o => o.Name == "--interactive-only");
        Assert.NotNull(opt);
    }

    [Fact]
    public void Parse_WithInteractiveOnly_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name MyButton --interactive-only");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithUntilEnabledAndType_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --name MyButton --type System.Windows.Controls.Button --until enabled --timeout 5000");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithUntilDisabledAndAutomationId_NoErrors()
    {
        var command = WaitCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("wait --pid 1234 --automationid BtnSave --until disabled");
        Assert.Equal(0, result.Errors.Count);
    }
}
