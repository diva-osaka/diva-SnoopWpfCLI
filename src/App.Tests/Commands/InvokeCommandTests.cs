using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using SnoopWpfCLI.Commands;
using Xunit;

namespace SnoopWpfCLI.Tests.Commands;

public class InvokeCommandTests
{
    [Fact]
    public void Command_HasCorrectName()
    {
        var command = InvokeCommand.Create();
        Assert.Equal("invoke", command.Name);
    }

    [Fact]
    public void Command_HasRequiredOptions()
    {
        var command = InvokeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--pid"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--type"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--hash"));
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--action"));
    }

    [Fact]
    public void Command_HasNameOption()
    {
        var command = InvokeCommand.Create();
        Assert.NotNull(command.Options.FirstOrDefault(o => o.Name == "--name"));
    }

    [Fact]
    public void Command_HasOptionalParamsOption()
    {
        var command = InvokeCommand.Create();
        var paramsOption = command.Options.FirstOrDefault(o => o.Name == "--params");
        Assert.NotNull(paramsOption);
    }

    [Fact]
    public void Parse_WithAllRequiredOptions_NoErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --type System.Windows.Controls.Button --hash 5678 --action Invoke_Invoke");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithNameOption_NoErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --name MyButton --action Invoke_Invoke");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithParams_NoErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --type System.Windows.Controls.TextBox --hash 9012 --action Value_Set --params {\"value\":\"hello\"}");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_MissingAction_HasErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --type System.Windows.Controls.Button --hash 5678");
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Parse_WithButtonBaseClickAction_NoErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --type System.Windows.Controls.RadioButton --hash 5678 --action ButtonBase_Click");
        Assert.Equal(0, result.Errors.Count);
    }

    [Fact]
    public void Parse_WithExecuteCommandAction_NoErrors()
    {
        var command = InvokeCommand.Create();
        var root = new RootCommand();
        root.Subcommands.Add(command);

        var result = root.Parse("invoke --pid 1234 --type System.Windows.Controls.Button --hash 5678 --action ExecuteCommand");
        Assert.Equal(0, result.Errors.Count);
    }
}
